using Dharmatlas.Domain.ValueObjects;

namespace Dharmatlas.Domain.Entities;

/// <summary>
/// A typed, directional, sourceable edge between two entities (e.g. "born-at",
/// "authored", "influenced-by"). Every relationship must cite at least one
/// source and carry a certainty value; disagreement is expressed as multiple
/// edges, never a single merged claim.
/// </summary>
public sealed record Relationship
{
    public EntityId Id { get; init; } = EntityId.New();
    public EntityId FromEntityId { get; init; }
    public EntityId ToEntityId { get; init; }
    public string Type { get; init; }
    public Certainty Certainty { get; init; }
    public IReadOnlyList<EntityId> SourceIds { get; init; }

    public Relationship(
        EntityId fromEntityId,
        EntityId toEntityId,
        string type,
        Certainty certainty,
        IReadOnlyList<EntityId> sourceIds)
    {
        if (fromEntityId == toEntityId)
        {
            throw new DomainValidationException("A relationship cannot connect an entity to itself.");
        }

        if (string.IsNullOrWhiteSpace(type))
        {
            throw new DomainValidationException("A relationship requires a type.");
        }

        if (sourceIds is null || sourceIds.Count == 0)
        {
            throw new DomainValidationException("A relationship must cite at least one source.");
        }

        FromEntityId = fromEntityId;
        ToEntityId = toEntityId;
        Type = type;
        Certainty = certainty;
        SourceIds = sourceIds;
    }

    /// <summary>
    /// Rejects references to entities or sources absent from the supplied known
    /// sets. Called by the persistence boundary before commit.
    /// </summary>
    public void ValidateReferences(IReadOnlySet<EntityId> knownEntities, IReadOnlySet<EntityId> knownSources)
    {
        if (!knownEntities.Contains(FromEntityId))
        {
            throw new InvalidReferenceException($"Relationship references unknown entity {FromEntityId}.");
        }

        if (!knownEntities.Contains(ToEntityId))
        {
            throw new InvalidReferenceException($"Relationship references unknown entity {ToEntityId}.");
        }

        foreach (var sourceId in SourceIds)
        {
            if (!knownSources.Contains(sourceId))
            {
                throw new InvalidReferenceException($"Relationship references unknown source {sourceId}.");
            }
        }
    }
}
