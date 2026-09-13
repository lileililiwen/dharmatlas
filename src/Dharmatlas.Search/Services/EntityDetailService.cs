using System.Diagnostics.CodeAnalysis;
using Dharmatlas.Domain;
using Dharmatlas.Domain.Entities;
using Dharmatlas.Persistence;
using Dharmatlas.Search.Engine;
using Dharmatlas.Search.Models;
using Microsoft.EntityFrameworkCore;

namespace Dharmatlas.Search.Services;

/// <summary>
/// Assembles the read-only entity detail payload from the data model. It loads
/// the entity, its names, relationships, and referenced sources, resolves related
/// entity names/types, and delegates projection to the pure assembler. Missing
/// fields (names, relationships, sources) are simply absent in the result rather
/// than invented.
/// </summary>
public sealed class EntityDetailService : IEntityDetailService
{
    private readonly DharmatlasDbContext _db;

    public EntityDetailService(DharmatlasDbContext db) => _db = db;

    public async Task<EntityDetail?> GetAsync(EntityId id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Entities.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        var names = await _db.EntityNames
            .Where(n => n.EntityId == id)
            .ToListAsync(cancellationToken);

        var relationships = await _db.Relationships
            .Where(r => r.FromEntityId == id || r.ToEntityId == id)
            .ToListAsync(cancellationToken);

        var claims = await _db.Claims
            .Where(c => c.SubjectEntityId == id)
            .ToListAsync(cancellationToken);

        var entitySourceIds = entity switch
        {
            Place p => p.SourceIds,
            Institution i => i.SourceIds,
            _ => Array.Empty<EntityId>()
        };

        var sourceIds = entitySourceIds
            .Concat(relationships.SelectMany(r => r.SourceIds))
            .Concat(claims.Where(c => c.Status == ClaimStatus.Published).SelectMany(c => c.SourceIds))
            .Distinct()
            .ToList();

        var sources = sourceIds.Count == 0
            ? new List<SourceView>()
            : (await _db.Sources
                .Where(s => sourceIds.Contains(s.Id))
                .ToListAsync(cancellationToken))
                .Select(ToView)
                .ToList();

        var relatedIds = relationships
            .Select(r => r.FromEntityId == id ? r.ToEntityId : r.FromEntityId)
            .Distinct()
            .ToList();

        var relatedLookup = await BuildRelatedLookupAsync(relatedIds, cancellationToken);
        var placeRegions = await _db.Entities
            .OfType<Place>()
            .Where(p => p.Region != null)
            .ToDictionaryAsync(p => p.Id, p => p.Region!, cancellationToken);

        var region = ResolveRegion(entity, placeRegions);
        var activePeriod = entity switch
        {
            Place p => p.Activity?.DisplayExpression,
            Institution i => i.Activity?.DisplayExpression,
            _ => null
        };

        return EntityDetailAssembler.Build(
            entity, names, relationships, sources, relatedLookup, region, activePeriod, claims);
    }

    private async Task<IReadOnlyDictionary<EntityId, EntityDetailAssembler.RelatedInfo>> BuildRelatedLookupAsync(
        IReadOnlyList<EntityId> relatedIds,
        CancellationToken cancellationToken)
    {
        if (relatedIds.Count == 0)
        {
            return new Dictionary<EntityId, EntityDetailAssembler.RelatedInfo>();
        }

        var entities = await _db.Entities
            .Where(e => relatedIds.Contains(e.Id))
            .ToListAsync(cancellationToken);

        var typeById = entities.ToDictionary(e => e.Id, e => e.Type);

        var relatedNames = await _db.EntityNames
            .Where(n => relatedIds.Contains(n.EntityId))
            .ToListAsync(cancellationToken);

        var canonicalByName = relatedNames
            .GroupBy(n => n.EntityId)
            .ToDictionary(g => g.Key, g => EntityNameReadModel.CanonicalName(g));

        var lookup = new Dictionary<EntityId, EntityDetailAssembler.RelatedInfo>();
        foreach (var rid in relatedIds)
        {
            if (!typeById.TryGetValue(rid, out var type))
            {
                continue;
            }

            lookup[rid] = new EntityDetailAssembler.RelatedInfo(type, canonicalByName.GetValueOrDefault(rid) ?? rid.ToString());
        }

        return lookup;
    }

    private static string? ResolveRegion(Entity entity, IReadOnlyDictionary<EntityId, string> placeRegions) => entity switch
    {
        Place p => p.Region,
        Tradition t => t.Region,
        Institution i when i.PlaceId is { } pid && placeRegions.TryGetValue(pid, out var r) => r,
        _ => null
    };

    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    private static SourceView ToView(Source s) => new()
    {
        Id = s.Id,
        Title = s.Title,
        Author = s.Author,
        Date = s.Date,
        Identifier = s.Identifier,
        Tier = s.Tier?.ToString()
    };
}
