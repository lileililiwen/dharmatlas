using Dharmatlas.Domain;
using Dharmatlas.Domain.Entities;
using Dharmatlas.Search.Engine;
using Dharmatlas.Search.Models;

namespace Dharmatlas.Domain.Tests;

public sealed class EntityNameReadModelTests
{
    [Fact]
    public void Names_are_ordered_and_fallback_uses_first_deterministic_alias()
    {
        var id = EntityId.New();
        var names = new[]
        {
            new EntityName(id, "eng", "latin", "", "The Awakened One"),
            new EntityName(id, "san", "devanagari", "IAST", "बुद्ध"),
            new EntityName(id, "eng", "latin", "", "Buddha", isPrimary: true)
        };

        var ordered = EntityNameReadModel.Order(names);

        Assert.Equal("Buddha", EntityNameReadModel.CanonicalName(ordered));
        Assert.Equal("Buddha", SearchEngine.Search(
            new SearchQuery { Term = "buddha" },
            new[] { new SearchEntity { Id = id, Type = EntityType.Person, Names = names } })
            .Hits.Single().CanonicalName);
    }

    [Fact]
    public void Validation_rejects_normalized_duplicate_names_and_invalid_metadata()
    {
        var id = EntityId.New();
        var names = new[]
        {
            new EntityName(id, "ENG", "latin", "", "Buddha"),
            new EntityName(id, "eng", "latin", "", " Buddha ")
        };

        var duplicate = Assert.Throws<DomainValidationException>(() => EntityNameReadModel.Validate(names));
        Assert.Contains("duplicate", duplicate.Message, StringComparison.OrdinalIgnoreCase);

        var invalid = Assert.Throws<DomainValidationException>(() => EntityNameReadModel.Validate(new[]
        {
            new EntityName(id, "", "latin", "", "Buddha")
        }));
        Assert.Contains("language", invalid.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Domain_primary_name_check_is_case_insensitive()
    {
        var person = new Person();
        person.AddName(new EntityName(person.Id, "ENG", "latin", "", "Buddha", isPrimary: true));

        var error = Assert.Throws<DomainValidationException>(() =>
            person.AddName(new EntityName(person.Id, "eng", "latin", "", "The Awakened One", isPrimary: true)));

        Assert.Contains("primary", error.Message, StringComparison.OrdinalIgnoreCase);
    }
}
