using Dharmatlas.Map.Models;

namespace Dharmatlas.Map.Services;

/// <summary>
/// Read-only historical map query contract. Consumes the data model and returns
/// a bounded, time-filtered feature set with provenance.
/// </summary>
public interface IMapQueryService
{
    Task<MapResult> QueryAsync(MapQuery query, CancellationToken cancellationToken = default);
}
