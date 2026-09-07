using Dharmatlas.Map.Models;

namespace Dharmatlas.Map.Models;

/// <summary>
/// Presentation state for the map surface. When tiles or rendering are
/// unavailable, the surface degrades to a readable feature list (the non-visual
/// equivalent) while still allowing a retry of the map.
/// </summary>
public abstract record MapViewState;

public sealed record MapLoading : MapViewState;

public sealed record MapReady(MapResult Result) : MapViewState;

/// <summary>Map tiles/rendering failed; the user can still inspect the list.</summary>
public sealed record MapListFallback(MapResult Result) : MapViewState;

public sealed record MapError(string Message) : MapViewState;

public static class MapViewStates
{
    /// <summary>Normal ready state.</summary>
    public static MapViewState Ready(MapResult result) => new MapReady(result);

    /// <summary>Fallback used when the map renderer is unavailable.</summary>
    public static MapViewState Fallback(MapResult result) => new MapListFallback(result);

    public static MapViewState Error(string message) => new MapError(message);
}
