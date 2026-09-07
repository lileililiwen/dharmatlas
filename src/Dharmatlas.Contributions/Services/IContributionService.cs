using Dharmatlas.Contributions.Models;
using Dharmatlas.Domain;
using Dharmatlas.Domain.Entities;

namespace Dharmatlas.Contributions.Services;

/// <summary>
/// Read/write contract for the contribution review workflow. Contributors create
/// drafts and submit them; reviewers decide; approvals mutate the published record
/// and record an immutable revision. Direct publication by contributors is impossible
/// because approval is the only path that changes published data.
/// </summary>
public interface IContributionService
{
    /// <summary>Validate and submit a draft contribution. Returns the pending submission.</summary>
    Task<Submission> SubmitAsync(
        EntityId contributorId,
        SubmissionType type,
        string summary,
        string payloadJson,
        IReadOnlyList<EntityId>? sourceIds = null,
        EntityId? targetId = null,
        CancellationToken cancellationToken = default);

    /// <summary>Record a reviewer decision. Returns the updated submission.</summary>
    Task<Submission> ReviewAsync(
        EntityId submissionId,
        EntityId reviewerId,
        ReviewDecisionType decision,
        string reason,
        CancellationToken cancellationToken = default);

    /// <summary>Ordered revision history for a target object (newest first).</summary>
    Task<ContributionHistoryView?> GetHistoryAsync(EntityId targetId, CancellationToken cancellationToken = default);

    /// <summary>All submissions by a contributor, with explicit status.</summary>
    Task<IReadOnlyList<SubmissionView>> GetContributorSubmissionsAsync(EntityId contributorId, CancellationToken cancellationToken = default);

    /// <summary>Pending and conflicted submissions awaiting reviewer action.</summary>
    Task<IReadOnlyList<SubmissionView>> GetReviewerQueueAsync(CancellationToken cancellationToken = default);
}
