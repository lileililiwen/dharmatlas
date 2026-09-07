using Dharmatlas.Domain;
using Dharmatlas.Domain.Entities;
using Dharmatlas.Domain.ValueObjects;
using Dharmatlas.Map.Engine;
using Dharmatlas.Map.Models;
using Xunit;

namespace Dharmatlas.Domain.Tests;

public class MapEngineTests
{
    private static Place ActivePlace(int year, string name = "Luoyang", PlaceKind kind = PlaceKind.City)
    {
        var p = new Place
        {
            Latitude = 34.0,
            Longitude = 113.0,
            Kind = kind,
            Region = "China",
            Certainty = Certainty.Documented,
            Activity = HistoricalDate.Interval("600-700 CE", 600, 700)
        };
        p.AddName(new EntityName(p.Id, "eng", "latin", "Wylie", name));
        return p;
    }

    [Fact]
    public void Selecting_650_CE_shows_active_place()
    {
        var result = MapEngine.Query(new MapQuery { Year = 650 }, new[] { ActivePlace(650) }, Array.Empty<Institution>());

        Assert.Single(result.Features);
        Assert.Equal(MapFeatureType.Place, result.Features[0].Type);
    }

    [Fact]
    public void Place_inactive_in_selected_year_is_excluded()
    {
        var result = MapEngine.Query(new MapQuery { Year = 900 }, new[] { ActivePlace(650) }, Array.Empty<Institution>());

        Assert.Empty(result.Features);
    }

    [Fact]
    public void Approximate_activity_remains_visible_with_certainty()
    {
        var place = new Place
        {
            Latitude = 34.0,
            Longitude = 113.0,
            Kind = PlaceKind.ArchaeologicalSite,
            Certainty = Certainty.Probable,
            Activity = HistoricalDate.Approximate("c. 650 CE", 600, 700)
        };
        place.AddName(new EntityName(place.Id, "eng", "latin", "Wylie", "Site X"));

        var result = MapEngine.Query(new MapQuery { Year = 650 }, new[] { place }, Array.Empty<Institution>());

        var feature = Assert.Single(result.Features);
        Assert.Equal("c. 650 CE", feature.ActivityExpression); // not collapsed to an exact year
        Assert.Equal(Certainty.Probable, feature.Certainty);
        Assert.Equal("ArchaeologicalSite", feature.Kind);
    }

    [Fact]
    public void Unknown_activity_excluded_unless_opted_in()
    {
        var place = new Place
        {
            Latitude = 34.0,
            Longitude = 113.0,
            Activity = HistoricalDate.Traditional("Year 3 of King X", lower: null, upper: null)
        };
        place.AddName(new EntityName(place.Id, "eng", "latin", "Wylie", "Traditional Site"));

        Assert.Empty(MapEngine.Query(new MapQuery { Year = 650 }, new[] { place }, Array.Empty<Institution>()).Features);
        Assert.Single(MapEngine.Query(new MapQuery { Year = 650, IncludeUnknownActivity = true }, new[] { place }, Array.Empty<Institution>()).Features);
    }

    [Fact]
    public void Institution_is_georeferenced_via_its_place()
    {
        var place = ActivePlace(650, "Luoyang");
        var institution = new Institution
        {
            PlaceId = place.Id,
            Activity = HistoricalDate.Exact("650 CE", 650),
            Certainty = Certainty.Documented,
            SourceIds = new[] { EntityId.New() }
        };
        institution.AddName(new EntityName(institution.Id, "eng", "latin", "Wylie", "White Horse Monastery"));

        var result = MapEngine.Query(new MapQuery { Year = 650 }, new[] { place }, new[] { institution });

        var feature = Assert.Single(result.Features, f => f.Type == MapFeatureType.Institution);
        Assert.Single(feature.Geometry); // located at the place's coordinates
        Assert.Single(feature.SourceIds);
    }

    [Fact]
    public void Route_is_projected_as_a_line_feature()
    {
        var route = MapEngine.BuildRoute(
            EntityId.New(),
            "Silk Road segment",
            new GeoPoint(34.0, 113.0),
            new GeoPoint(35.0, 114.0),
            HistoricalDate.Interval("100-800 CE", 100, 800),
            Certainty.TraditionalAccount,
            Array.Empty<EntityId>());

        Assert.Equal(MapFeatureType.Route, route.Type);
        Assert.True(route.Geometry.Count == 2);
    }

    [Fact]
    public void Region_and_type_filters_narrow_results()
    {
        var china = ActivePlace(650, "Luoyang", PlaceKind.City);
        var india = new Place
        {
            Latitude = 28.0,
            Longitude = 84.0,
            Kind = PlaceKind.City,
            Region = "India",
            Activity = HistoricalDate.Exact("650 CE", 650)
        };
        india.AddName(new EntityName(india.Id, "eng", "latin", "Wylie", "Nalanda"));

        var result = MapEngine.Query(
            new MapQuery { Year = 650, Region = "China", Types = new[] { MapFeatureType.Place } },
            new[] { china, india },
            Array.Empty<Institution>());

        Assert.Single(result.Features);
        Assert.Equal("China", result.Features[0].Region);
    }

    [Fact]
    public void Viewport_encloses_all_result_features()
    {
        var a = new Place { Latitude = 28.0, Longitude = 84.0, Activity = HistoricalDate.Exact("650 CE", 650) };
        var b = new Place { Latitude = 34.0, Longitude = 113.0, Activity = HistoricalDate.Exact("650 CE", 650) };
        var result = MapEngine.Query(new MapQuery { Year = 650 }, new[] { a, b }, Array.Empty<Institution>());

        Assert.Equal(28.0, result.Viewport.MinLatitude);
        Assert.Equal(34.0, result.Viewport.MaxLatitude);
        Assert.Equal(84.0, result.Viewport.MinLongitude);
        Assert.Equal(113.0, result.Viewport.MaxLongitude);
    }
}
