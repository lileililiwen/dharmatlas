namespace Dharmatlas.Map.Models;

/// <summary>
/// The bounded result of a map query: the matched features plus the viewport
/// they span. The viewport is stable and bounded so the rendering surface can
/// frame the result without unbounded panning.
/// </summary>
public sealed record MapResult(MapQuery Query, IReadOnlyList<MapFeature> Features, ViewportBounds Viewport);

/// <summary>Inclusive geographic bounds enclosing the result features.</summary>
public sealed record ViewportBounds(double MinLatitude, double MaxLatitude, double MinLongitude, double MaxLongitude);
