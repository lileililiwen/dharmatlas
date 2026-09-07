using Dharmatlas.Domain;
using Dharmatlas.Domain.ValueObjects;
using Xunit;

namespace Dharmatlas.Domain.Tests;

public class HistoricalDateTests
{
    [Fact]
    public void Approximate_date_preserves_display_and_normalized_bounds()
    {
        var date = HistoricalDate.Approximate("c. 150 CE", lower: -160, upper: -140);

        Assert.Equal(HistoricalDateKind.Approximate, date.Kind);
        Assert.Equal("c. 150 CE", date.DisplayExpression);
        Assert.Equal(-160, date.NormalizedLowerBound);
        Assert.Equal(-140, date.NormalizedUpperBound);
    }

    [Fact]
    public void Interval_stores_both_bounds_and_does_not_collapse_to_one_year()
    {
        var date = HistoricalDate.Interval("150-250 CE", lower: -250, upper: -150);

        Assert.Equal(HistoricalDateKind.Interval, date.Kind);
        Assert.Equal(-250, date.NormalizedLowerBound);
        Assert.Equal(-150, date.NormalizedUpperBound);
        Assert.Equal("150-250 CE", date.DisplayExpression);
    }

    [Fact]
    public void Interval_supports_overlap_filtering_without_replacing_display()
    {
        var eventDate = HistoricalDate.Interval("150-250 CE", lower: -250, upper: -150);
        var queryLower = -300;
        var queryUpper = -200; // query window 300-200 BCE overlaps the event's 250-150 BCE

        var overlaps = eventDate.NormalizedLowerBound <= queryUpper
                       && eventDate.NormalizedUpperBound >= queryLower;

        Assert.True(overlaps);
        Assert.Equal("150-250 CE", eventDate.DisplayExpression);
    }

    [Fact]
    public void Inverted_range_is_rejected_at_the_domain_boundary()
    {
        Assert.Throws<DomainValidationException>(() =>
            HistoricalDate.Interval("250-150 CE", lower: -150, upper: -250));
    }

    [Fact]
    public void Traditional_date_preserves_expression_with_optional_bounds()
    {
        var date = HistoricalDate.Traditional("Year 3 of King X's reign", lower: null, upper: null);

        Assert.Equal(HistoricalDateKind.Traditional, date.Kind);
        Assert.Equal("Year 3 of King X's reign", date.DisplayExpression);
        Assert.Null(date.NormalizedLowerBound);
    }

    [Fact]
    public void Century_date_stores_spanning_bounds()
    {
        var date = HistoricalDate.Century("4th c. BCE", -400, -301);

        Assert.Equal(HistoricalDateKind.Century, date.Kind);
        Assert.Equal(-400, date.NormalizedLowerBound);
        Assert.Equal(-301, date.NormalizedUpperBound);
    }
}
