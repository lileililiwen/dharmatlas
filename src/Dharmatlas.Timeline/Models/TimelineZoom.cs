namespace Dharmatlas.Timeline.Models;

/// <summary>
/// Practical zoom presets for the timeline (design: 1000/500/100/20/1 years).
/// The visual surface renders the computed sub-range; this logic is the
/// testable contract the UI consumes.
/// </summary>
public enum TimelineZoomPreset
{
    ThousandYears = 1000,
    FiveHundredYears = 500,
    HundredYears = 100,
    TwentyYears = 20,
    SingleYear = 1
}

public static class TimelineZoom
{
    /// <summary>
    /// Returns the inclusive [from, to] year range centered on
    /// <paramref name="centerYear"/> spanning the given preset width.
    /// </summary>
    public static (int From, int To) RangeAround(int centerYear, TimelineZoomPreset preset)
    {
        var half = ((int)preset) / 2;
        return (centerYear - half, centerYear + half);
    }
}
