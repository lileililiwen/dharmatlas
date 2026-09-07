using Dharmatlas.Map.Models;

namespace Dharmatlas.Map.Navigation;

/// <summary>
/// Keyboard/pointer selection model for map features. Exposes the focused
/// feature's detail route and a non-visual provenance summary (type, activity,
/// certainty, source count) so feature inspection works without the rendered map.
/// </summary>
public sealed class MapSelectionNavigator
{
    private readonly IReadOnlyList<MapFeature> _features;

    public MapSelectionNavigator(IReadOnlyList<MapFeature> features) => _features = features;

    public int Count => _features.Count;

    public MapFeature? Focus(int index) =>
        index >= 0 && index < _features.Count ? _features[index] : null;

    public int Next(int current) => Math.Min(current + 1, Math.Max(_features.Count - 1, 0));

    public int Previous(int current) => Math.Max(current - 1, 0);

    public string? DetailRoute(int index) => Focus(index)?.DetailRoute;

    /// <summary>Non-visual equivalent of inspecting a feature's provenance.</summary>
    public string ProvenanceLabel(int index)
    {
        var f = Focus(index);
        return f is null
            ? string.Empty
            : $"Feature {index + 1} of {_features.Count}: {f.Title}, {f.Kind ?? f.Type.ToString()}, {f.ActivityExpression}, " +
              $"{f.Certainty}, {f.SourceIds.Count} source(s).";
    }
}
