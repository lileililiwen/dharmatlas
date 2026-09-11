using Dharmatlas.Domain.Entities;

namespace Dharmatlas.Api.Models;

public sealed record CreateContributionRequest
{
    public required SubmissionType Type { get; init; }
    public required string Summary { get; init; }
    public required string PayloadJson { get; init; }
    public IReadOnlyList<string> SourceIds { get; init; } = Array.Empty<string>();
    public string? TargetId { get; init; }
}

public sealed record ReviewContributionRequest
{
    public required ReviewDecisionType Decision { get; init; }
    public required string Reason { get; init; }
}
