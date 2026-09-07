using Dharmatlas.Domain;
using Dharmatlas.Domain.Entities;

namespace Dharmatlas.Contributions.Models;

/// <summary>
/// The inspectable, ordered revision history for one target object. Revisions are
/// returned newest-first; each carries the prior value, changed fields, actor,
/// timestamp, reason, and sources (project-foundation audit requirement).
/// </summary>
public sealed record ContributionHistoryView
{
    public required EntityId TargetId { get; init; }
    public required IReadOnlyList<Revision> Revisions { get; init; }
}
