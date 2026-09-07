using Dharmatlas.Timeline.Models;

namespace Dharmatlas.Timeline.Services;

/// <summary>
/// Read-only timeline query contract. Consumes the historical data model and
/// returns projected event summaries for the timeline surface.
/// </summary>
public interface ITimelineQueryService
{
    Task<TimelineResult> QueryAsync(TimelineQuery query, CancellationToken cancellationToken = default);
}
