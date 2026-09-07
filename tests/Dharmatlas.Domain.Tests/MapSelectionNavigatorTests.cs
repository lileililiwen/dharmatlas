using Dharmatlas.Domain;
using Dharmatlas.Domain.Entities;
using Dharmatlas.Domain.ValueObjects;
using Dharmatlas.Map.Engine;
using Dharmatlas.Map.Models;
using Dharmatlas.Map.Navigation;
using Xunit;

namespace Dharmatlas.Domain.Tests;

public class MapSelectionNavigatorTests
{
    private static IReadOnlyList<MapFeature> Sample()
    {
        var place = new Place
        {
            Latitude = 34.0,
            Longitude = 113.0,
            Kind = PlaceKind.ArchaeologicalSite,
            Certainty = Certainty.Probable,
            Activity = HistoricalDate.Approximate("c. 650 CE", 600, 700),
            SourceIds = new[] { EntityId.New() }
        };
        place.AddName(new EntityName(place.Id, "eng", "latin", "Wylie", "Site X"));
        return new[] { MapEngine.ProjectPlace(place) };
    }

    [Fact]
    public void Selecting_a_feature_exposes_detail_navigation()
    {
        var nav = new MapSelectionNavigator(Sample());

        Assert.Equal("/places/" + nav.Focus(0)!.Id, nav.DetailRoute(0));
    }

    [Fact]
    public void Provenance_label_is_non_visual_equivalent()
    {
        var nav = new MapSelectionNavigator(Sample());

        Assert.Contains("Site X", nav.ProvenanceLabel(0));
        Assert.Contains("ArchaeologicalSite", nav.ProvenanceLabel(0));
        Assert.Contains("Probable", nav.ProvenanceLabel(0));
        Assert.Contains("1 source(s)", nav.ProvenanceLabel(0));
    }

    [Fact]
    public void Navigation_stays_within_bounds()
    {
        var nav = new MapSelectionNavigator(Sample());

        Assert.Equal(0, nav.Previous(0));
        Assert.Equal(0, nav.Next(0));
        Assert.Null(nav.Focus(5));
    }
}
