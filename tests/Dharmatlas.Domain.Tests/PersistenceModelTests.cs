using Dharmatlas.Domain.Entities;
using Dharmatlas.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Dharmatlas.Domain.Tests;

/// <summary>
/// Validates the EF Core persistence model (mappings + indexes) without a live
/// database. Confirms the configuration finalizes and the required indexes exist.
/// </summary>
public class PersistenceModelTests
{
    private static DharmatlasDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<DharmatlasDbContext>()
            .UseInMemoryDatabase("model-check")
            .Options;
        return new DharmatlasDbContext(options);
    }

    [Fact]
    public void Model_finalizes_without_errors()
    {
        using var context = CreateContext();

        Assert.NotNull(context.Model);
        Assert.NotEmpty(context.Model.GetEntityTypes());
    }

    [Fact]
    public void Entities_use_table_per_hierarchy()
    {
        using var context = CreateContext();

        var eventType = context.Model.FindEntityType(typeof(Event))!;
        Assert.Equal("entities", eventType.GetTableName());
        Assert.Equal("entities", context.Model.FindEntityType(typeof(Person))!.GetTableName());
    }

    [Fact]
    public void Event_exposes_normalized_date_bounds_and_range_index()
    {
        using var context = CreateContext();

        var eventType = context.Model.FindEntityType(typeof(Event))!;
        var owned = eventType.GetNavigations().Single(n => n.Name == nameof(Event.When)).TargetEntityType;
        Assert.Contains(owned.GetProperties(), p => p.GetColumnName() == "when_lower");
        Assert.Contains(owned.GetProperties(), p => p.GetColumnName() == "when_upper");
        Assert.Contains(owned.GetIndexes(), i => i.Name == "ix_events_when_range");
    }

    [Fact]
    public void Claim_has_certainty_status_index()
    {
        using var context = CreateContext();

        var claimType = context.Model.FindEntityType(typeof(Claim))!;
        Assert.Contains(claimType.GetIndexes(), i => i.Name == "ix_claims_certainty_status");
        Assert.Contains(claimType.GetIndexes(), i => i.Name == "ix_claims_subject");
    }

    [Fact]
    public void Relationship_has_endpoint_index()
    {
        using var context = CreateContext();

        var relationshipType = context.Model.FindEntityType(typeof(Relationship))!;
        Assert.Contains(relationshipType.GetIndexes(), i => i.Name == "ix_relationships_endpoints");
    }

    [Fact]
    public void Entity_names_have_primary_language_uniqueness_constraint()
    {
        using var context = CreateContext();

        var nameType = context.Model.FindEntityType(typeof(EntityName))!;
        var index = nameType.GetIndexes().Single(i => i.Name == "ux_entity_names_primary_language");

        Assert.True(index.IsUnique);
        Assert.Contains("is_primary", index.GetFilter(), StringComparison.OrdinalIgnoreCase);
    }
}
