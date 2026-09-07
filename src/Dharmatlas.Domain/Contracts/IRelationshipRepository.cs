using Dharmatlas.Domain.Entities;

namespace Dharmatlas.Domain.Contracts;

/// <summary>
/// Persistence contract for typed, sourceable relationships.
/// </summary>
public interface IRelationshipRepository
{
    Task<Relationship?> GetAsync(EntityId id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Relationship>> GetByEntityAsync(EntityId entityId, CancellationToken cancellationToken = default);
    Task AddAsync(Relationship relationship, CancellationToken cancellationToken = default);
}
