namespace Dharmatlas.Search.Models;

/// <summary>One recorded name form of an entity for the detail surface.</summary>
public sealed record NameView
{
    public required string Value { get; init; }
    public required string Language { get; init; }
    public required string Script { get; init; }
    public required string Romanization { get; init; }
    public bool IsPrimary { get; init; }
}
