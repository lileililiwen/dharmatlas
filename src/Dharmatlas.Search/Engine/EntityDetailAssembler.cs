using System.Diagnostics.CodeAnalysis;
using Dharmatlas.Domain;
using Dharmatlas.Domain.Entities;
using Dharmatlas.Domain.ValueObjects;
using Dharmatlas.Search.Models;

namespace Dharmatlas.Search.Engine;

/// <summary>
/// Pure assembler that turns an entity and its supporting records into the
/// read-only detail payload. Only recorded data is surfaced; relationships are
/// resolved against a supplied name/type lookup and grouped by type so the page
/// can show "related texts/places" and "teachers/students" without inventing
/// missing fields. Independent of EF Core for unit testing.
/// </summary>
public static class EntityDetailAssembler
{
    /// <summary>Minimal info about an entity referenced by a relationship.</summary>
    public sealed record RelatedInfo(EntityType Type, string Name);

    public static EntityDetail Build(
        Entity entity,
        IReadOnlyList<EntityName> names,
        IReadOnlyList<Relationship> relationships,
        IReadOnlyList<SourceView> sources,
        IReadOnlyDictionary<EntityId, RelatedInfo> relatedLookup,
        string? region,
        string? activePeriod)
    {
        var canonical = CanonicalNameOf(names, entity.Id);

        var nameViews = names
            .Select(n => new NameView
            {
                Value = n.Value,
                Language = n.Language,
                Script = n.Script,
                Romanization = n.Romanization,
                IsPrimary = n.IsPrimary
            })
            .ToList();

        var related = new List<RelatedEntity>();
        foreach (var r in relationships)
        {
            var isOutgoing = r.FromEntityId == entity.Id;
            var otherId = isOutgoing ? r.ToEntityId : r.FromEntityId;

            relatedLookup.TryGetValue(otherId, out var info);
            var otherType = info?.Type ?? EntityType.Person; // unknown referenced type defaults safely
            var otherName = info?.Name ?? otherId.ToString();

            related.Add(new RelatedEntity
            {
                Id = otherId,
                Type = otherType,
                Name = otherName,
                RelationType = r.Type,
                Direction = isOutgoing ? RelationshipDirection.Outgoing : RelationshipDirection.Incoming,
                Certainty = r.Certainty,
                SourceIds = r.SourceIds
            });
        }

        return new EntityDetail
        {
            Id = entity.Id,
            Type = entity.Type,
            CanonicalName = canonical,
            Names = nameViews,
            Summary = entity.Summary,
            Certainty = CertaintyOf(entity),
            ActivePeriod = activePeriod,
            Region = region,
            Sources = sources,
            Relationships = related,
            RelatedPeople = Group(related, EntityType.Person),
            RelatedPlaces = Group(related, EntityType.Place),
            RelatedTexts = Group(related, EntityType.Text),
            RelatedInstitutions = Group(related, EntityType.Institution),
            RelatedEvents = Group(related, EntityType.Event)
        };
    }

    private static IReadOnlyList<RelatedEntity> Group(IReadOnlyList<RelatedEntity> related, EntityType type) =>
        related.Where(r => r.Type == type).ToList();

    private static string CanonicalNameOf(IReadOnlyList<EntityName> names, EntityId fallback)
    {
        var primary = names.FirstOrDefault(n => n.IsPrimary) ?? names.FirstOrDefault();
        return primary?.Value ?? fallback.ToString();
    }

    [SuppressMessage("ReSharper", "PatternIsUnnecessary")]
    private static Certainty CertaintyOf(Entity entity) => entity switch
    {
        Place p => p.Certainty,
        Institution i => i.Certainty,
        _ => Certainty.Unknown
    };
}
