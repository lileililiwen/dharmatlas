using System.Globalization;
using Dharmatlas.Domain;
using Dharmatlas.Domain.Entities;
using Dharmatlas.Domain.ValueObjects;
using Dharmatlas.Map.Models;

namespace Dharmatlas.Map.Engine;

/// <summary>
/// Pure, side-effect-free map logic over the historical data model. Kept
/// independent of EF Core and MapLibre so time filtering, provenance projection,
/// and clustering can be unit tested without a database or map renderer.
/// </summary>
public static class MapEngine
{
    private const int UnboundedLow = int.MinValue;
    private const int UnboundedHigh = int.MaxValue;

    /// <summary>
    /// True when the recorded activity interval contains <paramref name="year"/>.
    /// Open bounds are unbounded on the missing side. Activity with no bounds is
    /// not "active" here; it is handled via <see cref="MapQuery.IncludeUnknownActivity"/>.
    /// </summary>
    public static bool IsActive(HistoricalDate? activity, int year)
    {
        if (activity is null)
        {
            return false;
        }

        if (activity.NormalizedLowerBound is null && activity.NormalizedUpperBound is null)
        {
            return false;
        }

        var lower = activity.NormalizedLowerBound ?? UnboundedLow;
        var upper = activity.NormalizedUpperBound ?? UnboundedHigh;
        return lower <= year && year <= upper;
    }

    public static bool HasUnknownActivity(HistoricalDate? activity) =>
        activity is not null && activity.NormalizedLowerBound is null && activity.NormalizedUpperBound is null;

    private static bool PassesYear(HistoricalDate? activity, MapQuery query) =>
        IsActive(activity, query.Year) || (query.IncludeUnknownActivity && HasUnknownActivity(activity));

    private static bool PassesFilters(MapFeature feature, MapQuery query)
    {
        if (query.Types is { Count: > 0 } && !query.Types.Contains(feature.Type))
        {
            return false;
        }

        if (query.Region is not null && !string.Equals(feature.Region, query.Region, StringComparison.Ordinal))
        {
            return false;
        }

        var point = feature.Geometry.FirstOrDefault();
        if (point is null && (query.MinLatitude is not null || query.MaxLatitude is not null ||
            query.MinLongitude is not null || query.MaxLongitude is not null) ||
            point is not null && (query.MinLatitude is not null && point.Latitude < query.MinLatitude ||
            query.MaxLatitude is not null && point.Latitude > query.MaxLatitude ||
            query.MinLongitude is not null && point.Longitude < query.MinLongitude ||
            query.MaxLongitude is not null && point.Longitude > query.MaxLongitude))
        {
            return false;
        }

        return true;
    }

    public static MapFeature ProjectPlace(Place place)
    {
        var point = new GeoPoint(place.Latitude ?? 0, place.Longitude ?? 0);
        return new MapFeature(
            place.Id,
            MapFeatureType.Place,
            TitleOf(place),
            place.Activity?.DisplayExpression ?? "Unknown activity",
            place.Certainty,
            place.SourceIds,
            new[] { point },
            place.Kind.ToString(),
            place.Region,
            $"/places/{place.Id}");
    }

    public static MapFeature ProjectInstitution(Institution institution, GeoPoint? location)
    {
        return new MapFeature(
            institution.Id,
            MapFeatureType.Institution,
            TitleOf(institution),
            institution.Activity?.DisplayExpression ?? "Unknown activity",
            institution.Certainty,
            institution.SourceIds,
            location is null ? Array.Empty<GeoPoint>() : new[] { location },
            institution.InstitutionalForm,
            Region: null,
            $"/institutions/{institution.Id}");
    }

    /// <summary>
    /// Builds a route feature as a line between two coordinates. Used for
    /// routes/trade paths; the surface renders it as a typed line layer.
    /// </summary>
    public static MapFeature BuildRoute(
        EntityId id,
        string title,
        GeoPoint from,
        GeoPoint to,
        HistoricalDate? activity,
        Certainty certainty,
        IReadOnlyList<EntityId> sourceIds)
    {
        return new MapFeature(
            id,
            MapFeatureType.Route,
            title,
            activity?.DisplayExpression ?? "Unknown activity",
            certainty,
            sourceIds,
            new[] { from, to },
            Kind: "Route",
            Region: null,
            $"/routes/{id}");
    }

    /// <summary>Selects and projects the time-filtered, bounded feature set.</summary>
    public static MapResult Query(
        MapQuery query,
        IReadOnlyList<Place> places,
        IReadOnlyList<Institution> institutions)
    {
        var placeLookup = places
            .Where(p => p.Latitude.HasValue && p.Longitude.HasValue)
            .ToDictionary(p => p.Id, p => new GeoPoint(p.Latitude!.Value, p.Longitude!.Value));

        var features = new List<MapFeature>();

        foreach (var place in places.Where(p => PassesYear(p.Activity, query)))
        {
            features.Add(ProjectPlace(place));
        }

        foreach (var institution in institutions.Where(i => PassesYear(i.Activity, query)))
        {
            GeoPoint? location = institution.PlaceId.HasValue &&
                                 placeLookup.TryGetValue(institution.PlaceId.Value, out var g)
                ? g
                : null;
            features.Add(ProjectInstitution(institution, location));
        }

        features = features.Where(f => PassesFilters(f, query)).ToList();

        return new MapResult(query, features, ComputeViewport(features));
    }

    public static ViewportBounds ComputeViewport(IReadOnlyList<MapFeature> features)
    {
        var points = features.SelectMany(f => f.Geometry).ToList();
        if (points.Count == 0)
        {
            return new ViewportBounds(0, 0, 0, 0);
        }

        return new ViewportBounds(
            points.Min(p => p.Latitude),
            points.Max(p => p.Latitude),
            points.Min(p => p.Longitude),
            points.Max(p => p.Longitude));
    }

    private static string TitleOf(Entity e)
    {
        var name = e.Names.FirstOrDefault(n => n.IsPrimary) ?? e.Names.FirstOrDefault();
        return name?.Value ?? "Untitled";
    }
}
