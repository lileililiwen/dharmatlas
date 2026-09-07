using Dharmatlas.Domain.Entities;
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
        var events = await _db.Entities
            .OfType<Event>()
            .ToListAsync(cancellationToken);

        var matched = events
            .Where(e => TimelineEngine.Matches(e, query))
            .Select(TimelineEngine.Project)
            .Take(query.Limit ?? 200)
            .ToList();

        return new TimelineResult(query, matched);
    }
}
