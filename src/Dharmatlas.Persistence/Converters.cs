using System.Text.Json;
using Dharmatlas.Domain;
using Dharmatlas.Domain.Entities;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Dharmatlas.Persistence;

/// <summary>
/// Persists <see cref="EntityId"/> as its underlying Guid.
/// </summary>
public class EntityIdConverter : ValueConverter<EntityId, Guid>
{
    public EntityIdConverter()
        : base(id => id.Value, g => EntityId.From(g)) { }
}

/// <summary>
/// Persists a list of source references as a JSON string so the column stays
/// single-valued while preserving order and multiplicity.
/// </summary>
public class SourceIdsConverter : ValueConverter<IReadOnlyList<EntityId>, string>
{
    public SourceIdsConverter()
        : base(
            ids => JsonSerializer.Serialize(ids.Select(x => x.Value).ToArray()),
            json => JsonSerializer.Deserialize<Guid[]>(json)!.Select(EntityId.From).ToList()) { }
}

public class ContributorRolesConverter : ValueConverter<IReadOnlyList<ContributorRole>, string>
{
    public ContributorRolesConverter()
        : base(
            roles => JsonSerializer.Serialize(roles.Select(role => role.ToString()).ToArray()),
            json => JsonSerializer.Deserialize<string[]>(json)!
                .Select(role => Enum.Parse<ContributorRole>(role, true)).ToArray()) { }
}
