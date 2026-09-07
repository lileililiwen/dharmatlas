using Dharmatlas.Domain.Entities;

namespace Dharmatlas.Domain.Contracts;

/// <summary>
/// Persistence contract for entities and their multilingual names. Implemented
/// by the future data layer; independent of any UI.
/// </summary>
public interface IEntityRepository
{
    Task<Entity?> GetAsync(EntityId id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Entity>> FindByNameAsync(string query, CancellationToken cancellationToken = default);
    Task AddAsync(Entity entity, CancellationToken cancellationToken = default);
}
