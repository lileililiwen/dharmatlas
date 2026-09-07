using Dharmatlas.Search.Models;

namespace Dharmatlas.Search.Models;

/// <summary>
/// The full response for a search: the echoed term plus ranked, de-duplicated
/// entity hits (one stable identity per entity, even if several of its names match).
/// </summary>
public sealed record SearchResult
{
    public required string Term { get; init; }
    public required IReadOnlyList<EntitySearchHit> Hits { get; init; }
}
