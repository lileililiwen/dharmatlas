using Dharmatlas.Domain;
using Dharmatlas.Domain.ValueObjects;

namespace Dharmatlas.Map.Models;

/// <summary>Kind of map feature: a point (place/institution) or a route (line).</summary>
public enum MapFeatureType
{
    Place,
    Institution,
    Route
}

/// <summary>
/// A time-filtered, georeferenced feature for the historical map. Exposes its
/// type, activity expression, certainty, and source links so the surface never
/// collapses uncertain activity into exact continuity.
/// </summary>
/// <param name="Id">Stable feature identifier.</param>
/// <param name="Type">Feature kind.</param>
/// <param name="Title">Display title from the entity's primary name.</param>
/// <param name="ActivityExpression">Authored activity expression (e.g. "400-1000 CE").</param>
/// <param name="Certainty">Historical-certainty of the feature's activity.</param>
/// <param name="SourceIds">Linked source references (provenance).</param>
/// <param name="Geometry">One point for place/institution; two or more for a route.</param>
/// <param name="Kind">Sub-kind (e.g. "ArchaeologicalSite"), if available.</param>
/// <param name="Region">Political region, if available.</param>
/// <param name="DetailRoute">Route to the entity detail view.</param>
public sealed record MapFeature(
    EntityId Id,
    MapFeatureType Type,
    string Title,
    string ActivityExpression,
    Certainty Certainty,
    IReadOnlyList<EntityId> SourceIds,
    IReadOnlyList<GeoPoint> Geometry,
    string? Kind,
    string? Region,
    string DetailRoute);
