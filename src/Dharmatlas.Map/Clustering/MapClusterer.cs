using Dharmatlas.Map.Models;

namespace Dharmatlas.Map.Clustering;

/// <summary>
/// Groups nearby features into grid cells for low-zoom clustering. A cluster
/// keeps its member features so the surface can expand it on zoom-in. Pure and
/// testable; the visual layer decides when to render clusters vs individual
/// typed points.
/// </summary>
public sealed record MapCluster(GeoPoint Center, IReadOnlyList<MapFeature> Members)
{
    public int Count => Members.Count;
}

public static class MapClusterer
{
    /// <summary>
    /// Clusters features by a square grid of <paramref name="cellSizeDegrees"/>.
    /// Features in the same cell share a cluster; the cluster center is the cell
    /// midpoint. Returns an empty list when there are no features.
    /// </summary>
    public static IReadOnlyList<MapCluster> Cluster(IEnumerable<MapFeature> features, double cellSizeDegrees)
    {
        if (cellSizeDegrees <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cellSizeDegrees), "Cell size must be positive.");
        }

        var groups = new Dictionary<(int, int), List<MapFeature>>();
        foreach (var feature in features)
        {
            var rep = feature.Geometry.FirstOrDefault();
            if (rep is null)
            {
                continue;
            }

            var key = (Cell(rep.Latitude, cellSizeDegrees), Cell(rep.Longitude, cellSizeDegrees));
            if (!groups.TryGetValue(key, out var list))
            {
                list = new List<MapFeature>();
                groups[key] = list;
            }

            list.Add(feature);
        }

        var clusters = new List<MapCluster>();
        foreach (var (key, members) in groups)
        {
            var (latCell, lngCell) = key;
            var center = new GeoPoint(
                (latCell + 0.5) * cellSizeDegrees,
                (lngCell + 0.5) * cellSizeDegrees);
            clusters.Add(new MapCluster(center, members));
        }

        return clusters;
    }

    private static int Cell(double coordinate, double cellSize) =>
        (int)Math.Floor(coordinate / cellSize);
}
