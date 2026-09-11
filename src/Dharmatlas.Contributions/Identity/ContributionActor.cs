using System.Security.Claims;
using Dharmatlas.Domain;
using Dharmatlas.Domain.Entities;
using Dharmatlas.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dharmatlas.Contributions.Identity;

/// <summary>Provider-neutral application identity after host authentication succeeds.</summary>
public sealed record ContributionActor(
    EntityId ContributorId,
    string ExternalSubject,
    IReadOnlySet<ContributorRole> Roles)
{
    public bool IsContributor => Roles.Contains(ContributorRole.Contributor) || IsAdministrator;
    public bool IsReviewer => Roles.Contains(ContributorRole.Reviewer) || IsAdministrator;
    public bool IsAdministrator => Roles.Contains(ContributorRole.Administrator);
}

public interface IContributionActorResolver
{
    Task<ContributionActor?> ResolveAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default);
}

public sealed class ContributionActorResolver : IContributionActorResolver
{
    private readonly DharmatlasDbContext _db;

    public ContributionActorResolver(DharmatlasDbContext db) => _db = db;

    public async Task<ContributionActor?> ResolveAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
    {
        if (principal.Identity?.IsAuthenticated != true) return null;
        var subject = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? principal.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(subject)) return null;

        var contributor = await _db.Contributors.SingleOrDefaultAsync(c => c.ExternalSubject == subject, cancellationToken);
        if (contributor is null) return null;
        return new ContributionActor(contributor.Id, subject, contributor.Roles.ToHashSet());
    }
}
