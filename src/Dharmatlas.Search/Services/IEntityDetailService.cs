using Dharmatlas.Domain;
using Dharmatlas.Search.Models;

namespace Dharmatlas.Search.Services;

/// <summary>
/// Read-only entity detail (entity page) contract. Returns a fully assembled
/// payload or null when the entity does not exist.
/// </summary>
public interface IEntityDetailService
{
    Task<EntityDetail?> GetAsync(EntityId id, CancellationToken cancellationToken = default);
}
