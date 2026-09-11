using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using Dharmatlas.Contributions.Engine;
using Dharmatlas.Contributions.Identity;
using Dharmatlas.Contributions.Models;
using Dharmatlas.Contributions.Services;
using Dharmatlas.Domain;
using Dharmatlas.Domain.Entities;
using Dharmatlas.Domain.ValueObjects;
using Dharmatlas.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Dharmatlas.Domain.Tests;

[SuppressMessage("ReSharper", "AccessToDisposedClosure")]
public class ContributionTests
{
    private const string DatePayload700 = "{\"DisplayExpression\":\"700 CE\",\"Kind\":\"Exact\",\"Lower\":700,\"Upper\":700}";
    private const string DatePayload800 = "{\"DisplayExpression\":\"800 CE\",\"Kind\":\"Exact\",\"Lower\":800,\"Upper\":800}";

    [Fact]
    public void Submission_without_source_is_rejected()
    {
        var draft = new Submission(EntityId.New(), SubmissionType.Date, "Fix date", DatePayload700);

        var result = SubmissionValidator.Validate(draft);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("source", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Date_submission_without_display_expression_is_rejected()
    {
        var draft = new Submission(EntityId.New(), SubmissionType.Date, "Fix date",
            "{\"Kind\":\"Exact\",\"Lower\":700,\"Upper\":700}", new[] { EntityId.New() });

        var result = SubmissionValidator.Validate(draft);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Valid_submission_passes_validation()
    {
        var draft = new Submission(EntityId.New(), SubmissionType.Date, "Fix date", DatePayload700, new[] { EntityId.New() });

        Assert.True(SubmissionValidator.Validate(draft).IsValid);
    }

    [Fact]
    public void Review_decisions_drive_status_transition()
    {
        var pending = new Submission(EntityId.New(), SubmissionType.Date, "Fix", DatePayload700, new[] { EntityId.New() }).Submit();
        var reviewer = EntityId.New();

        var approved = ReviewEngine.RecordDecision(pending, new ReviewDecision(reviewer, ReviewDecisionType.Approve, "Looks right", DateTimeOffset.UtcNow));
        var changes = ReviewEngine.RecordDecision(pending, new ReviewDecision(reviewer, ReviewDecisionType.RequestChanges, "Need more", DateTimeOffset.UtcNow));
        var rejected = ReviewEngine.RecordDecision(pending, new ReviewDecision(reviewer, ReviewDecisionType.Reject, "Wrong", DateTimeOffset.UtcNow));

        Assert.Equal(SubmissionStatus.Approved, approved.Status);
        Assert.Equal(SubmissionStatus.ChangesRequested, changes.Status);
        Assert.Equal(SubmissionStatus.Rejected, rejected.Status);
    }

    [Fact]
    public void Conflict_detected_when_approved_change_differs()
    {
        var approved = new Submission(EntityId.New(), SubmissionType.Date, "First", DatePayload700, new[] { EntityId.New() })
            .Submit().WithDecision(new ReviewDecision(EntityId.New(), ReviewDecisionType.Approve, "ok", DateTimeOffset.UtcNow));
        var same = new Submission(EntityId.New(), SubmissionType.Date, "Same", DatePayload700, new[] { EntityId.New() }).Submit();
        var different = new Submission(EntityId.New(), SubmissionType.Date, "Other", DatePayload800, new[] { EntityId.New() }).Submit();

        Assert.False(ReviewEngine.HasConflictWithApproved(new[] { approved }, same));
        Assert.True(ReviewEngine.HasConflictWithApproved(new[] { approved }, different));
    }

    [Fact]
    public void Diff_fields_identifies_changed_date_properties()
    {
        var prior = "{\"DisplayExpression\":\"650 CE\",\"Kind\":\"Exact\",\"Lower\":650,\"Upper\":650}";

        var changed = ReviewEngine.DiffFields(prior, DatePayload700);

        Assert.Contains("DisplayExpression", changed);
        Assert.Contains("Lower", changed);
        Assert.Contains("Upper", changed);
    }

    [Fact]
    public void Diff_fields_marks_new_entity_as_created()
    {
        Assert.Equal(new[] { "created" }, ReviewEngine.DiffFields(null, DatePayload700));
    }

    [Fact]
    public void Build_revision_captures_provenance()
    {
        var revision = ReviewEngine.BuildRevision(
            EntityId.New(),
            "prior",
            EntityId.New(),
            EntityId.New(),
            "Corrected per inscription",
            new[] { EntityId.New() },
            "[\"When\"]",
            DateTimeOffset.UtcNow);

        Assert.NotNull(revision.ReviewerId);
        Assert.Equal("[\"When\"]", revision.ChangedFieldsJson);
        Assert.Single(revision.SourceIds);
        Assert.Equal("Corrected per inscription", revision.Reason);
    }

    [Fact]
    public async Task Contributor_submits_event_as_pending_not_published()
    {
        var db = NewDb();
        var service = new ContributionService(db);
        var contributor = EntityId.New();

        var submission = await service.SubmitAsync(
            contributor, SubmissionType.Event, "New event",
            "{\"Summary\":\"Council of X\",\"When\":{\"DisplayExpression\":\"650 CE\",\"Kind\":\"Exact\",\"Lower\":650,\"Upper\":650}}",
            new[] { EntityId.New() });

        Assert.Equal(SubmissionStatus.PendingReview, submission.Status);
        Assert.Empty(await db.Entities.OfType<Event>().ToListAsync());
    }

    [Fact]
    public async Task Approved_date_correction_updates_published_record_and_records_revision()
    {
        var db = NewDb();
        var target = new Event { When = HistoricalDate.Exact("650 CE", 650) };
        db.Entities.Add(target);
        await db.SaveChangesAsync();

        var service = new ContributionService(db);
        var contributor = EntityId.New();
        var reviewer = EntityId.New();
        var source = EntityId.New();

        var submission = await service.SubmitAsync(
            contributor, SubmissionType.Date, "Fix to 700 CE", DatePayload700,
            new[] { source }, target.Id);

        var decided = await service.ReviewAsync(submission.Id, reviewer, ReviewDecisionType.Approve, "Inscription confirms 700", "request-700");

        Assert.Equal(SubmissionStatus.Approved, decided.Status);

        var updated = await db.Entities.OfType<Event>().FirstAsync(e => e.Id == target.Id);
        Assert.Equal("700 CE", updated.When!.DisplayExpression);

        var history = await service.GetHistoryAsync(target.Id);
        Assert.NotNull(history);
        var revision = Assert.Single(history!.Revisions);
        Assert.Equal(contributor, revision.ContributorId);
        Assert.Equal(reviewer, revision.ReviewerId);
        Assert.Equal(source, Assert.Single(revision.SourceIds));
        Assert.Equal("Inscription confirms 700", revision.Reason);
        Assert.Equal("request-700", revision.CorrelationId);
        Assert.Contains("DisplayExpression", revision.ChangedFieldsJson!);
    }

    [Fact]
    public async Task Rejected_submission_does_not_change_published_record()
    {
        var db = NewDb();
        var target = new Event { When = HistoricalDate.Exact("650 CE", 650) };
        db.Entities.Add(target);
        await db.SaveChangesAsync();

        var service = new ContributionService(db);
        var submission = await service.SubmitAsync(
            EntityId.New(), SubmissionType.Date, "Wrong fix", DatePayload800,
            new[] { EntityId.New() }, target.Id);

        var decided = await service.ReviewAsync(submission.Id, EntityId.New(), ReviewDecisionType.Reject, "Unsourced claim");

        Assert.Equal(SubmissionStatus.Rejected, decided.Status);
        Assert.Empty(await db.Revisions.Where(r => r.TargetId == target.Id).ToListAsync());
        var unchanged = await db.Entities.OfType<Event>().FirstAsync(e => e.Id == target.Id);
        Assert.Equal("650 CE", unchanged.When!.DisplayExpression);
    }

    [Fact]
    public async Task Conflicting_submission_is_surfaced_not_merged()
    {
        var db = NewDb();
        var target = new Event { When = HistoricalDate.Exact("650 CE", 650) };
        db.Entities.Add(target);
        await db.SaveChangesAsync();

        var service = new ContributionService(db);

        var first = await service.SubmitAsync(EntityId.New(), SubmissionType.Date, "To 700", DatePayload700, new[] { EntityId.New() }, target.Id);
        await service.ReviewAsync(first.Id, EntityId.New(), ReviewDecisionType.Approve, "Approved 700");

        var second = await service.SubmitAsync(EntityId.New(), SubmissionType.Date, "To 800", DatePayload800, new[] { EntityId.New() }, target.Id);
        var decided = await service.ReviewAsync(second.Id, EntityId.New(), ReviewDecisionType.Approve, "Propose 800");

        Assert.Equal(SubmissionStatus.Conflict, decided.Status);
        var updated = await db.Entities.OfType<Event>().FirstAsync(e => e.Id == target.Id);
        Assert.Equal("700 CE", updated.When!.DisplayExpression);
    }

    [Fact]
    public async Task Reviewer_queue_returns_pending_and_conflict()
    {
        var db = NewDb();
        var service = new ContributionService(db);
        var pending = await service.SubmitAsync(EntityId.New(), SubmissionType.Source, "New source",
            "{\"Title\":\"Chronicle\"}", new[] { EntityId.New() });
        var contributor = await service.SubmitAsync(EntityId.New(), SubmissionType.Event, "Draft event",
            "{\"Summary\":\"Y\"}", new[] { EntityId.New() });

        var queue = await service.GetReviewerQueueAsync();

        Assert.Contains(queue, v => v.Id == pending.Id && v.Status == SubmissionStatus.PendingReview);
        Assert.Contains(queue, v => v.Id == contributor.Id);
    }

    [Fact]
    public async Task Authenticated_submission_requires_registered_sources_and_sets_server_timestamp()
    {
        var db = NewDb();
        var contributor = new Contributor("Researcher", "subject-1");
        db.Contributors.Add(contributor);
        await db.SaveChangesAsync();
        var actor = new ContributionActor(contributor.Id, "subject-1", contributor.Roles.ToHashSet());
        var service = new ContributionService(db);

        await Assert.ThrowsAsync<InvalidReferenceException>(() => service.SubmitAsync(
            actor, SubmissionType.Source, "New source", "{\"Title\":\"Chronicle\"}", new[] { EntityId.New() }));

        var source = new Source("Chronicle");
        db.Sources.Add(source);
        await db.SaveChangesAsync();
        var submission = await service.SubmitAsync(
            actor, SubmissionType.Source, "New source", "{\"Title\":\"Chronicle\"}", new[] { source.Id });

        Assert.Equal(SubmissionStatus.PendingReview, submission.Status);
        Assert.True(submission.CreatedAt > DateTimeOffset.UtcNow.AddMinutes(-1));
        Assert.Equal(0, submission.Version);
    }

    [Fact]
    public async Task Reviewer_cannot_approve_their_own_submission()
    {
        var db = NewDb();
        var contributor = new Contributor("Reviewer", "reviewer-1", new[] { ContributorRole.Contributor, ContributorRole.Reviewer });
        var source = new Source("Chronicle");
        db.AddRange(contributor, source);
        await db.SaveChangesAsync();
        var actor = new ContributionActor(contributor.Id, "reviewer-1", contributor.Roles.ToHashSet());
        var service = new ContributionService(db);
        var submission = await service.SubmitAsync(actor, SubmissionType.Source, "New source", "{\"Title\":\"Chronicle\"}", new[] { source.Id });

        await Assert.ThrowsAsync<DomainValidationException>(() => service.ReviewAsync(
            actor, submission.Id, ReviewDecisionType.Approve, "I reviewed it"));
    }

    [Fact]
    public async Task Actor_resolver_maps_verified_subject_to_persisted_roles()
    {
        var db = NewDb();
        var contributor = new Contributor("Reviewer", "oidc-subject", new[] { ContributorRole.Reviewer });
        db.Contributors.Add(contributor);
        await db.SaveChangesAsync();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new System.Security.Claims.Claim(ClaimTypes.NameIdentifier, "oidc-subject") }, "test"));

        var actor = await new ContributionActorResolver(db).ResolveAsync(principal);

        Assert.NotNull(actor);
        Assert.Equal(contributor.Id, actor!.ContributorId);
        Assert.True(actor.IsReviewer);
        Assert.False(actor.IsContributor);
    }

    [Fact]
    public async Task Concurrent_review_of_one_submission_is_rejected_by_version_token()
    {
        var databaseName = "concurrency-" + Guid.NewGuid();
        var options = new DbContextOptionsBuilder<DharmatlasDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
        await using var writer = new DharmatlasDbContext(options);
        var submission = await new ContributionService(writer).SubmitAsync(
            EntityId.New(), SubmissionType.Source, "New source", "{\"Title\":\"Chronicle\"}", new[] { EntityId.New() });

        await using var first = new DharmatlasDbContext(options);
        await using var second = new DharmatlasDbContext(options);
        _ = await first.Submissions.SingleAsync(s => s.Id == submission.Id);
        _ = await second.Submissions.SingleAsync(s => s.Id == submission.Id);

        await new ContributionService(first).ReviewAsync(submission.Id, EntityId.New(), ReviewDecisionType.Reject, "First decision");

        await Assert.ThrowsAsync<DomainValidationException>(() =>
            new ContributionService(second).ReviewAsync(submission.Id, EntityId.New(), ReviewDecisionType.Reject, "Stale decision"));
    }

    private static DharmatlasDbContext NewDb() =>
        new(new DbContextOptionsBuilder<DharmatlasDbContext>()
            .UseInMemoryDatabase("contributions-" + Guid.NewGuid())
            .Options);
}
