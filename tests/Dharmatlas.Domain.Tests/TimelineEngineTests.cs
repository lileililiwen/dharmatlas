using Dharmatlas.Domain;
using Dharmatlas.Domain.Entities;
using Dharmatlas.Domain.ValueObjects;
using Dharmatlas.Timeline.Engine;
using Dharmatlas.Timeline.Models;
using Xunit;

namespace Dharmatlas.Domain.Tests;

public class TimelineEngineTests
{
    private static Event ChineseCouncil()
    {
        var e = new Event
        {
            Category = "council",
            Region = "China",
            Certainty = Certainty.Documented,
            When = HistoricalDate.Interval("650-680 CE", 650, 680)
        };
        e.AddName(new EntityName(e.Id, "eng", "latin", "Wylie", "Council of X"));
        return e;
    }

    [Fact]
    public void Region_and_period_filter_returns_overlapping_events()
    {
        var query = new TimelineQuery { FromYear = 600, ToYear = 700, Regions = new[] { "China" } };

        Assert.True(TimelineEngine.Matches(ChineseCouncil(), query));
    }

    [Fact]
    public void Event_outside_period_is_excluded()
    {
        var query = new TimelineQuery { FromYear = 600, ToYear = 700, Regions = new[] { "China" } };
        var late = new Event
        {
            Region = "China",
            When = HistoricalDate.Exact("900 CE", 900)
        };

        Assert.False(TimelineEngine.Matches(late, query));
    }

    [Fact]
    public void Region_filter_excludes_other_regions()
    {
        var query = new TimelineQuery { FromYear = 600, ToYear = 700, Regions = new[] { "China" } };
        var india = new Event { Region = "India", When = HistoricalDate.Exact("650 CE", 650) };

        Assert.False(TimelineEngine.Matches(india, query));
    }

    [Fact]
    public void Open_upper_bound_overlaps_when_lower_is_within_window()
    {
        var query = new TimelineQuery { FromYear = 100, ToYear = 300 };
        var after = new Event { When = HistoricalDate.OpenInterval("after 150 CE", lower: 150, upper: null) };

        Assert.True(TimelineEngine.Matches(after, query));
    }

    [Fact]
    public void Unknown_dates_are_excluded_unless_opted_in()
    {
        var unknown = new Event
        {
            Region = "China",
            When = HistoricalDate.Traditional("Year 3 of King X", lower: null, upper: null)
        };

        Assert.False(TimelineEngine.Matches(unknown, new TimelineQuery { Regions = new[] { "China" } }));
        Assert.True(TimelineEngine.Matches(unknown,
            new TimelineQuery { Regions = new[] { "China" }, IncludeUnknownDates = true }));
    }

    [Fact]
    public void Approximate_event_preserves_display_and_certainty_in_projection()
    {
        var evt = new Event
        {
            Category = "translation",
            Region = "India",
            Certainty = Certainty.Probable,
            When = HistoricalDate.Approximate("c. 150 CE", lower: -160, upper: -140)
        };
        evt.AddName(new EntityName(evt.Id, "eng", "latin", "Wylie", "First Translation"));

        var summary = TimelineEngine.Project(evt);

        Assert.Equal("c. 150 CE", summary.DisplayDate);
        Assert.Equal(Certainty.Probable, summary.Certainty);
        Assert.Equal("First Translation", summary.Title);
        Assert.Equal("/events/" + evt.Id, summary.DetailRoute);
    }

    [Fact]
    public void Category_filter_narrows_results()
    {
        var query = new TimelineQuery
        {
            FromYear = -600,
            ToYear = 1000,
            Categories = new[] { "council" }
        };

        Assert.True(TimelineEngine.Matches(ChineseCouncil(), query));
        Assert.False(TimelineEngine.Matches(
            new Event { Category = "birth", When = HistoricalDate.Exact("650 CE", 650) }, query));
    }
}
