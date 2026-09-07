using Dharmatlas.Domain;
using Dharmatlas.Domain.Entities;
using Dharmatlas.Search.Engine;
using Dharmatlas.Search.Models;
using Xunit;

namespace Dharmatlas.Domain.Tests;

public class SearchEngineTests
{
    private static EntityName Name(EntityId owner, string value, string script = "latin", bool primary = false, string lang = "eng", string romanization = "") =>
        new(owner, lang, script, romanization, value, primary);

    private static SearchEntity Person(string value, bool primary = true, params EntityName[] extra) =>
        Build(EntityType.Person, value, primary, extra);

    private static SearchEntity Build(EntityType type, string value, bool primary, EntityName[] extra = null!, string? region = null)
    {
        var id = EntityId.New();
        var names = new List<EntityName> { Name(id, value, primary ? "han" : "latin", primary) };
        names.AddRange(extra ?? Array.Empty<EntityName>());
        return new SearchEntity { Id = id, Type = type, Names = names, Region = region };
    }

    [Fact]
    public void Romanized_alias_returns_entity_with_matched_form()
    {
        var id = EntityId.New();
        var xuanzang = new SearchEntity
        {
            Id = id,
            Type = EntityType.Person,
            Names = new List<EntityName>
            {
                Name(id, "玄奘", script: "han", primary: true, lang: "chn"),
                Name(id, "Hsuan-tsang", script: "latin", primary: false, lang: "eng", romanization: "Wade-Giles")
            }
        };

        var result = SearchEngine.Search(new SearchQuery { Term = "Hsuan-tsang" }, new[] { xuanzang });

        var hit = Assert.Single(result.Hits);
        Assert.Equal(id, hit.Id);
        Assert.Equal("玄奘", hit.CanonicalName);
        Assert.Equal("Hsuan-tsang", hit.MatchedName);
        Assert.Equal(NameForm.Romanization, hit.MatchedForm);
    }

    [Fact]
    public void Diacritic_insensitive_match()
    {
        var id = EntityId.New();
        var entity = new SearchEntity
        {
            Id = id,
            Type = EntityType.Person,
            Names = new List<EntityName> { Name(id, "Asaṅga", script: "latin", primary: true) }
        };

        var result = SearchEngine.Search(new SearchQuery { Term = "Asanga" }, new[] { entity });

        Assert.Single(result.Hits);
    }

    [Fact]
    public void One_stable_identity_per_entity_even_with_multiple_matching_aliases()
    {
        var id = EntityId.New();
        var entity = new SearchEntity
        {
            Id = id,
            Type = EntityType.Text,
            Names = new List<EntityName>
            {
                Name(id, "Xuanzang", script: "latin", primary: true),
                Name(id, "Xuan", script: "latin", primary: false)
            }
        };

        var result = SearchEngine.Search(new SearchQuery { Term = "Xuan" }, new[] { entity });

        var hit = Assert.Single(result.Hits);
        // The exact alias wins over the substring match on the primary name.
        Assert.Equal("Xuan", hit.MatchedName);
        Assert.Equal(NameForm.Romanization, hit.MatchedForm);
    }

    [Fact]
    public void Ranking_prefers_exact_primary_over_substring()
    {
        var primary = Person("Nalanda", primary: true);
        var substring = Person("Analanda", primary: true); // contains "nalanda"

        var result = SearchEngine.Search(new SearchQuery { Term = "Nalanda" }, new[] { substring, primary });

        Assert.Equal(2, result.Hits.Count);
        Assert.Equal("Nalanda", result.Hits[0].MatchedName);
        Assert.Equal(100, result.Hits[0].Score);
        Assert.Equal(40, result.Hits[1].Score);
    }

    [Fact]
    public void Type_filter_excludes_other_types()
    {
        var person = Person("Xuanzang", primary: true);
        var place = Build(EntityType.Place, "Xuanzang", primary: true);

        var result = SearchEngine.Search(
            new SearchQuery { Term = "Xuanzang", Types = new[] { EntityType.Person } },
            new[] { person, place });

        var hit = Assert.Single(result.Hits);
        Assert.Equal(EntityType.Person, hit.Type);
    }

    [Fact]
    public void Region_filter_matches_place_region_case_insensitively()
    {
        var china = Build(EntityType.Place, "Luoyang", primary: true, Array.Empty<EntityName>(), region: "China");
        var india = Build(EntityType.Place, "Nalanda", primary: true, Array.Empty<EntityName>(), region: "India");

        var result = SearchEngine.Search(
            new SearchQuery { Term = "a", Region = "china" },
            new[] { china, india });

        var hit = Assert.Single(result.Hits);
        Assert.Equal("China", hit.Region);
    }

    [Fact]
    public void Institution_inherits_place_region_for_filtering()
    {
        var placeId = EntityId.New();
        var institution = new SearchEntity
        {
            Id = EntityId.New(),
            Type = EntityType.Institution,
            Names = new List<EntityName> { Name(EntityId.New(), "White Horse", primary: true) },
            Region = "China" // resolved by the service from the place
        };

        var result = SearchEngine.Search(
            new SearchQuery { Term = "White", Region = "China" },
            new[] { institution });

        Assert.Single(result.Hits);
    }

    [Fact]
    public void Ambiguous_name_distinguished_by_type()
    {
        var place = Build(EntityType.Place, "Nalanda", primary: true, Array.Empty<EntityName>(), region: "India");
        var institution = Build(EntityType.Institution, "Nalanda", primary: true, Array.Empty<EntityName>(), region: "India");

        var result = SearchEngine.Search(new SearchQuery { Term = "Nalanda" }, new[] { place, institution });

        Assert.Equal(2, result.Hits.Count);
        Assert.Contains(result.Hits, h => h.Type == EntityType.Place);
        Assert.Contains(result.Hits, h => h.Type == EntityType.Institution);
    }

    [Fact]
    public void Empty_term_returns_no_hits()
    {
        var entity = Person("Xuanzang", primary: true);

        var result = SearchEngine.Search(new SearchQuery { Term = "   " }, new[] { entity });

        Assert.Empty(result.Hits);
    }

    [Fact]
    public void Limit_is_applied()
    {
        var entities = Enumerable.Range(0, 5)
            .Select(_ => Person(Guid.NewGuid().ToString("N"), primary: true))
            .ToArray();

        var result = SearchEngine.Search(new SearchQuery { Term = "a", Limit = 2 }, entities);

        Assert.Equal(2, result.Hits.Count);
    }
}
