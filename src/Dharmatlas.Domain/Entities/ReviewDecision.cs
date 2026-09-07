using Dharmatlas.Domain;

namespace Dharmatlas.Domain.Entities;

/// <summary>
/// One reviewer action on a submission. Stored immutably on the submission so the
/// full decision trail is inspectable; the decision type drives the submission's
/// status transition.
/// </summary>
public sealed record ReviewDecision
{
    public EntityId Id { get; init; } = EntityId.New();
    public EntityId ReviewerId { get; init; }
    public ReviewDecisionType Decision { get; init; }
    public string Reason { get; init; }
    public DateTimeOffset Timestamp { get; init; }

    public ReviewDecision(
        EntityId reviewerId,
        ReviewDecisionType decision,
        string reason,
        DateTimeOffset timestamp)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainValidationException("A review decision requires a reason.");
        }

        ReviewerId = reviewerId;
        Decision = decision;
        Reason = reason;
        Timestamp = timestamp;
    }
}
