using Dharmatlas.Domain;
using Dharmatlas.Domain.ValueObjects;

namespace Dharmatlas.Timeline.Models;

/// <summary>
/// Read-only projection of an event for the timeline surface. Preserves the
/// authored date expression and certainty state rather than collapsing uncertain
/// dates into exact years.
/// </summary>
/// <param name="Id">Stable event identifier.</param>
/// <param name="Title">Display title from the event's primary name.</param>
/// <param name="DisplayDate">Authored date expression (e.g. "c. 150 CE").</param>
/// <param name="Certainty">Historical-certainty state of the event.</param>
/// <param name="Category">Timeline category, if any.</param>
/// <param name="Region">Geographic region, if any.</param>
/// <param name="LinkedEntityIds">Entities linked to the event (e.g. place).</param>
/// <param name="DetailRoute">Route to the event detail view.</param>
public sealed record EventSummary(
    EntityId Id,
    string Title,
    string DisplayDate,
    Certainty Certainty,
    string? Category,
    string? Region,
    IReadOnlyList<EntityId> LinkedEntityIds,
    string DetailRoute);
