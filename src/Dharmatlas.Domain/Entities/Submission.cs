using System.Diagnostics.CodeAnalysis;
using Dharmatlas.Domain;

namespace Dharmatlas.Domain.Entities;

/// <summary>
/// A contributor's proposed change to a historical record. It carries the
/// proposed payload as JSON, the supporting source references, and the decision
/// trail. A submission is always created as a <see cref="SubmissionStatus.Draft"/>
/// and only becomes <see cref="SubmissionStatus.Approved"/> through an explicit
/// reviewer decision, so contributors can never directly publish.
/// </summary>
public sealed record Submission
{
    public EntityId Id { get; init; } = EntityId.New();
    public EntityId ContributorId { get; init; }
    public SubmissionType Type { get; init; }

    /// <summary>The existing entity being changed, or null when proposing a new entity.</summary>
    public EntityId? TargetId { get; init; }

    public string Summary { get; init; }
    public string PayloadJson { get; init; }
    public IReadOnlyList<EntityId> SourceIds { get; init; } = Array.Empty<EntityId>();
    public SubmissionStatus Status { get; init; } = SubmissionStatus.Draft;
    public DateTimeOffset CreatedAt { get; init; }
    public long Version { get; init; }
    public IReadOnlyList<ReviewDecision> Decisions { get; init; } = new List<ReviewDecision>();

    public Submission(
        EntityId contributorId,
        SubmissionType type,
        string summary,
        string payloadJson,
        IReadOnlyList<EntityId>? sourceIds = null,
        EntityId? targetId = null)
    {
        if (string.IsNullOrWhiteSpace(summary))
        {
            throw new DomainValidationException("A submission requires a summary.");
        }

        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            throw new DomainValidationException("A submission requires a payload.");
        }

        ContributorId = contributorId;
        Type = type;
        Summary = summary;
        PayloadJson = payloadJson;
        SourceIds = sourceIds ?? Array.Empty<EntityId>();
        TargetId = targetId;
    }

    /// <summary>Move a draft into the review queue. No-op fields are preserved.</summary>
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public Submission Submit()
    {
        if (Status != SubmissionStatus.Draft)
        {
            throw new DomainValidationException($"Only a draft can be submitted (current status: {Status}).");
        }

        return this with { Status = SubmissionStatus.PendingReview };
    }

    /// <summary>
    /// Apply a reviewer decision, returning a new submission whose status reflects
    /// the decision and whose decision trail records it.
    /// </summary>
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public Submission WithDecision(ReviewDecision decision)
    {
        if (Status is not (SubmissionStatus.PendingReview or SubmissionStatus.ChangesRequested))
        {
            throw new DomainValidationException(
                $"A decision can only be recorded on a pending or changes-requested submission (current: {Status}).");
        }

        var status = decision.Decision switch
        {
            ReviewDecisionType.Approve => SubmissionStatus.Approved,
            ReviewDecisionType.RequestChanges => SubmissionStatus.ChangesRequested,
            ReviewDecisionType.Reject => SubmissionStatus.Rejected,
            _ => throw new DomainValidationException($"Unsupported decision {decision.Decision}.")
        };

        return this with
        {
            Status = status,
            Decisions = Decisions.Append(decision).ToList()
        };
    }

    /// <summary>Mark the submission as conflicting with an existing approved change.</summary>
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public Submission MarkConflict(string reason)
    {
        if (Status is not (SubmissionStatus.PendingReview or SubmissionStatus.ChangesRequested))
        {
            throw new DomainValidationException("Only a pending submission can be flagged as a conflict.");
        }

        return this with { Status = SubmissionStatus.Conflict };
    }
}
