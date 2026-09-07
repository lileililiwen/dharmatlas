using Dharmatlas.Timeline.Models;
using Xunit;

namespace Dharmatlas.Domain.Tests;

public class TimelineZoomTests
{
    [Theory]
    [InlineData(TimelineZoomPreset.ThousandYears, 500, 0, 1000)]
    [InlineData(TimelineZoomPreset.FiveHundredYears, 500, 250, 750)]
    [InlineData(TimelineZoomPreset.HundredYears, 500, 450, 550)]
    [InlineData(TimelineZoomPreset.TwentyYears, 500, 490, 510)]
    [InlineData(TimelineZoomPreset.SingleYear, 500, 500, 500)]
    public void RangeAround_is_centered_on_preset_width(TimelineZoomPreset preset, int center, int from, int to)
    {
        var (actualFrom, actualTo) = TimelineZoom.RangeAround(center, preset);

        Assert.Equal(from, actualFrom);
        Assert.Equal(to, actualTo);
    }

    [Fact]
    public void Single_year_preset_spans_exactly_one_year()
    {
        var (from, to) = TimelineZoom.RangeAround(100, TimelineZoomPreset.SingleYear);

        Assert.Equal(100, from);
        Assert.Equal(100, to);
    }
}
