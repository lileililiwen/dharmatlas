using System.Diagnostics.CodeAnalysis;
using Dharmatlas.Contributions.Engine;
using Dharmatlas.Contributions.Identity;
using Dharmatlas.Contributions.Models;
using Dharmatlas.Domain;
using Dharmatlas.Domain.Entities;
using Dharmatlas.Domain.ValueObjects;
using Dharmatlas.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace Dharmatlas.Contributions.Services;

/// <summary>
/// Applies the pure review engine to the data model. Contributors submit through
/// <see cref="SubmitAsync"/>; reviewers decide through <see cref="ReviewAsync"/>,
/// which is the only path that mutates published data and records an immutable,
/// field-level revision. Conflicts with an existing approved change are surfaced
/// (status <see cref="SubmissionStatus.Conflict"/>) rather than silently merged.
/// </summary>
public sealed class ContributionService : IContributionService
{
    private readonly DharmatlasDbContext _db;
    private readonly ILogger<ContributionService>? _logger;

    public ContributionService(DharmatlasDbContext db, ILogger<ContributionService>? logger = null)
    {
        _db = db;
        _logger = logger;
    }

    public Task<Submission> SubmitAsync(
        ContributionActor actor,
        SubmissionType type,
        string summary,
        string payloadJson,
        IReadOnlyList<EntityId>? sourceIds = null,
        EntityId? targetId = null,
        CancellationToken cancellationToken = default)
    {
        if (!actor.IsContributor) throw new DomainValidationException("The authenticated actor is not a contributor.");
        return SubmitAuthenticatedAsync(actor.ContributorId, type, summary, payloadJson, sourceIds, targetId, cancellationToken);
    }

    private async Task<Submission> SubmitAuthenticatedAsync(EntityId contributorId, SubmissionType type, string summary, string payloadJson, IReadOnlyList<EntityId>? sourceIds, EntityId? targetId, CancellationToken cancellationToken)
    {
        var submission = await SubmitCoreAsync(contributorId, type, summary, payloadJson, sourceIds, targetId, true, cancellationToken);
        _logger?.LogInformation("Contribution submitted {SubmissionId} by contributor {ContributorId} for type {SubmissionType}", submission.Id, contributorId, type);
        return submission;
    }

    public async Task<Submission> SubmitAsync(
        EntityId contributorId,
        SubmissionType type,
        string summary,
        string payloadJson,
        IReadOnlyList<EntityId>? sourceIds = null,
        EntityId? targetId = null,
        CancellationToken cancellationToken = default)
    {
        return await SubmitCoreAsync(contributorId, type, summary, payloadJson, sourceIds, targetId, false, cancellationToken);
    }

    private async Task<Submission> SubmitCoreAsync(
        EntityId contributorId,
        SubmissionType type,
        string summary,
        string payloadJson,
        IReadOnlyList<EntityId>? sourceIds,
        EntityId? targetId,
        bool enforceReferences,
        CancellationToken cancellationToken)
    {
        if (enforceReferences && !await _db.Contributors.AnyAsync(c => c.Id == contributorId, cancellationToken))
            throw new InvalidReferenceException("The authenticated contributor is not registered.");

        var sourceIdsValue = sourceIds ?? Array.Empty<EntityId>();
        if (enforceReferences)
        {
            var existingSources = await _db.Sources.CountAsync(s => sourceIdsValue.Contains(s.Id), cancellationToken);
            if (existingSources != sourceIdsValue.Distinct().Count())
                throw new InvalidReferenceException("Every cited source must exist before a contribution enters review.");

            if (targetId is { } target && !await TargetMatchesTypeAsync(target, type, cancellationToken))
                throw new InvalidReferenceException("The contribution target does not exist or has the wrong entity type.");
        }

        var draft = new Submission(contributorId, type, summary, payloadJson, sourceIdsValue, targetId);

        var validation = SubmissionValidator.Validate(draft);
        if (!validation.IsValid)
        {
            throw new DomainValidationException(
                "Invalid submission: " + string.Join("; ", validation.Errors));
        }

        var pending = draft.Submit() with { CreatedAt = DateTimeOffset.UtcNow, Version = 0 };
        _db.Submissions.Add(pending);
        await _db.SaveChangesAsync(cancellationToken);
        return pending;
    }

    public async Task<Submission> ReviewAsync(
        ContributionActor actor,
        EntityId submissionId,
        ReviewDecisionType decision,
        string reason,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        if (!actor.IsReviewer) throw new DomainValidationException("The authenticated actor is not a reviewer.");
        var submission = await _db.Submissions.AsNoTracking().FirstOrDefaultAsync(s => s.Id == submissionId, cancellationToken)
            ?? throw new InvalidReferenceException($"Submission {submissionId} not found.");
        if (!actor.IsAdministrator && submission.ContributorId == actor.ContributorId)
            throw new DomainValidationException("A contributor cannot review their own submission.");
        return await ReviewCoreAsync(submissionId, actor.ContributorId, decision, reason, correlationId, cancellationToken);
    }

    public async Task<Submission> ReviewAsync(
        EntityId submissionId,
        EntityId reviewerId,
        ReviewDecisionType decision,
        string reason,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        var submission = await ReviewCoreAsync(submissionId, reviewerId, decision, reason, correlationId, cancellationToken);
        _logger?.LogInformation("Moderation decision recorded for {SubmissionId} by reviewer {ReviewerId}: {Decision}", submissionId, reviewerId, decision);
        return submission;
    }

    private async Task<Submission> ReviewCoreAsync(
        EntityId submissionId,
        EntityId reviewerId,
        ReviewDecisionType decision,
        string reason,
        string? correlationId,
        CancellationToken cancellationToken)
    {
        var tracked = await _db.Submissions.FirstOrDefaultAsync(s => s.Id == submissionId, cancellationToken)
            ?? throw new InvalidReferenceException($"Submission {submissionId} not found.");

        if (tracked.Status is not (SubmissionStatus.PendingReview or SubmissionStatus.ChangesRequested))
        {
            throw new DomainValidationException($"Submission is not awaiting review (status: {tracked.Status}).");
        }

        await using var transaction = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(cancellationToken)
            : null;
        var reviewDecision = new ReviewDecision(reviewerId, decision, reason, DateTimeOffset.UtcNow, correlationId);

        // Approve: first check for a conflicting already-approved change on the same target.
        if (decision == ReviewDecisionType.Approve && tracked.TargetId is { } target)
        {
            var approved = await _db.Submissions
                .Where(s => s.TargetId == target && s.Status == SubmissionStatus.Approved && s.Id != tracked.Id)
                .ToListAsync(cancellationToken);

            if (ReviewEngine.HasConflictWithApproved(approved, tracked))
            {
                _db.Entry(tracked).Property(s => s.Status).CurrentValue = SubmissionStatus.Conflict;
                _db.Entry(tracked).Property(s => s.Version).CurrentValue = tracked.Version + 1;
                await SaveWithConcurrencyGuardAsync(cancellationToken);
                if (transaction is not null) await transaction.CommitAsync(cancellationToken);
                return tracked;
            }
        }

        var decided = ReviewEngine.RecordDecision(tracked, reviewDecision);

        if (decision == ReviewDecisionType.Approve)
        {
            await ApplyApprovedAsync(tracked, reviewDecision, cancellationToken);
        }

        // Persist the status and decision trail onto the tracked submission rather
        // than replacing the instance, which keeps the owned decision rows consistent.
        _db.Entry(tracked).Property(s => s.Status).CurrentValue = decided.Status;
        _db.Entry(tracked).Collection(s => s.Decisions).CurrentValue = decided.Decisions.ToList();
        _db.Entry(tracked).Property(s => s.Version).CurrentValue = tracked.Version + 1;
        await SaveWithConcurrencyGuardAsync(cancellationToken);
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        return decided with { Version = tracked.Version };
    }

    private async Task SaveWithConcurrencyGuardAsync(CancellationToken cancellationToken)
    {
        try { await _db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException)
        {
            throw new DomainValidationException("This submission changed while it was being reviewed. Reload it and try again.");
        }
    }

    private async Task<bool> TargetMatchesTypeAsync(EntityId targetId, SubmissionType type, CancellationToken cancellationToken)
    {
        var target = await _db.Entities.AsNoTracking().FirstOrDefaultAsync(e => e.Id == targetId, cancellationToken);
        return type switch
        {
            SubmissionType.Date => target is Event,
            SubmissionType.Name or SubmissionType.Translation => target is not null,
            SubmissionType.Event => target is Event,
            SubmissionType.Institution => target is Institution,
            SubmissionType.Person => target is Person,
            _ => target is not null
        };
    }

    public async Task<ContributionHistoryView?> GetHistoryAsync(EntityId targetId, CancellationToken cancellationToken = default)
    {
        var revisions = await _db.Revisions
            .Where(r => r.TargetId == targetId)
            .OrderByDescending(r => r.Timestamp)
            .ToListAsync(cancellationToken);

        if (revisions.Count == 0)
        {
            return null;
        }

        return new ContributionHistoryView { TargetId = targetId, Revisions = revisions };
    }

    public async Task<IReadOnlyList<SubmissionView>> GetContributorSubmissionsAsync(
        EntityId contributorId, CancellationToken cancellationToken = default)
    {
        var submissions = await _db.Submissions
            .Where(s => s.ContributorId == contributorId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);

        return submissions.Select(ToView).ToList();
    }

    public async Task<IReadOnlyList<SubmissionView>> GetReviewerQueueAsync(CancellationToken cancellationToken = default)
    {
        var submissions = await _db.Submissions
            .Where(s => s.Status == SubmissionStatus.PendingReview || s.Status == SubmissionStatus.Conflict)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);

        return submissions.Select(ToView).ToList();
    }

    private async Task ApplyApprovedAsync(Submission submission, ReviewDecision decision, CancellationToken cancellationToken)
    {
        string priorJson;
        var targetId = submission.TargetId;

        switch (submission.Type)
        {
            case SubmissionType.Date:
                var ev = await _db.Entities.OfType<Event>()
                    .FirstOrDefaultAsync(e => e.Id == targetId, cancellationToken)
                    ?? throw new InvalidReferenceException($"Target event {targetId} not found.");
                priorJson = SubmissionPayloads.WriteDate(ev.When);
                var updatedEvent = ev with { When = SubmissionPayloads.ReadDate(submission.PayloadJson) };
                _db.Entry(ev).State = EntityState.Detached;
                _db.Entities.Update(updatedEvent);
                break;

            case SubmissionType.Name:
            case SubmissionType.Translation:
                priorJson = "null";
                _db.EntityNames.Add(SubmissionPayloads.ReadName(targetId!.Value, submission.PayloadJson));
                break;

            case SubmissionType.Event:
                var newEvent = SubmissionPayloads.ReadEvent(submission.PayloadJson);
                _db.Entities.Add(newEvent);
                targetId = newEvent.Id;
                priorJson = "null";
                break;

            case SubmissionType.Source:
                var newSource = SubmissionPayloads.ReadSource(submission.PayloadJson);
                _db.Sources.Add(newSource);
                targetId = newSource.Id;
                priorJson = "null";
                break;

            case SubmissionType.Institution:
                var newInstitution = SubmissionPayloads.ReadInstitution(submission.PayloadJson);
                _db.Entities.Add(newInstitution);
                targetId = newInstitution.Id;
                priorJson = "null";
                break;

            case SubmissionType.Person:
                var newPerson = SubmissionPayloads.ReadPerson(submission.PayloadJson);
                _db.Entities.Add(newPerson);
                targetId = newPerson.Id;
                priorJson = "null";
                break;

            case SubmissionType.Relationship:
                var newRelationship = SubmissionPayloads.ReadRelationship(submission.PayloadJson);
                _db.Relationships.Add(newRelationship);
                targetId = newRelationship.Id;
                priorJson = "null";
                break;

            default:
                throw new DomainValidationException($"Unsupported submission type {submission.Type}.");
        }

        var changed = ComputeChangedFields(submission, priorJson);

        var revision = ReviewEngine.BuildRevision(
            targetId!.Value,
            priorJson,
            submission.ContributorId,
            decision.ReviewerId,
            decision.Reason,
            submission.SourceIds,
            ReviewEngine.SerializeChangedFields(changed),
            decision.Timestamp,
            decision.CorrelationId);

        _db.Revisions.Add(revision);
        // The submission status update is persisted by the caller after this returns.
    }

    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    private static IReadOnlyList<string> ComputeChangedFields(Submission submission, string priorJson) =>
        submission.Type == SubmissionType.Date
            ? ReviewEngine.DiffFields(priorJson, submission.PayloadJson)
            : new[] { submission.Type is SubmissionType.Name or SubmissionType.Translation ? "name" : "created" };

    private static SubmissionView ToView(Submission s) => new()
    {
        Id = s.Id,
        ContributorId = s.ContributorId,
        Type = s.Type,
        TargetId = s.TargetId,
        Summary = s.Summary,
        Status = s.Status,
        CreatedAt = s.CreatedAt,
        SourceCount = s.SourceIds.Count,
        LastDecisionReason = s.Decisions.LastOrDefault()?.Reason
    };
}
