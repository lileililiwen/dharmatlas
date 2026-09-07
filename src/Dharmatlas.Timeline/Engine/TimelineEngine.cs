using Dharmatlas.Domain;
using Dharmatlas.Domain.Entities;
using Dharmatlas.Domain.ValueObjects;
using Dharmatlas.Timeline.Models;

namespace Dharmatlas.Timeline.Engine;

/// <summary>
/// Pure, side-effect-free timeline logic operating over the domain model. Kept
/// independent of EF Core so the overlap, filter, and projection rules can be
/// unit tested without a database. The query service applies these rules to a
/// materialized event set.
/// </summary>
public static class TimelineEngine
{
    private const int UnboundedLow = int.MinValue;
    private const int UnboundedHigh = int.MaxValue;

    /// <summary>
    /// True when an event's normalized bounds overlap the query window. Open
    /// bounds (only a lower or only an upper) are treated as unbounded on the
    /// missing side. Events with no bounds at all are not "overlapping" here;
    /// they are handled separately via <see cref="TimelineQuery.IncludeUnknownDates"/>.
    /// </summary>
    public static bool Overlaps(HistoricalDate? when, int? fromYear, int? toYear)
    {
        if (when is null)
        {
            return false;
        }

        if (when.NormalizedLowerBound is null && when.NormalizedUpperBound is null)
        {
            return false;
        }

        var lower = when.NormalizedLowerBound ?? UnboundedLow;
        var upper = when.NormalizedUpperBound ?? UnboundedHigh;
        var queryFrom = fromYear ?? UnboundedLow;
        var queryTo = toYear ?? UnboundedHigh;

        return lower <= queryTo && upper >= queryFrom;
    }

    /// <summary>
    /// Whether the event matches the query: date overlap (or unknown-date opt-in)
    /// plus category and region filters.
    /// </summary>
    public static bool Matches(Event e, TimelineQuery query)
    {
        var hasBounds = e.When is not null &&
                        (e.When.NormalizedLowerBound is not null || e.When.NormalizedUpperBound is not null);

        if (!hasBounds)
        {
            return query.IncludeUnknownDates;
        }

        if (!Overlaps(e.When, query.FromYear, query.ToYear))
        {
            return false;
        }

        if (query.Categories is { Count: > 0 } &&
            (e.Category is null || !query.Categories.Contains(e.Category)))
        {
            return false;
        }

        if (query.Regions is { Count: > 0 } &&
            (e.Region is null || !query.Regions.Contains(e.Region)))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Projects a domain event into a read-only summary for the timeline surface,
    /// preserving the authored date expression and certainty state.
    /// </summary>
    public static EventSummary Project(Event e)
    {
        var linked = e.PlaceId.HasValue
            ? new List<EntityId> { e.PlaceId.Value }
            : new List<EntityId>();

        return new EventSummary(
            e.Id,
            TitleOf(e),
            e.When?.DisplayExpression ?? "Unknown date",
            e.Certainty,
            e.Category,
            e.Region,
            linked,
            $"/events/{e.Id}");
    }

    private static string TitleOf(Event e)
    {
        var name = e.Names.FirstOrDefault(n => n.IsPrimary) ?? e.Names.FirstOrDefault();
        return name?.Value ?? e.When?.DisplayExpression ?? "Untitled event";
    }
}
