using System.Diagnostics.CodeAnalysis;
using Dharmatlas.AI.Engine;
using Dharmatlas.AI.Services;
using Dharmatlas.Contributions.Services;
using Dharmatlas.Domain;
using Dharmatlas.Domain.Entities;
using Dharmatlas.Domain.ValueObjects;
using Dharmatlas.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Dharmatlas.Domain.Tests;

[SuppressMessage("ReSharper", "AccessToDisposedClosure")]
public class AiCurationTests
{
    private const string Transcript = "Nagarjuna (c. 150 CE) founded the Madhyamaka school in India.";

    [Fact]
    public async Task Extract_person_retains_full_provenance_and_does_not_publish()
    {
        var db = NewDb();
        var service = NewService(db);
        var source = EntityId.New();

        var draft = await service.ExtractPersonFromSourceAsync(source, Transcript);

        Assert.Equal(AiDraftKind.EntityExtraction, draft.Kind);
        Assert.Equal(source, draft.InputReferenceId);
        Assert.Equal(Transcript, draft.InputText);
        Assert.Equal(AiJobs.DefaultModel, draft.Model);
        Assert.Equal(AiJobs.DefaultVersion, draft.ModelVersion);
        Assert.Equal("entity-extraction/v1", draft.PromptRef);
        Assert.InRange(draft.Confidence, 0.0, 1.0);
        Assert.Equal(AiDraftStatus.Draft, draft.Status);
        Assert.Contains("Nagarjuna", draft.SuggestionJson);

        // The draft is stored, but no published entity appears.
        Assert.Empty(await db.Entities.OfType<Person>().ToListAsync());
    }

    [Fact]
    public async Task Rejected_draft_creates_no_published_claim()
    {
        var db = NewDb();
        var service = NewService(db);
        var source = EntityId.New();
        var from = new Person();
        var to = new Person();
        db.Entities.AddRange(from, to);
        db.Sources.Add(new Source("Chronicle") { Id = source });
        await db.SaveChangesAsync();

        var draft = await service.SuggestRelationshipAsync(source, from.Id, to.Id, "teacher-of", Certainty.Documented, new[] { source });
        var decided = await service.DecideAsync(draft.Id, EntityId.New(), AiDecisionType.Reject, "Citation cannot be verified");

        Assert.Equal(AiDraftStatus.Rejected, decided.Status);
        Assert.Equal("Citation cannot be verified", decided.Decisions[^1].Reason);
        Assert.Empty(await db.Relationships.ToListAsync());
    }

    [Fact]
    public async Task Duplicate_suggestion_preserves_both_records()
    {
        var db = NewDb();
        var service = NewService(db);
        var a = new Person();
        var b = new Person();
        db.Entities.AddRange(a, b);
        await db.SaveChangesAsync();

        var drafts = await service.SuggestDuplicatesAsync(new[]
        {
            (a.Id, "Nagarjuna"),
            (b.Id, "nagarjuna")
        });

        Assert.Single(drafts);
        Assert.Equal(AiDraftKind.DuplicateDetection, drafts[0].Kind);

        // Both records remain; nothing is merged.
        Assert.Equal(2, await db.Entities.OfType<Person>().CountAsync());
    }

    [Fact]
    public async Task Date_conflict_suggestion_is_non_destructive()
    {
        var db = NewDb();
        var service = NewService(db);
        var early = new Event { When = HistoricalDate.Exact("300 CE", 300) };
        var late = new Event { When = HistoricalDate.Exact("900 CE", 900) };
        db.Entities.AddRange(early, late);
        await db.SaveChangesAsync();

        var drafts = await service.SuggestDateConflictsAsync(new[]
        {
            (early.Id, early.When!),
            (late.Id, late.When!)
        });

        Assert.Single(drafts);
        Assert.Equal(AiDraftKind.DateConflict, drafts[0].Kind);

        // Neither record's date was altered.
        var stillEarly = await db.Entities.OfType<Event>().FirstAsync(e => e.Id == early.Id);
        var stillLate = await db.Entities.OfType<Event>().FirstAsync(e => e.Id == late.Id);
        Assert.Equal("300 CE", stillEarly.When!.DisplayExpression);
        Assert.Equal("900 CE", stillLate.When!.DisplayExpression);
    }

    [Fact]
    public async Task Accept_promotes_to_pending_submission_publish_through_review()
    {
        var db = NewDb();
        var service = NewService(db);
        var contributions = new ContributionService(db);
        var source = EntityId.New();

        var draft = await service.ExtractPersonFromSourceAsync(source, Transcript);
        var accepted = await service.DecideAsync(draft.Id, EntityId.New(), AiDecisionType.Accept, "Plausible, verify against source");

        // Acceptance alone does not publish: it only creates a pending submission.
        Assert.Equal(AiDraftStatus.Accepted, accepted.Status);
        Assert.Empty(await db.Entities.OfType<Person>().ToListAsync());

        var submission = await service.PromoteAsync(draft.Id, EntityId.New(), EntityId.New());

        Assert.Equal(SubmissionStatus.PendingReview, submission.Status);
        Assert.Empty(await db.Entities.OfType<Person>().ToListAsync());

        // A separate human approval is required before publication.
        await contributions.ReviewAsync(submission.Id, EntityId.New(), ReviewDecisionType.Approve, "Confirmed in chronicle");

        var persons = await db.Entities.OfType<Person>().ToListAsync();
        Assert.Single(persons);
        Assert.Contains("Nagarjuna", persons[0].Summary);
    }

    [Fact]
    public async Task Return_for_correction_keeps_draft_editable()
    {
        var db = NewDb();
        var service = NewService(db);
        var source = EntityId.New();

        var draft = await service.ExtractPersonFromSourceAsync(source, Transcript);
        var returned = await service.DecideAsync(draft.Id, EntityId.New(), AiDecisionType.ReturnForCorrection, "Re-run with cleaner transcript");
        Assert.Equal(AiDraftStatus.Draft, returned.Status);

        var rejected = await service.DecideAsync(draft.Id, EntityId.New(), AiDecisionType.Reject, "Still unverifiable");
        Assert.Equal(AiDraftStatus.Rejected, rejected.Status);
    }

    [Fact]
    public async Task Decided_draft_cannot_be_re_decided()
    {
        var db = NewDb();
        var service = NewService(db);
        var source = EntityId.New();

        var draft = await service.ExtractPersonFromSourceAsync(source, Transcript);
        await service.DecideAsync(draft.Id, EntityId.New(), AiDecisionType.Reject, "Wrong");

        await Assert.ThrowsAsync<DomainValidationException>(() =>
            service.DecideAsync(draft.Id, EntityId.New(), AiDecisionType.Accept, "Changed my mind"));
    }

    [Fact]
    public void Pure_engine_extracts_person_from_supplied_text()
    {
        var result = AiJobs.ExtractPerson(Transcript);

        Assert.NotNull(result);
        Assert.Equal(0.8, result!.Confidence);
        Assert.Contains("Nagarjuna", result.SuggestionJson);
    }

    [Fact]
    public void Pure_engine_normalizes_name_without_external_state()
    {
        var result = AiJobs.NormalizeName("Nāgārjuna", "devanagari");

        Assert.Contains("Nagarjuna", result.SuggestionJson);
        Assert.InRange(result.Confidence, 0.0, 1.0);
    }

    [Fact]
    public void Pure_engine_finds_duplicate_pairs_without_merging()
    {
        var pairs = AiJobs.DetectDuplicates(new[]
        {
            (EntityId.New(), "Nagarjuna"),
            (EntityId.New(), "nagarjuna"),
            (EntityId.New(), "Vasubandhu")
        });

        Assert.Single(pairs);
    }

    [Fact]
    public void Pure_engine_finds_disjoint_date_conflicts()
    {
        var conflicts = AiJobs.DetectDateConflicts(new[]
        {
            (EntityId.New(), HistoricalDate.Exact("300 CE", 300)),
            (EntityId.New(), HistoricalDate.Exact("900 CE", 900))
        });

        Assert.Single(conflicts);
    }

    [Fact]
    public void Ai_draft_rejects_out_of_range_confidence()
    {
        Assert.Throws<DomainValidationException>(() => new AiDraft(
            AiDraftKind.EntityExtraction, EntityId.New(), "input",
            AiJobs.DefaultModel, AiJobs.DefaultVersion, "{\"x\":1}", 1.5, DateTimeOffset.UtcNow));
    }

    private static DharmatlasDbContext NewDb() =>
        new(new DbContextOptionsBuilder<DharmatlasDbContext>()
            .UseInMemoryDatabase("ai-" + Guid.NewGuid())
            .Options);

    private static AiCurationService NewService(DharmatlasDbContext db) =>
        new(db, new ContributionService(db));
}
