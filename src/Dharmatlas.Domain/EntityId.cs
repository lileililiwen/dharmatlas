namespace Dharmatlas.Domain;

/// <summary>
/// Strongly-typed stable identifier for every domain object (entities, sources,
/// claims, revisions, contributors). Wraps a Guid so identifiers cannot be
/// confused with arbitrary strings at the domain boundary.
/// </summary>
public readonly record struct EntityId
{
    public Guid Value { get; }

    private EntityId(Guid value) => Value = value;

    public static EntityId New() => new(Guid.NewGuid());

    public static EntityId From(Guid value) => new(value);

    public static bool TryParse(string? raw, out EntityId id)
    {
        if (Guid.TryParse(raw, out var guid))
        {
            id = new EntityId(guid);
            return true;
        }

        id = default;
        return false;
    }

    public override string ToString() => Value.ToString();
}
