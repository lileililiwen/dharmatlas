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
    private readonly DharmatlasDbContext _db;

    public SearchQueryService(DharmatlasDbContext db) => _db = db;

    public async Task<SearchResult> QueryAsync(SearchQuery query, CancellationToken cancellationToken = default)
    {
        var entities = await _db.Entities.ToListAsync(cancellationToken);
        var names = await _db.EntityNames.ToListAsync(cancellationToken);

        var namesByEntity = names
            .GroupBy(n => n.EntityId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<EntityName>)EntityNameReadModel.Order(g));

        var placeRegions = entities
            .OfType<Place>()
            .Where(p => p.Region is not null)
            .ToDictionary(p => p.Id, p => p.Region!);

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
