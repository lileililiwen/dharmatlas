namespace Dharmatlas.Domain.Entities;

/// <summary>
/// A first-class bibliographic record. Claims and relationships link to sources
/// by reference rather than embedding free-text citations that cannot be
/// inspected.
/// </summary>
public sealed record Source
{
    public EntityId Id { get; init; } = EntityId.New();
    public string Title { get; init; }
    public string? Author { get; init; }
    public string? Date { get; init; }
    public string? PublisherOrCollection { get; init; }

    /// <summary>DOI, inscription number, or other stable locator.</summary>
    public string? Identifier { get; init; }

    /// <summary>
    /// Editorial quality tier (primary, scholarly, traditional, reference).
    /// Null for legacy records authored before tiering; new corpus records
    /// always carry a tier.
    /// </summary>
    public SourceTier? Tier { get; init; }

    public Source(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainValidationException("A source requires a title.");
        }

        Title = title;
    }
}
