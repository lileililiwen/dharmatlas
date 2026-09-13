using Dharmatlas.AI.Engine;
using Dharmatlas.Api.Services;
using Dharmatlas.Domain;
using Dharmatlas.Domain.Entities;
using Dharmatlas.Domain.ValueObjects;
using Dharmatlas.Import;
using Dharmatlas.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dharmatlas.Domain.Tests;

public sealed class CorpusVerificationTests
{
    [Fact]
    public void V2_manifest_passes_structural_validation()
    {
        var manifest = V2();

        var result = SeedManifestValidator.Validate(manifest);

        Assert.True(result.IsValid, string.Join(Environment.NewLine, result.Errors));
    }

    [Fact]
    public void V2_manifest_passes_publication_readiness()
    {
        var manifest = V2();

        var errors = SeedPublicationReadiness.Check(manifest);

        Assert.Empty(errors);
    }

    [Fact]
    public void V2_corpus_spans_required_regions_types_and_record_counts()
    {
        var manifest = V2();
        var coverage = SeedCoverage.Report(manifest);

        Assert.True(coverage.IsComplete);
        Assert.InRange(manifest.Entities.Count, 50, 100);
        Assert.All(
            new[] { "Ashoka", "Xuanzang", "Nalanda" },
            canonical => Assert.Contains(manifest.Names, n => n.Value.Contains(canonical)));
    }

    [Fact]
    public void V2_competing_interpretations_persist_as_separate_claims()
    {
        var manifest = V2();
        var japan = manifest.Entities.Single(e => e.Summary != null && e.Summary.Contains("chronicle dates compete"));
        var japanClaims = manifest.Claims.Where(c => c.SubjectEntityId == japan.Id).ToList();
        var buddha = manifest.Names.Single(n => n.Value == "The Buddha");
        var buddhaClaims = manifest.Claims.Where(c => c.SubjectEntityId == buddha.EntityId).ToList();

        Assert.Equal(2, japanClaims.Count);
        Assert.Equal(2, buddhaClaims.Count);
        Assert.Contains(japanClaims, c => c.Certainty == "TraditionalAccount" && c.Interpretation == "TraditionalAccount");
        Assert.Contains(buddhaClaims, c => c.Interpretation == "ScholarlyInterpretation");
    }

    [Fact]
    public void Unknown_tier_is_rejected_with_named_code()
    {
        var manifest = SeedManifestJson.Parse("""
        {
          "schemaVersion": "1.1.0",
          "metadata": { "license": "CC-BY-4.0", "licenseUrl": "https://example.invalid/" },
          "sources": [{ "id": "11111111-1111-1111-1111-111111111111", "title": "X", "tier": "forged" }],
          "entities": [], "names": [], "claims": [], "relationships": []
        }
        """);

        var result = SeedManifestValidator.Validate(manifest);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("tier"));
    }

    [Fact]
    public void Denied_tone_is_rejected_with_named_code()
    {
        var manifest = SeedManifestJson.Parse("""
        {
          "schemaVersion": "1.1.0",
          "metadata": { "license": "CC-BY-4.0", "licenseUrl": "https://example.invalid/" },
          "sources": [], "entities": [],
          "names": [], "relationships": [],
          "claims": [{ "id": "41111111-1111-1111-1111-111111111111", "subjectEntityId": "21111111-1111-1111-1111-111111111111", "statement": "The greatest school of all.", "certainty": "Probable", "sourceIds": [] }]
        }
        """);

        var result = SeedManifestValidator.Validate(manifest);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("tone"));
    }

    [Fact]
    public void Unsourced_record_fails_readiness_with_named_rejection()
    {
        var manifest = SeedManifestJson.Parse("""
        {
          "schemaVersion": "1.1.0",
          "metadata": { "license": "CC-BY-4.0", "licenseUrl": "https://example.invalid/" },
          "sources": [{ "id": "11111111-1111-1111-1111-111111111111", "title": "X", "tier": "scholarly" }],
          "entities": [{ "id": "21111111-1111-1111-1111-111111111111", "type": "Person", "summary": "A well-known figure." }],
          "names": [], "relationships": [],
          "claims": [{ "id": "41111111-1111-1111-1111-111111111111", "subjectEntityId": "21111111-1111-1111-1111-111111111111", "statement": "An assertion.", "certainty": "Probable", "sourceIds": [] }]
        }
        """);

        var errors = SeedPublicationReadiness.Check(manifest);

        Assert.Contains(errors, e => e.Contains("resolvability"));
    }

    [Fact]
    public void Citation_checker_flags_missing_sources_and_tier_mismatches_only()
    {
        var traditional = EntityId.New();
        var scholarly = EntityId.New();
        var tiers = new Dictionary<EntityId, SourceTier?>
        {
            [traditional] = SourceTier.Traditional,
            [scholarly] = SourceTier.Scholarly
        };

        var report = CitationChecker.Check(new[]
        {
            ("documented-by-chronicle", Certainty.Documented, ClaimInterpretation.Historical, (IReadOnlyList<EntityId>)new[] { traditional }),
            ("traditional-without-chronicle", Certainty.TraditionalAccount, ClaimInterpretation.TraditionalAccount, (IReadOnlyList<EntityId>)new[] { scholarly }),
            ("unresolvable", Certainty.Probable, ClaimInterpretation.Historical, (IReadOnlyList<EntityId>)Array.Empty<EntityId>()),
            ("clean", Certainty.Probable, ClaimInterpretation.Historical, (IReadOnlyList<EntityId>)new[] { traditional, scholarly })
        }, tiers);

        Assert.Equal(3, report.Flags.Count);
        Assert.Contains(report.Flags, f => f.Kind == CitationFlag.TierMismatch && f.SubjectId == "documented-by-chronicle");
        Assert.Contains(report.Flags, f => f.Kind == CitationFlag.TierMismatch && f.SubjectId == "traditional-without-chronicle");
        Assert.Contains(report.Flags, f => f.Kind == CitationFlag.MissingSource && f.SubjectId == "unresolvable");
    }

    [Fact]
    public void Citation_check_is_review_only_and_corpus_is_clean()
    {
        var manifest = V2();
        var tiers = manifest.Sources.ToDictionary(
            s => EntityId.From(Guid.Parse(s.Id)),
            s => string.IsNullOrWhiteSpace(s.Tier) ? (SourceTier?)null : SourceTierParser.Parse(s.Tier));
        var subjects = manifest.Claims
            .Select(c => (c.Id, CertaintyParser.Parse(c.Certainty),
                string.IsNullOrWhiteSpace(c.Interpretation) ? ClaimInterpretation.Historical : Enum.Parse<ClaimInterpretation>(c.Interpretation, true),
                (IReadOnlyList<EntityId>)c.SourceIds.Select(id => EntityId.From(Guid.Parse(id))).ToList()))
            .Concat(manifest.Relationships.Select(r => (r.Id, CertaintyParser.Parse(r.Certainty),
                ClaimInterpretation.Historical,
                (IReadOnlyList<EntityId>)r.SourceIds.Select(id => EntityId.From(Guid.Parse(id))).ToList())))
            .ToList();

        var before = (manifest.Claims.Count, manifest.Relationships.Count);
        var report = CitationChecker.Check(subjects, tiers);

        Assert.True(report.IsClean, string.Join(Environment.NewLine, report.Flags.Select(f => $"{f.SubjectId}: {f.Kind} {f.Detail}")));
        Assert.Equal(before, (manifest.Claims.Count, manifest.Relationships.Count));
    }

    [Fact]
    public async Task V2_import_is_idempotent_and_preserves_tiers_interpretations_locators()
    {
        var manifest = V2();
        await using var db = NewDb();

        await SeedImporter.ImportAsync(db, manifest);
        await SeedImporter.ImportAsync(db, manifest);

        Assert.Equal(manifest.Entities.Count, await db.Entities.CountAsync());
        Assert.Equal(manifest.Sources.Count, await db.Sources.CountAsync());
        Assert.Equal(manifest.Claims.Count, await db.Claims.CountAsync());

        var chronicle = await db.Sources.SingleAsync(s => s.Title == "The Mahavamsa");
        Assert.Equal(SourceTier.Traditional, chronicle.Tier);
        var edict = await db.Sources.SingleAsync(s => s.Title == "The Inscriptions of Asoka");
        Assert.Equal(SourceTier.Primary, edict.Tier);

        Assert.Contains(await db.Claims.ToListAsync(),
            c => c.Interpretation == ClaimInterpretation.TraditionalAccount && c.SourceLocator == "Mahavamsa I");
        Assert.Contains(await db.Claims.ToListAsync(), c => c.SourceLocator == "Rock Edict XIII");
    }

    [Fact]
    public async Task V2_export_checksum_is_reproducible_and_carries_tiers()
    {
        var manifest = V2();
        await using var db = NewDb();
        await SeedImporter.ImportAsync(db, manifest);

        var first = BulkExporter.Build(
            await db.Entities.ToListAsync(), await db.EntityNames.ToListAsync(),
            await db.Relationships.ToListAsync(), await db.Sources.ToListAsync(),
            DateTimeOffset.UnixEpoch, await db.Claims.ToListAsync());
        var second = BulkExporter.Build(
            await db.Entities.ToListAsync(), await db.EntityNames.ToListAsync(),
            await db.Relationships.ToListAsync(), await db.Sources.ToListAsync(),
            DateTimeOffset.UnixEpoch, await db.Claims.ToListAsync());

        Assert.Equal(first.Checksum, second.Checksum);
        Assert.Contains(first.Sources, s => s.Tier == "Traditional");
        Assert.Contains(first.Sources, s => s.Tier == "Primary");
        Assert.Contains(first.Claims, c => c.Interpretation == "TraditionalAccount");
    }

    private static SeedManifest V2() => SeedManifestJson.Parse(
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "data/seed/v2/manifest.json")));

    private static DharmatlasDbContext NewDb() => new(new DbContextOptionsBuilder<DharmatlasDbContext>()
        .UseInMemoryDatabase("corpus-" + Guid.NewGuid())
        .Options);
}
