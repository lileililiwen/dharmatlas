namespace Dharmatlas.Domain.Entities;

/// <summary>
/// An immutable audit record of a change to a target object. Retains the prior
/// value and the field-level diff so review history is fully inspectable. It
/// records both the contributor who proposed the change and the reviewer who
/// approved it, plus the supporting sources; it does not by itself grant
/// publication rights (project-foundation contract). Once written, a revision is
/// never mutated, which is what makes the history rollback-safe.
/// </summary>
public sealed record Revision
{
    public EntityId Id { get; init; } = EntityId.New();
    public EntityId TargetId { get; init; }
    public string PriorValueJson { get; init; }

    /// <summary>The contributor who proposed the underlying submission.</summary>
    public EntityId ContributorId { get; init; }

    /// <summary>The reviewer who approved the change, when applicable.</summary>
    public EntityId? ReviewerId { get; init; }

    /// <summary>Human-readable reason recorded at approval time.</summary>
    public string Reason { get; init; }

    /// <summary>JSON array of changed top-level field names (field-level diff).</summary>
    public string? ChangedFieldsJson { get; init; }

    /// <summary>Sources that support the approved change.</summary>
    public IReadOnlyList<EntityId> SourceIds { get; init; } = Array.Empty<EntityId>();

    public DateTimeOffset Timestamp { get; init; }

    public Revision(
        EntityId targetId,
        string priorValueJson,
        EntityId contributorId,
        string reason,
        DateTimeOffset timestamp,
        EntityId? reviewerId = null,
        string? changedFieldsJson = null,
        IReadOnlyList<EntityId>? sourceIds = null)
    {
        if (string.IsNullOrWhiteSpace(priorValueJson))
        {
            throw new DomainValidationException("A revision requires the prior value.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainValidationException("A revision requires a reason.");
        }

        TargetId = targetId;
        PriorValueJson = priorValueJson;
        ContributorId = contributorId;
        Reason = reason;
        Timestamp = timestamp;
        ReviewerId = reviewerId;
        ChangedFieldsJson = changedFieldsJson;
        SourceIds = sourceIds ?? Array.Empty<EntityId>();
    }
}
