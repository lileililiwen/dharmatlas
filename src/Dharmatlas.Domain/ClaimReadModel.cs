using Dharmatlas.Domain.Entities;

namespace Dharmatlas.Domain;

/// <summary>Shared publication boundary for claim projections.</summary>
public static class ClaimReadModel
{
    public static IReadOnlyList<Claim> Published(IEnumerable<Claim> claims) => claims
        .Where(c => c.Status == ClaimStatus.Published)
        .OrderBy(c => c.SubjectEntityId?.ToString() ?? string.Empty, StringComparer.Ordinal)
        .ThenBy(c => c.Id.ToString(), StringComparer.Ordinal)
        .ToList();
}
