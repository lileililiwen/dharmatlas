namespace Dharmatlas.Timeline.Models;

/// <summary>
/// Parameters for a timeline query. Years use BCE as negatives (e.g. 600 BCE is
/// -600). Null bounds mean "unbounded". Unknown-date events (no normalized
/// bounds) are only returned when <see cref="IncludeUnknownDates"/> is set,
/// keeping them discoverable without inventing exact years.
/// </summary>
public sealed record TimelineQuery
{
    /// <summary>Inclusive lower bound year, or null for no lower bound.</summary>
    public int? FromYear { get; init; }

    /// <summary>Inclusive upper bound year, or null for no upper bound.</summary>
    public int? ToYear { get; init; }

    public IReadOnlyList<string>? Categories { get; init; }
    public IReadOnlyList<string>? Regions { get; init; }

    /// <summary>
    /// When true, events whose dates have no normalized bounds are included
    /// (approximate/traditional/unknown that could not be normalized).
    /// </summary>
    public bool IncludeUnknownDates { get; init; }

    public int? Limit { get; init; } = 200;
}
