using Dharmatlas.Domain;
using Dharmatlas.Domain.Entities;

namespace Dharmatlas.Contributions.Models;

/// <summary>
/// A flattened, read-only view of a submission for contributor and reviewer
/// surfaces. Status is explicit so draft vs. published state is never ambiguous.
/// </summary>
public sealed record SubmissionView
{
    public required EntityId Id { get; init; }
    public required EntityId ContributorId { get; init; }
    public string? ContributorName { get; init; }
    public required SubmissionType Type { get; init; }
    public EntityId? TargetId { get; init; }
    public required string Summary { get; init; }
    public required SubmissionStatus Status { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required int SourceCount { get; init; }
    public string? LastDecisionReason { get; init; }
}
