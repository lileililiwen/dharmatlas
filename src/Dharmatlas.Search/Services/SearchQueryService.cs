using Dharmatlas.Domain;
using Dharmatlas.Domain.Entities;
using Dharmatlas.Domain.ValueObjects;
using Dharmatlas.Persistence;
using Dharmatlas.Search.Engine;
using Dharmatlas.Search.Models;
using Microsoft.EntityFrameworkCore;

namespace Dharmatlas.Search.Services;

/// <summary>
/// Applies the pure search engine to entities loaded from the data model.
/// Read-only: it does not mutate entities or duplicate name/source semantics. The
/// per-entity region is resolved (an institution inherits its place's region) so
/// region filtering works uniformly across geo entity kinds.
/// </summary>
public sealed class SearchQueryService : ISearchQueryService
{
    /// <summary>Candidate cap before precise engine ranking (results cap at 100).</summary>
    internal const int MaxCandidates = 1000;

    private readonly DharmatlasDbContext _db;

    public SearchQueryService(DharmatlasDbContext db) => _db = db;

    public async Task<SearchResult> QueryAsync(SearchQuery query, CancellationToken cancellationToken = default)
    {
        var term = query.Term.Trim();
        if (term.Length == 0)
        {
            return new SearchResult { Term = term, Hits = Array.Empty<EntitySearchHit>() };
        }

        // Candidate selection mirrors the engine's match layers: the raw form
        // (alternate scripts), the base-normalized form (diacritic-insensitive),
        // and the transliteration key (Wade-Giles/Pinyin/Wylie tolerant). The
        // engine re-scores every candidate precisely, so an over-broad
        // candidate set only costs work, never correctness. Capped so
        // adversarial terms cannot materialize the whole table.
        var normalizedTerm = NameNormalizer.Normalize(term);
        var keyTerm = NameNormalizer.FoldTransliteration(normalizedTerm);
        IQueryable<EntityName> namesQuery = _db.EntityNames.AsNoTracking();
        if (_db.Database.IsRelational())
        {
            namesQuery = namesQuery.Where(n =>
                EF.Functions.ILike(n.Value, $"%{term}%") ||
                EF.Functions.ILike(EF.Property<string>(n, "NormalizedValue"), $"%{normalizedTerm}%") ||
                EF.Functions.ILike(EF.Property<string>(n, "NormalizedValue"), $"%{keyTerm}%"));
        }
        else
        {
            namesQuery = namesQuery.Where(n =>
                n.Value.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                EF.Property<string>(n, "NormalizedValue").Contains(normalizedTerm, StringComparison.Ordinal) ||
                EF.Property<string>(n, "NormalizedValue").Contains(keyTerm, StringComparison.Ordinal));
        }

        var candidateIds = await namesQuery.Select(n => n.EntityId).Distinct().Take(MaxCandidates).ToListAsync(cancellationToken);
        var entitiesQuery = _db.Entities.AsNoTracking().Where(e => candidateIds.Contains(e.Id));
        if (query.Types is { Count: > 0 }) entitiesQuery = entitiesQuery.Where(e => query.Types.Contains(e.Type));
        var entities = await entitiesQuery.ToListAsync(cancellationToken);
        var names = await _db.EntityNames.AsNoTracking().Where(n => candidateIds.Contains(n.EntityId)).ToListAsync(cancellationToken);

        var namesByEntity = names
            .GroupBy(n => n.EntityId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<EntityName>)EntityNameReadModel.Order(g));

        var institutionPlaceIds = entities.OfType<Institution>().Where(i => i.PlaceId is not null).Select(i => i.PlaceId!.Value).ToList();
        var placeRegions = await _db.Entities.OfType<Place>().AsNoTracking()
            .Where(p => p.Region != null && institutionPlaceIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Region!, cancellationToken);

        var searchEntities = entities.Select(e => ToSearchEntity(e, namesByEntity, placeRegions)).ToList();

        return SearchEngine.Search(query, searchEntities);
    }

    private static SearchEntity ToSearchEntity(
        Entity entity,
        IReadOnlyDictionary<EntityId, IReadOnlyList<EntityName>> namesByEntity,
        IReadOnlyDictionary<EntityId, string> placeRegions)
    {
        namesByEntity.TryGetValue(entity.Id, out var names);

        var region = entity switch
        {
            Place p => p.Region,
            Tradition t => t.Region,
            Institution i when i.PlaceId is { } pid && placeRegions.TryGetValue(pid, out var r) => r,
            _ => null
        };

        string? activePeriod = entity switch
        {
            Place p => p.Activity?.DisplayExpression,
            Institution i => i.Activity?.DisplayExpression,
            _ => null
        };

        return new SearchEntity
        {
            Id = entity.Id,
            Type = entity.Type,
            Names = names ?? Array.Empty<EntityName>(),
            Region = region,
            ActivePeriod = activePeriod,
            Certainty = entity switch
            {
                Place p => p.Certainty,
                Institution i => i.Certainty,
                _ => Certainty.Unknown
            }
        };
    }
}
