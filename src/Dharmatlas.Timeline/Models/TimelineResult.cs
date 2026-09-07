namespace Dharmatlas.Timeline.Models;

/// <summary>
/// The result of a timeline query: the matched event summaries plus the query
/// that produced them (so the UI can reflect active filters).
/// </summary>
public sealed record TimelineResult(TimelineQuery Query, IReadOnlyList<EventSummary> Events);
