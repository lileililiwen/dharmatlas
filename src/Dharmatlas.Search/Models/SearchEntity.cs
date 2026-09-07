using Dharmatlas.Domain;
using Dharmatlas.Domain.Entities;
using Dharmatlas.Domain.ValueObjects;

namespace Dharmatlas.Search.Models;

/// <summary>
/// Engine input for search: a flattened, persistence-independent view of an
/// entity carrying the fields the search needs (names, resolved region, active
/// period, certainty). Built by the query service from the data model so the
/// pure engine never touches EF Core.
/// </summary>
public sealed record SearchEntity
{
    public required EntityId Id { get; init; }
    public required EntityType Type { get; init; }
    public required IReadOnlyList<EntityName> Names { get; init; }

    /// <summary>Resolved region for geo entities (Place/Tradition, or an Institution's place).</summary>
    public string? Region { get; init; }

    /// <summary>Display expression of the recorded active period, when known.</summary>
    public string? ActivePeriod { get; init; }

    public Certainty Certainty { get; init; } = Certainty.Unknown;
}
