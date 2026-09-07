namespace Dharmatlas.Domain.Entities;

/// <summary>
/// Lifecycle state of a contribution submission. A submission never moves to a
/// published state without an explicit reviewer approval; conflicts are surfaced
/// for review rather than silently merged.
/// </summary>
public enum SubmissionStatus
{
    /// <summary>Authoring in progress; not yet sent for review (not publicly visible).</summary>
    Draft,

    /// <summary>Submitted and awaiting a reviewer decision.</summary>
    PendingReview,

    /// <summary>Approved by a reviewer; the published record reflects it.</summary>
    Approved,

    /// <summary>Reviewer asked for changes before it can be reconsidered.</summary>
    ChangesRequested,

    /// <summary>Reviewer rejected the submission outright.</summary>
    Rejected,

    /// <summary>Overlapping or incompatible with an existing approved change; needs review.</summary>
    Conflict
}
