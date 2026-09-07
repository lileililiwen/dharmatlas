using Dharmatlas.Domain;
using Dharmatlas.Map.Clustering;
using Dharmatlas.Map.Engine;
using Dharmatlas.Map.Models;
using Dharmatlas.Domain.ValueObjects;
using Dharmatlas.Domain.Entities;
using Xunit;

namespace Dharmatlas.Domain.Tests;

public class MapClustererTests
{
    private static MapFeature Point(string name, double lat, double lng)
    {
        var place = new Place
        {
            Latitude = lat,
            Longitude = lng,
            Activity = HistoricalDate.Exact("650 CE", 650)
        };
        place.AddName(new EntityName(EntityId.New(), "eng", "latin", "Wylie", name));
        return MapEngine.ProjectPlace(place);
    }

    [Fact]
    public void Nearby_features_cluster_together()
    {
        var features = new[] { Point("A", 34.0, 113.0), Point("B", 34.1, 113.1) };

        var clusters = MapClusterer.Cluster(features, cellSizeDegrees: 1.0);

        Assert.Single(clusters);
        Assert.True(clusters[0].Count == 2);
    }

    [Fact]
    public void Distant_features_form_separate_clusters()
    {
        var features = new[] { Point("A", 34.0, 113.0), Point("B", 50.0, 130.0) };

        var clusters = MapClusterer.Cluster(features, cellSizeDegrees: 1.0);

        Assert.Equal(2, clusters.Count);
        Assert.All(clusters, c => Assert.True(c.Count == 1));
    }

    [Fact]
    public void Non_positive_cell_size_is_rejected()
    {
        Assert.Throws<System.ArgumentOutOfRangeException>(() =>
            MapClusterer.Cluster(Array.Empty<MapFeature>(), cellSizeDegrees: 0));
    }
}
