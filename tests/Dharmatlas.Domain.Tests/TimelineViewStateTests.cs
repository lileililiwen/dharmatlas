using Dharmatlas.Domain.ValueObjects;
using Dharmatlas.Timeline.Models;
using Xunit;

namespace Dharmatlas.Domain.Tests;

public class TimelineViewStateTests
{
    private static TimelineResult ResultWith(int count) =>
        new(new TimelineQuery(), new List<EventSummary>());

    [Fact]
    public void Empty_result_maps_to_empty_state()
    {
        var state = TimelineViewStates.From(new TimelineResult(new TimelineQuery(), Array.Empty<EventSummary>()));

        Assert.IsType<TimelineEmpty>(state);
    }

    [Fact]
    public void Non_empty_result_maps_to_ready_state()
    {
        var events = new List<EventSummary>
        {
            new(EntityId.New(), "Council", "650 CE", Certainty.Documented, "council", "China", Array.Empty<EntityId>(), "/events/1")
        };
        var state = TimelineViewStates.From(new TimelineResult(new TimelineQuery(), events));

        var ready = Assert.IsType<TimelineReady>(state);
        Assert.Single(ready.Result.Events);
    }

    [Fact]
    public void Error_state_carries_message()
    {
        var state = TimelineViewStates.Error("source unavailable");

        Assert.Equal("source unavailable", Assert.IsType<TimelineError>(state).Message);
    }
}
