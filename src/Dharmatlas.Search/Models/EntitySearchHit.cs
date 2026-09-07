using Dharmatlas.Domain;
using Dharmatlas.Domain.Entities;
using Dharmatlas.Domain.ValueObjects;

namespace Dharmatlas.Search.Models;

/// <summary>
/// One search result row. Carries enough provenance to distinguish entities that
/// share a name: the entity type, the exact name form that matched, its region,
/// and its active period. Missing fields are left null rather than invented.
/// </summary>
public sealed record EntitySearchHit
{
    public required EntityId Id { get; init; }
    public required EntityType Type { get; init; }

    /// <summary>The entity's canonical (primary) name for display.</summary>
    public required string CanonicalName { get; init; }

    /// <summary>The specific name value that matched the query.</summary>
    public required string MatchedName { get; init; }

    /// <summary>Which kind of name form matched (canonical / romanization / alternate script).</summary>
    public required NameForm MatchedForm { get; init; }

    /// <summary>Resolved region, when the entity has one.</summary>
    public string? Region { get; init; }

    /// <summary>Active-period display string, when recorded.</summary>
    public string? ActivePeriod { get; init; }

    /// <summary>Historical certainty of the entity's recorded identity/activity.</summary>
    public Certainty Certainty { get; init; } = Certainty.Unknown;

    /// <summary>Relative ranking score from the engine; higher is a stronger match.</summary>
    public int Score { get; init; }
}
