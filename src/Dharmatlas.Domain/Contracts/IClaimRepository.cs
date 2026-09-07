using Dharmatlas.Domain.Entities;

namespace Dharmatlas.Domain.Contracts;

/// <summary>
/// Persistence contract for source-linked claims.
/// </summary>
public interface IClaimRepository
{
    Task<Claim?> GetAsync(EntityId id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Claim>> GetBySubjectAsync(EntityId subjectId, CancellationToken cancellationToken = default);
    Task AddAsync(Claim claim, CancellationToken cancellationToken = default);
}
