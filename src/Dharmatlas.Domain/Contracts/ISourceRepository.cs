using Dharmatlas.Domain.Entities;

namespace Dharmatlas.Domain.Contracts;

/// <summary>
/// Persistence contract for first-class bibliographic sources.
/// </summary>
public interface ISourceRepository
{
    Task<Source?> GetAsync(EntityId id, CancellationToken cancellationToken = default);
    Task AddAsync(Source source, CancellationToken cancellationToken = default);
}
