using Dharmatlas.Domain;

namespace Dharmatlas.Search.Models;

/// <summary>A minimal, inspectable view of a bibliographic source referenced by an entity.</summary>
public sealed record SourceView
{
    public required EntityId Id { get; init; }
    public required string Title { get; init; }
    public string? Author { get; init; }
    public string? Date { get; init; }
    public string? Identifier { get; init; }
    public string? Tier { get; init; }
}
