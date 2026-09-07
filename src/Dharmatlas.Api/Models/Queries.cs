using Dharmatlas.Domain;
using Dharmatlas.Domain.Entities;
using Dharmatlas.Domain.ValueObjects;

namespace Dharmatlas.Api.Models;

/// <summary>Filters for the persons list endpoint.</summary>
public sealed record PersonQuery(string? Name, Paging Paging);

/// <summary>Filters for the events list endpoint.</summary>
public sealed record EventQuery(
    int? FromYear,
    int? ToYear,
    string? Region,
    string? Category,
    EntityId? PlaceId,
    Paging Paging);

/// <summary>Filters for the places list endpoint.</summary>
public sealed record PlaceQuery(string? Region, PlaceKind? Kind, Paging Paging);

/// <summary>Filters for the texts list endpoint.</summary>
public sealed record TextQuery(string? Language, Paging Paging);

/// <summary>Filters for the relationships list endpoint.</summary>
public sealed record RelationshipQuery(
    EntityId? From,
    EntityId? To,
    string? Type,
    Certainty? MinCertainty,
    Paging Paging);
