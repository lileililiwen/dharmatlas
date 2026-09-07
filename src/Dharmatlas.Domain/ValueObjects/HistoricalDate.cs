namespace Dharmatlas.Domain.ValueObjects;

/// <summary>
/// The kind of historical date. BCE years are stored as negative integers
/// (e.g. 327 BCE -> -327) so ranges remain comparable.
/// </summary>
public enum HistoricalDateKind
{
    Exact,
    Approximate,
    Century,
    Interval,
    OpenInterval,
    Traditional
}

/// <summary>
/// Uncertainty-aware date. The display expression is preserved exactly as
/// authored (e.g. "c. 150 CE" or "Year 3 of King X's reign") while optional
/// normalized bounds support range filtering. The system must never force false
/// precision: an interval keeps both bounds and is never reduced to a single year.
/// </summary>
public sealed record HistoricalDate
{
    public HistoricalDateKind Kind { get; } = HistoricalDateKind.Exact;
    public string DisplayExpression { get; } = string.Empty;
    public int? NormalizedLowerBound { get; }
    public int? NormalizedUpperBound { get; }

    // Parameterless constructor for EF Core materialization; stored data is
    // assumed valid, so validation is not re-run here.
    private HistoricalDate() { }

    private HistoricalDate(
        HistoricalDateKind kind,
        string displayExpression,
        int? lower,
        int? upper)
    {
        if (string.IsNullOrWhiteSpace(displayExpression))
        {
            throw new DomainValidationException("A historical date requires a display expression.");
        }

        if (kind == HistoricalDateKind.Interval)
        {
            if (lower is null || upper is null)
            {
                throw new DomainValidationException("An interval date requires both normalized bounds.");
            }
        }

        if (lower is not null && upper is not null && lower.Value > upper.Value)
        {
            throw new DomainValidationException(
                $"Inverted date range: lower bound {lower} is after upper bound {upper}.");
        }

        Kind = kind;
        DisplayExpression = displayExpression;
        NormalizedLowerBound = lower;
        NormalizedUpperBound = upper;
    }

    public static HistoricalDate Exact(string display, int year) =>
        new(HistoricalDateKind.Exact, display, year, year);

    public static HistoricalDate Approximate(string display, int? lower, int? upper)
    {
        if (lower is null && upper is null)
        {
            throw new DomainValidationException("An approximate date needs at least one normalized bound.");
        }

        return new(HistoricalDateKind.Approximate, display, lower, upper);
    }

    public static HistoricalDate Century(string display, int centuryStartYear, int centuryEndYear) =>
        new(HistoricalDateKind.Century, display, centuryStartYear, centuryEndYear);

    public static HistoricalDate Interval(string display, int lower, int upper) =>
        new(HistoricalDateKind.Interval, display, lower, upper);

    public static HistoricalDate OpenInterval(string display, int? lower, int? upper)
    {
        if (lower is null && upper is null)
        {
            throw new DomainValidationException("An open interval needs at least one bound.");
        }

        return new(HistoricalDateKind.OpenInterval, display, lower, upper);
    }

    public static HistoricalDate Traditional(string display, int? lower, int? upper) =>
        new(HistoricalDateKind.Traditional, display, lower, upper);
}
