namespace Dharmatlas.Domain.Entities;

/// <summary>
/// Concrete entity kinds sharing one identity in the project-foundation contract.
/// </summary>
public enum EntityType
{
    Person,
    Place,
    Institution,
    Text,
    Tradition,
    Event
}

/// <summary>
/// Common identity for all historical entities. A stable <see cref="Id"/> is
/// decoupled from names, so renaming or adding an alias never changes identity.
/// </summary>
public abstract record Entity
{
    public EntityId Id { get; init; } = EntityId.New();
    public EntityType Type { get; }
    public string? Summary { get; init; }

    private readonly List<EntityName> _names = new();
    public IReadOnlyList<EntityName> Names => _names.AsReadOnly();

    protected Entity(EntityType type) => Type = type;

    /// <summary>
    /// Adds an alternate name. Enforces a non-empty value and at most one primary
    /// name per language so multilingual search stays unambiguous.
    /// </summary>
    public Entity AddName(EntityName name)
    {
        if (string.IsNullOrWhiteSpace(name.Value))
        {
            throw new DomainValidationException("An entity name requires a non-empty value.");
        }

        if (name.IsPrimary && _names.Any(n => n.IsPrimary && n.Language == name.Language))
        {
            throw new DomainValidationException(
                $"Entity already has a primary name for language '{name.Language}'.");
        }

        _names.Add(name);
        return this;
    }
}

public sealed record Person : Entity
{
    public Person() : base(EntityType.Person) { }
}

public sealed record Place : Entity
{
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public string? ModernName { get; init; }

    public Place() : base(EntityType.Place) { }
}

public sealed record Institution : Entity
{
    public string? InstitutionalForm { get; init; }

    public Institution() : base(EntityType.Institution) { }
}

public sealed record Text : Entity
{
    public string? OriginalLanguage { get; init; }

    public Text() : base(EntityType.Text) { }
}

public sealed record Tradition : Entity
{
    public string? Region { get; init; }

    public Tradition() : base(EntityType.Tradition) { }
}
