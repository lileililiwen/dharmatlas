using Dharmatlas.Timeline.Models;

namespace Dharmatlas.Timeline.Models;

/// <summary>
/// Presentation state machine for the timeline surface. The read-only timeline
/// distinguishes an in-flight load, an empty result for the active filters, a
/// recoverable error, and a ready result. This is the non-visual equivalent the
/// UI renders; it is testable without a browser.
/// </summary>
public abstract record TimelineViewState;

public sealed record TimelineLoading : TimelineViewState;

public sealed record TimelineEmpty : TimelineViewState;

public sealed record TimelineError(string Message) : TimelineViewState;

public sealed record TimelineReady(TimelineResult Result) : TimelineViewState;

public static class TimelineViewStates
{
    public static TimelineViewState From(TimelineResult result) =>
        result.Events.Count == 0 ? new TimelineEmpty() : new TimelineReady(result);

    public static TimelineViewState Error(string message) => new TimelineError(message);
}
