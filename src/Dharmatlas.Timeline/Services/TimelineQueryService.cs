using Dharmatlas.Domain.Entities;
using Dharmatlas.Domain;
using Dharmatlas.Persistence;
using Dharmatlas.Timeline.Engine;
using Dharmatlas.Timeline.Models;
using Microsoft.EntityFrameworkCore;

namespace Dharmatlas.Timeline.Services;

/// <summary>
/// Applies the timeline engine to events loaded from the data model. The query
/// is read-only: it does not mutate entities or duplicate date/source semantics.
/// </summary>
public sealed class TimelineQueryService : ITimelineQueryService
{
    private readonly DharmatlasDbContext _db;

    public TimelineQueryService(DharmatlasDbContext db) => _db = db;

    public async Task<TimelineResult> QueryAsync(TimelineQuery query, CancellationToken cancellationToken = default)
    {
        var limit = Math.Clamp(query.Limit ?? 200, 1, 500);
        var eventsQuery = _db.Entities.OfType<Event>().AsNoTracking();
        if (query.Categories is { Count: > 0 }) eventsQuery = eventsQuery.Where(e => e.Category != null && query.Categories.Contains(e.Category));
        if (query.Regions is { Count: > 0 }) eventsQuery = eventsQuery.Where(e => e.Region != null && query.Regions.Contains(e.Region));
        if (query.FromYear is { } from) eventsQuery = eventsQuery.Where(e => e.When == null || e.When.NormalizedUpperBound == null || e.When.NormalizedUpperBound >= from);
        if (query.ToYear is { } to) eventsQuery = eventsQuery.Where(e => e.When == null || e.When.NormalizedLowerBound == null || e.When.NormalizedLowerBound <= to);
        if (!query.IncludeUnknownDates) eventsQuery = eventsQuery.Where(e => e.When != null && (e.When.NormalizedLowerBound != null || e.When.NormalizedUpperBound != null));

        var events = await eventsQuery.OrderBy(e => e.Id.Value).Take(limit).ToListAsync(cancellationToken);
        var names = await _db.EntityNames.AsNoTracking().Where(n => events.Select(e => e.Id).Contains(n.EntityId)).ToListAsync(cancellationToken);
        var namesByEvent = names.GroupBy(n => n.EntityId).ToDictionary(g => g.Key, g => EntityNameReadModel.CanonicalName(g));

        var matched = events
            .Where(e => TimelineEngine.Matches(e, query))
            .Select(e => new EventSummary(
                e.Id,
                namesByEvent.GetValueOrDefault(e.Id) ?? e.When?.DisplayExpression ?? "Untitled event",
                e.When?.DisplayExpression ?? "Unknown date",
                e.Certainty,
                e.Category,
                e.Region,
                e.PlaceId is { } placeId ? new[] { placeId } : Array.Empty<Dharmatlas.Domain.EntityId>(),
                $"/events/{e.Id}"))
            .ToList();

        return new TimelineResult(query, matched);
    }
}
