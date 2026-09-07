namespace Dharmatlas.Domain.Entities;

/// <summary>
/// A person or agent who records or revises data. Distinct from publication
/// rights: recording a revision does not authorize publishing a claim.
/// </summary>
public sealed record Contributor
{
    public EntityId Id { get; init; } = EntityId.New();
    public string DisplayName { get; init; }
    public string? Email { get; init; }

    public Contributor(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new DomainValidationException("A contributor requires a display name.");
        }

        DisplayName = displayName;
    }
}
