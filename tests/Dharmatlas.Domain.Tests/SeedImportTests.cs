using Dharmatlas.Import;
using Dharmatlas.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dharmatlas.Domain.Tests;

public sealed class SeedImportTests
{
    [Fact]
    public void Validator_rejects_unknown_source_with_record_path()
    {
        var manifest = SeedManifestJson.Parse("""
        {
          "schemaVersion": "1.0.0",
          "metadata": { "license": "CC-BY-4.0", "licenseUrl": "https://creativecommons.org/licenses/by/4.0/" },
          "sources": [],
          "entities": [{ "id": "11111111-1111-1111-1111-111111111111", "type": "Person", "sourceIds": ["22222222-2222-2222-2222-222222222222"] }],
          "names": [], "claims": [], "relationships": []
        }
        """);

        var result = SeedManifestValidator.Validate(manifest);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("entities[0].sourceIds") && error.Contains("22222222"));
    }

    [Fact]
    public void Validator_accepts_representative_uncertain_manifest()
    {
        var manifest = SeedManifestJson.Parse(File.ReadAllText(SeedFixturePath()));

        var result = SeedManifestValidator.Validate(manifest);

        Assert.True(result.IsValid, string.Join(Environment.NewLine, result.Errors));
        Assert.Contains(manifest.Entities, entity => entity.Date?.Kind == "Traditional");
        Assert.Contains(manifest.Entities, entity => entity.Date?.Kind == "Interval");
        Assert.Contains(manifest.Entities, entity => entity.Certainty == "Disputed");
    }

    [Fact]
    public void Parsing_the_same_manifest_produces_stable_records()
    {
        var first = SeedManifestJson.Parse(File.ReadAllText(SeedFixturePath()));
        var second = SeedManifestJson.Parse(File.ReadAllText(SeedFixturePath()));

        Assert.Equal(SeedManifestJson.Serialize(first), SeedManifestJson.Serialize(second));
        Assert.Equal(first.Entities.Select(entity => entity.Id), second.Entities.Select(entity => entity.Id));
    }

    [Fact]
    public async Task Import_is_idempotent_for_the_pinned_manifest()
    {
        var manifest = SeedManifestJson.Parse(File.ReadAllText(SeedFixturePath()));
        await using var db = new DharmatlasDbContext(new DbContextOptionsBuilder<DharmatlasDbContext>()
            .UseInMemoryDatabase("seed-import-" + Guid.NewGuid())
            .Options);

        await SeedImporter.ImportAsync(db, manifest);
        await SeedImporter.ImportAsync(db, manifest);

        Assert.Equal(manifest.Entities.Count, await db.Entities.CountAsync());
        Assert.Equal(manifest.Names.Count, await db.EntityNames.CountAsync());
        Assert.Equal(manifest.Sources.Count, await db.Sources.CountAsync());
        Assert.Equal(manifest.Claims.Count, await db.Claims.CountAsync());
        Assert.Equal(manifest.Relationships.Count, await db.Relationships.CountAsync());
    }

    [Fact]
    public async Task Invalid_manifest_is_rejected_before_existing_data_changes()
    {
        var manifest = SeedManifestJson.Parse("""
        {
          "schemaVersion": "1.0.0",
          "metadata": { "license": "CC-BY-4.0", "licenseUrl": "https://creativecommons.org/licenses/by/4.0/" },
          "sources": [], "entities": [], "names": [], "claims": [],
          "relationships": [{ "id": "51111111-1111-1111-1111-111111111111", "fromEntityId": "52222222-2222-2222-2222-222222222222", "toEntityId": "53333333-3333-3333-3333-333333333333", "type": "linked", "certainty": "Documented", "sourceIds": ["54444444-4444-4444-4444-444444444444"] }]
        }
        """);
        await using var db = new DharmatlasDbContext(new DbContextOptionsBuilder<DharmatlasDbContext>()
            .UseInMemoryDatabase("seed-invalid-" + Guid.NewGuid())
            .Options);

        await Assert.ThrowsAsync<SeedManifestException>(() => SeedImporter.ImportAsync(db, manifest));

        Assert.Equal(0, await db.Entities.CountAsync());
        Assert.Equal(0, await db.Relationships.CountAsync());
    }

    private static string SeedFixturePath() => Path.Combine(
        AppContext.BaseDirectory, "data/seed/v1/manifest.json");
}
