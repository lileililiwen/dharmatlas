using Dharmatlas.Domain.Entities;
using Dharmatlas.Map.Engine;
using Dharmatlas.Map.Models;
using Dharmatlas.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dharmatlas.Map.Services;

/// <summary>
/// Applies the map engine to georeferenced entities loaded from the data model.
/// Read-only: it does not mutate entities or duplicate date/source semantics.
/// </summary>
public sealed class MapQueryService : IMapQueryService
{
    private readonly DharmatlasDbContext _db;

    public MapQueryService(DharmatlasDbContext db) => _db = db;

    public async Task<MapResult> QueryAsync(MapQuery query, CancellationToken cancellationToken = default)
    {
        const int maxFeatures = 500;
        var placesQuery = _db.Entities.OfType<Place>().AsNoTracking()
            .Where(p => (p.Activity == null && query.IncludeUnknownActivity) ||
                (p.Activity != null && (p.Activity.NormalizedLowerBound == null || p.Activity.NormalizedLowerBound <= query.Year) &&
                 (p.Activity.NormalizedUpperBound == null || p.Activity.NormalizedUpperBound >= query.Year)));
        if (!string.IsNullOrWhiteSpace(query.Region)) placesQuery = placesQuery.Where(p => p.Region == query.Region);
        if (query.MinLatitude is { } minLat) placesQuery = placesQuery.Where(p => p.Latitude >= minLat);
        if (query.MaxLatitude is { } maxLat) placesQuery = placesQuery.Where(p => p.Latitude <= maxLat);
        if (query.MinLongitude is { } minLng) placesQuery = placesQuery.Where(p => p.Longitude >= minLng);
        if (query.MaxLongitude is { } maxLng) placesQuery = placesQuery.Where(p => p.Longitude <= maxLng);
        if (query.Types is { Count: > 0 } && !query.Types.Contains(MapFeatureType.Place)) placesQuery = placesQuery.Where(_ => false);
        var places = await placesQuery.OrderBy(p => p.Id.Value).Take(maxFeatures).ToListAsync(cancellationToken);

        var institutions = new List<Institution>();
        if (query.Types is null or { Count: 0 } || query.Types.Contains(MapFeatureType.Institution))
        {
            var institutionQuery = _db.Entities.OfType<Institution>().AsNoTracking()
                .Where(i => (i.Activity == null && query.IncludeUnknownActivity) ||
                    (i.Activity != null && (i.Activity.NormalizedLowerBound == null || i.Activity.NormalizedLowerBound <= query.Year) &&
                     (i.Activity.NormalizedUpperBound == null || i.Activity.NormalizedUpperBound >= query.Year)));
            institutions = await institutionQuery.OrderBy(i => i.Id.Value).Take(maxFeatures).ToListAsync(cancellationToken);
        }

        return MapEngine.Query(query, places, institutions);
    }
}
