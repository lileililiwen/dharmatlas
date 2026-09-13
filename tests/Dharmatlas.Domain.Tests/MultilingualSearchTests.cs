using Dharmatlas.Domain;
using Dharmatlas.Domain.Entities;
using Dharmatlas.Persistence;
using Dharmatlas.Search.Engine;
using Dharmatlas.Search.Models;
using Dharmatlas.Search.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Dharmatlas.Domain.Tests;

public sealed class MultilingualSearchTests
{
    private static EntityName Alias(EntityId owner, string value, bool primary = false, string script = "latin") =>
        new(owner, "eng", script, string.Empty, value, primary);

    private static SearchEntity Entity(string canonical, bool primary = true, params string[] aliases)
    {
        var id = EntityId.New();
        var names = new List<EntityName> { Alias(id, canonical, primary) };
        names.AddRange(aliases.Select(a => Alias(id, a)));
        return new SearchEntity { Id = id, Type = EntityType.Person, Names = names };
    }

    [Fact]
    public void Unstored_wade_giles_form_matches_via_transliteration_kind()
    {
        var entity = Entity("Xuanzang", aliases: new[] { "玄奘" });

        var result = SearchEngine.Search(new SearchQuery { Term = "Hsuantsang" }, new[] { entity });

        var hit = Assert.Single(result.Hits);
        Assert.Equal("Xuanzang", hit.MatchedName);
        Assert.Equal(MatchKind.Transliteration, hit.MatchKind);
        Assert.Equal(70, hit.Score);
    }

    [Fact]
    public void Transposed_typo_matches_via_fuzzy_kind_within_distance_cap()
    {
        var entity = Entity("Xuanzang");

        var result = SearchEngine.Search(new SearchQuery { Term = "Xuanzagn" }, new[] { entity });

        var hit = Assert.Single(result.Hits);
        Assert.Equal(MatchKind.Fuzzy, hit.MatchKind);
        Assert.Equal(20, hit.Score);
    }

    [Fact]
    public void Distant_typo_does_not_match()
    {
        var entity = Entity("Xuanzang");

        var result = SearchEngine.Search(new SearchQuery { Term = "Xyzqwv" }, new[] { entity });

        Assert.Empty(result.Hits);
    }

    [Fact]
    public void Short_terms_never_fuzzy_match()
    {
        var entity = Entity("Nalanda");

        var result = SearchEngine.Search(new SearchQuery { Term = "Na" }, new[] { entity });

        // "na" is a substring of "nalanda", so the hit exists but must not be fuzzy.
        var hit = Assert.Single(result.Hits);
        Assert.Equal(MatchKind.Substring, hit.MatchKind);
    }

    [Fact]
    public void Results_never_exceed_100_hits()
    {
        var entities = Enumerable.Range(0, 250)
            .Select(i => Entity($"Aa Test {i:000}"))
            .ToArray();

        var unbounded = SearchEngine.Search(new SearchQuery { Term = "Aa", Limit = 10000 }, entities);
        var @default = SearchEngine.Search(new SearchQuery { Term = "Aa", Limit = null }, entities);

        Assert.Equal(100, unbounded.Hits.Count);
        Assert.Equal(100, @default.Hits.Count);
    }

    [Fact]
    public void Adversarial_terms_stay_bounded_and_throw_nothing()
    {
        var entities = Enumerable.Range(0, 150)
            .Select(i => Entity($"Aa Test {i:000}"))
            .ToArray();
        var hostile = new[] { "%", "_", new string('a', 500), "玄", "'", "\"", "--", "/*" };

        foreach (var term in hostile)
        {
            var result = SearchEngine.Search(new SearchQuery { Term = term, Limit = 10000 }, entities);
            Assert.True(result.Hits.Count <= 100);
        }

        Assert.Empty(SearchEngine.Search(new SearchQuery { Term = "   " }, entities).Hits);
        Assert.Empty(SearchEngine.Search(new SearchQuery { Term = "Aa", Limit = -5 }, entities).Hits);
    }

    [Fact]
    public void Ambiguous_term_returns_separate_hits_without_merging()
    {
        var placeId = EntityId.New();
        var place = new SearchEntity
        {
            Id = placeId,
            Type = EntityType.Place,
            Names = new List<EntityName> { Alias(placeId, "Nalanda", primary: true) }
        };
        var institutionId = EntityId.New();
        var institution = new SearchEntity
        {
            Id = institutionId,
            Type = EntityType.Institution,
            Names = new List<EntityName>
            {
                Alias(institutionId, "Nalanda Mahavihara", primary: true),
                Alias(institutionId, "Nalanda")
            }
        };

        var result = SearchEngine.Search(new SearchQuery { Term = "Nalanda" }, new[] { institution, place });

        Assert.Equal(2, result.Hits.Count);
        Assert.Equal(placeId, result.Hits[0].Id);
        Assert.Equal(institutionId, result.Hits[1].Id);
        Assert.All(result.Hits, h => Assert.False(string.IsNullOrWhiteSpace(h.MatchedName)));
        Assert.All(result.Hits, h => Assert.False(string.IsNullOrWhiteSpace(h.CanonicalName)));
    }

    [Fact]
    public async Task Save_populates_normalized_shadow_column()
    {
        var options = new DbContextOptionsBuilder<DharmatlasDbContext>()
            .UseInMemoryDatabase("multilingual-shadow-" + Guid.NewGuid())
            .Options;
        using (var db = new DharmatlasDbContext(options))
        {
            var owner = EntityId.New();
            var name = new EntityName(owner, "san", "latin", "IAST", "Nālandā", true);
            db.EntityNames.Add(name);
            await db.SaveChangesAsync();

            Assert.Equal("nalanda", db.Entry(name).Property<string>("NormalizedValue").CurrentValue);
        }

        using (var db = new DharmatlasDbContext(options))
        {
            var values = await db.EntityNames
                .Select(n => EF.Property<string>(n, "NormalizedValue"))
                .ToListAsync();
            Assert.Contains("nalanda", values);
        }
    }

    [Fact]
    public async Task Query_service_resolves_diacritic_and_variant_forms_from_storage()
    {
        using var db = NewDb();
        var owner = EntityId.New();
        db.Entities.Add(new Person { Id = owner, Summary = "Pilgrim." });
        db.EntityNames.Add(new EntityName(owner, "eng", "latin", string.Empty, "Xuanzang", true));
        db.EntityNames.Add(new EntityName(owner, "zho", "hani", string.Empty, "玄奘"));
        await db.SaveChangesAsync();
        var service = new SearchQueryService(db);

        var diacritic = await service.QueryAsync(new SearchQuery { Term = "Xuánzàng" });
        var variant = await service.QueryAsync(new SearchQuery { Term = "hsuantsang" });
        var script = await service.QueryAsync(new SearchQuery { Term = "玄奘" });

        Assert.Equal(owner, Assert.Single(diacritic.Hits).Id);
        Assert.Equal(MatchKind.Exact, diacritic.Hits.Single().MatchKind);
        Assert.Equal(owner, Assert.Single(variant.Hits).Id);
        Assert.Equal(MatchKind.Transliteration, variant.Hits.Single().MatchKind);
        Assert.Equal(owner, Assert.Single(script.Hits).Id);
    }

    private static DharmatlasDbContext NewDb() =>
        new(new DbContextOptionsBuilder<DharmatlasDbContext>()
            .UseInMemoryDatabase("multilingual-" + Guid.NewGuid())
            .Options);
}
