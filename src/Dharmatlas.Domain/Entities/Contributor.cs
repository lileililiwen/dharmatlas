namespace Dharmatlas.Domain.Entities;

/// <summary>
/// A person or agent who records or revises data. Distinct from publication
/// rights: recording a revision does not authorize publishing a claim.
/// </summary>
public sealed record Contributor
{
    public EntityId Id { get; init; } = EntityId.New();
    public string? ExternalSubject { get; init; }
    public string DisplayName { get; init; }
    public string? Email { get; init; }
    public IReadOnlyList<ContributorRole> Roles { get; init; } = new[] { ContributorRole.Contributor };

    public Contributor(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new DomainValidationException("A contributor requires a display name.");
        }

        DisplayName = displayName;
    }

    public Contributor(string displayName, string externalSubject, IReadOnlyList<ContributorRole>? roles = null)
        : this(displayName)
    {
        if (string.IsNullOrWhiteSpace(externalSubject))
        {
            throw new DomainValidationException("An authenticated contributor requires an external subject.");
        }

        ExternalSubject = externalSubject;
        Roles = roles is { Count: > 0 } ? roles : new[] { ContributorRole.Contributor };
    }
}
