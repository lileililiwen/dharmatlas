namespace Dharmatlas.Map.Models;

/// <summary>
/// Parameters for a historical map query. The selected year uses BCE as
/// negatives (e.g. 650 CE is 650, 50 BCE is -50). Features whose recorded
/// activity interval overlaps that year are returned. Features with no recorded
/// activity bounds are only included when <see cref="IncludeUnknownActivity"/>
/// is set, keeping them discoverable without inventing exact dates.
/// </summary>
public sealed record MapQuery
{
    /// <summary>The selected historical year (BCE as negative).</summary>
    public int Year { get; init; }

    public IReadOnlyList<MapFeatureType>? Types { get; init; }

    public string? Region { get; init; }

    public double? MinLatitude { get; init; }
    public double? MaxLatitude { get; init; }
    public double? MinLongitude { get; init; }
    public double? MaxLongitude { get; init; }

    /// <summary>Include features whose activity has no normalized bounds.</summary>
    public bool IncludeUnknownActivity { get; init; }
}
