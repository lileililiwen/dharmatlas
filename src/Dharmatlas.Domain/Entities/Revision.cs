namespace Dharmatlas.Domain.Entities;

/// <summary>
/// An audit record of a change to a target object. Retains the prior value so
/// review history is inspectable; it does not by itself grant publication
/// rights (project-foundation contract).
/// </summary>
public sealed record Revision
{
    public EntityId Id { get; init; } = EntityId.New();
    public EntityId TargetId { get; init; }
    public string PriorValueJson { get; init; }
    public EntityId ContributorId { get; init; }
    public string Reason { get; init; }
    public DateTimeOffset Timestamp { get; init; }

    public Revision(
        EntityId targetId,
        string priorValueJson,
        EntityId contributorId,
        string reason,
        DateTimeOffset timestamp)
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
    }
}
