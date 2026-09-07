using Dharmatlas.Domain;
using Dharmatlas.Domain.Entities;
using Dharmatlas.Domain.ValueObjects;

namespace Dharmatlas.Search.Models;

/// <summary>
/// A typed relationship edge rendered for the entity detail surface. The
/// direction is relative to the focal entity: <see cref="Outgoing"/> means the
/// focal entity is the source of the relationship (e.g. "X authored Y"),
/// <see cref="Incoming"/> means it is the target (e.g. "Y was authored by X").
/// </summary>
public enum RelationshipDirection
{
    Outgoing,
    Incoming
}

/// <summary>One related entity and the relationship that connects it.</summary>
public sealed record RelatedEntity
{
    public required EntityId Id { get; init; }
    public required EntityType Type { get; init; }
    public required string Name { get; init; }
    public required string? RelationType { get; init; }
    public required RelationshipDirection Direction { get; init; }
    public required Certainty Certainty { get; init; }
    public required IReadOnlyList<EntityId> SourceIds { get; init; }
}
