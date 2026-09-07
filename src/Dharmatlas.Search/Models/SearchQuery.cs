using Dharmatlas.Domain.Entities;

namespace Dharmatlas.Search.Models;

/// <summary>
/// Parameters for a multilingual entity search. Matching is performed across an
/// entity's canonical name and every alternate script, transliteration, and
/// romanization. Empty or whitespace terms return no results so the call is
/// always explicit rather than a silent "return everything".
/// </summary>
public sealed record SearchQuery
{
    /// <summary>The user's search term (a name in any script/romanization).</summary>
    public string Term { get; init; } = string.Empty;

    /// <summary>Optional entity-type filter; null means all types.</summary>
    public IReadOnlyList<EntityType>? Types { get; init; }

    /// <summary>Optional region filter (case-insensitive); null means any region.</summary>
    public string? Region { get; init; }

    public int? Limit { get; init; } = 50;
}
