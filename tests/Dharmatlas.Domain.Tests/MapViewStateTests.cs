using Dharmatlas.Domain;
using Dharmatlas.Domain.ValueObjects;
using Dharmatlas.Map.Engine;
using Dharmatlas.Map.Models;
using Dharmatlas.Domain.Entities;
using Xunit;

namespace Dharmatlas.Domain.Tests;

public class MapViewStateTests
{
    private static MapResult Sample() =>
        new(new MapQuery { Year = 650 },
            new[] { MapEngine.ProjectPlace(new Place { Latitude = 34.0, Longitude = 113.0, Activity = HistoricalDate.Exact("650 CE", 650) }) },
            new ViewportBounds(34, 34, 113, 113));

    [Fact]
    public void Ready_state_carries_result()
    {
        var state = MapViewStates.Ready(Sample());
        var ready = Assert.IsType<MapReady>(state);
        Assert.Single(ready.Result.Features);
    }

    [Fact]
    public void Tile_failure_degrades_to_list_fallback()
    {
        var state = MapViewStates.Fallback(Sample());

        var fallback = Assert.IsType<MapListFallback>(state);
        Assert.Single(fallback.Result.Features); // user can still inspect the list
    }

    [Fact]
    public void Error_state_carries_message()
    {
        var state = MapViewStates.Error("tiles unavailable");
        Assert.Equal("tiles unavailable", Assert.IsType<MapError>(state).Message);
    }
}
