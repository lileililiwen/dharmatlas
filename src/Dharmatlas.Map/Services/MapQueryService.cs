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
        var places = await _db.Entities.OfType<Place>().ToListAsync(cancellationToken);
        var institutions = await _db.Entities.OfType<Institution>().ToListAsync(cancellationToken);

        return MapEngine.Query(query, places, institutions);
    }
}
