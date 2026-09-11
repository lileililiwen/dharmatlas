using Dharmatlas.Domain;
using Dharmatlas.Domain.Entities;
using Dharmatlas.Domain.ValueObjects;
using Dharmatlas.Search.Models;

namespace Dharmatlas.Search.Models;

/// <summary>
/// The read-only detail payload for one entity ("entity page" data). It surfaces
/// only what is recorded: names, relationships, sources, and type-specific period
/// or region context. Fields that are not in the data are left null (or grouped
/// lists are empty) rather than inferred or fabricated.
/// </summary>
public sealed record EntityDetail
{
    public required EntityId Id { get; init; }
    public required EntityType Type { get; init; }
    public required string CanonicalName { get; init; }
    public required IReadOnlyList<NameView> Names { get; init; }
    public string? Summary { get; init; }

    /// <summary>Historical certainty of the entity's recorded identity/activity.</summary>
    public Certainty Certainty { get; init; } = Certainty.Unknown;

    /// <summary>Active-period display string (Place/Institution), when recorded.</summary>
    public string? ActivePeriod { get; init; }

    /// <summary>Resolved region (Place/Tradition, or an Institution's place), when known.</summary>
    public string? Region { get; init; }

    public required IReadOnlyList<SourceView> Sources { get; init; }
    public IReadOnlyList<Claim> Claims { get; init; } = Array.Empty<Claim>();

    /// <summary>All typed relationships touching this entity.</summary>
    public required IReadOnlyList<RelatedEntity> Relationships { get; init; }

    // Related-entity groupings by type, derived from <see cref="Relationships"/>.
    // These power the "related texts/places" and "teachers/students" surfaces and
    // are empty when no such relationships are recorded.
    public required IReadOnlyList<RelatedEntity> RelatedPeople { get; init; }
    public required IReadOnlyList<RelatedEntity> RelatedPlaces { get; init; }
    public required IReadOnlyList<RelatedEntity> RelatedTexts { get; init; }
    public required IReadOnlyList<RelatedEntity> RelatedInstitutions { get; init; }
    public required IReadOnlyList<RelatedEntity> RelatedEvents { get; init; }
}
