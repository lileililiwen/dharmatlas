using System.Text.Json;
using System.Text.Json.Nodes;
using Dharmatlas.AI.Engine;
using Dharmatlas.Contributions.Services;
using Dharmatlas.Domain;
using Dharmatlas.Domain.Entities;
using Dharmatlas.Domain.ValueObjects;
using Dharmatlas.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dharmatlas.AI.Services;

/// <summary>
/// Applies the pure <see cref="AiJobs"/> engine to the data model and manages the
/// human review of AI drafts. Drafts are immutable records of what the model saw and
/// produced; they never write to published tables. Acceptance only promotes a draft
/// into the existing human contribution-review queue (<see cref="IContributionService"/>),
/// where a separate approval is still required before any published record changes.
/// Duplicate and date-conflict suggestions are review-only and never merge records.
/// </summary>
public sealed class AiCurationService
{
    private readonly DharmatlasDbContext _db;
    private readonly IContributionService _contributions;

    public AiCurationService(DharmatlasDbContext db, IContributionService contributions)
    {
        _db = db;
        _contributions = contributions;
    }

    /// <summary>Extract a person mention from supplied source text and store it as a draft.</summary>
    public async Task<AiDraft> ExtractPersonFromSourceAsync(
        EntityId sourceId,
        string transcript,
        CancellationToken cancellationToken = default)
    {
        var extraction = AiJobs.ExtractPerson(transcript)
            ?? throw new DomainValidationException("No person mention found in the supplied text.");

        return await StoreDraftAsync(
            AiDraftKind.EntityExtraction, sourceId, transcript,
            AiJobs.DefaultModel, AiJobs.DefaultVersion, extraction.SuggestionJson, extraction.Confidence,
            "entity-extraction/v1", cancellationToken);
    }

    /// <summary>Suggest a romanized name from supplied material and store it as a draft.</summary>
    public async Task<AiDraft> NormalizeNameAsync(
        EntityId targetId,
        string rawName,
        string script,
        CancellationToken cancellationToken = default)
    {
        var normalization = AiJobs.NormalizeName(rawName, script);

        return await StoreDraftAsync(
            AiDraftKind.NameNormalization, targetId, rawName,
            AiJobs.DefaultModel, AiJobs.DefaultVersion, normalization.SuggestionJson, normalization.Confidence,
            "name-normalization/v1", cancellationToken);
    }

    /// <summary>Suggest a relationship between two existing entities and store it as a draft.</summary>
    public async Task<AiDraft> SuggestRelationshipAsync(
        EntityId inputReferenceId,
        EntityId fromEntityId,
        EntityId toEntityId,
        string type,
        Certainty certainty,
        IReadOnlyList<EntityId> sourceIds,
        CancellationToken cancellationToken = default)
    {
        if (sourceIds.Count == 0)
        {
            throw new DomainValidationException("An AI relationship suggestion must cite at least one source.");
        }

        var suggestion = JsonSerializer.Serialize(new
        {
            fromEntityId = fromEntityId.Value,
            toEntityId = toEntityId.Value,
            type,
            certainty = certainty.ToString(),
            sourceIds = sourceIds.Select(s => s.Value).ToArray()
        });

        return await StoreDraftAsync(
            AiDraftKind.RelationshipSuggestion, inputReferenceId,
            $"{fromEntityId} -> {toEntityId} ({type})",
            AiJobs.DefaultModel, AiJobs.DefaultVersion, suggestion, 0.7,
            "relationship-suggestion/v1", cancellationToken);
    }

    /// <summary>Find possible duplicates among supplied records. Review-only; never merges.</summary>
    public async Task<IReadOnlyList<AiDraft>> SuggestDuplicatesAsync(
        IReadOnlyList<(EntityId Id, string CanonicalName)> entities,
        CancellationToken cancellationToken = default)
    {
        var pairs = AiJobs.DetectDuplicates(entities);
        var drafts = pairs.Select(p => new AiDraft(
            AiDraftKind.DuplicateDetection, p.LeftId,
            $"Possible duplicate: '{p.LeftName}' ({p.LeftId}) ~ '{p.RightName}' ({p.RightId})",
            AiJobs.DefaultModel, AiJobs.DefaultVersion,
            JsonSerializer.Serialize(new { leftId = p.LeftId.Value, rightId = p.RightId.Value, leftName = p.LeftName, rightName = p.RightName }),
            0.7, DateTimeOffset.UtcNow, "duplicate-detection/v1")).ToList();

        _db.AiDrafts.AddRange(drafts);
        await _db.SaveChangesAsync(cancellationToken);
        return drafts;
    }

    /// <summary>Find possible date contradictions among supplied records. Review-only; never alters them.</summary>
    public async Task<IReadOnlyList<AiDraft>> SuggestDateConflictsAsync(
        IReadOnlyList<(EntityId Id, HistoricalDate When)> records,
        CancellationToken cancellationToken = default)
    {
        var conflicts = AiJobs.DetectDateConflicts(records);
        var drafts = conflicts.Select(c => new AiDraft(
            AiDraftKind.DateConflict, c.LeftId,
            $"Possible date conflict: '{c.LeftExpression}' ({c.LeftLower}-{c.LeftUpper}) vs '{c.RightExpression}' ({c.RightLower}-{c.RightUpper})",
            AiJobs.DefaultModel, AiJobs.DefaultVersion,
            JsonSerializer.Serialize(new
            {
                leftId = c.LeftId.Value, rightId = c.RightId.Value,
                leftExpression = c.LeftExpression, rightExpression = c.RightExpression,
                leftLower = c.LeftLower, leftUpper = c.LeftUpper,
                rightLower = c.RightLower, rightUpper = c.RightUpper
            }),
            0.8, DateTimeOffset.UtcNow, "date-conflict/v1")).ToList();

        _db.AiDrafts.AddRange(drafts);
        await _db.SaveChangesAsync(cancellationToken);
        return drafts;
    }

    /// <summary>Record a human reviewer decision on a draft still awaiting one.</summary>
    public async Task<AiDraft> DecideAsync(
        EntityId draftId,
        EntityId reviewerId,
        AiDecisionType decision,
        string reason,
        string? editedSuggestionJson = null,
        CancellationToken cancellationToken = default)
    {
        var tracked = await _db.AiDrafts.FirstOrDefaultAsync(d => d.Id == draftId, cancellationToken)
            ?? throw new InvalidReferenceException($"AI draft {draftId} not found.");

        if (tracked.Status != AiDraftStatus.Draft)
        {
            throw new DomainValidationException($"AI draft is not awaiting a decision (status: {tracked.Status}).");
        }

        var decided = tracked.WithDecision(new AiReviewDecision(reviewerId, decision, reason, DateTimeOffset.UtcNow, editedSuggestionJson));

        _db.Entry(tracked).Property(d => d.Status).CurrentValue = decided.Status;
        _db.Entry(tracked).Collection(d => d.Decisions).CurrentValue = decided.Decisions.ToList();
        await _db.SaveChangesAsync(cancellationToken);
        return decided;
    }

    /// <summary>
    /// Publish-through-review: an accepted draft is promoted into a pending
    /// contribution submission. Approval of that submission (a separate human action)
    /// is what eventually mutates published data; this method never publishes.
    /// </summary>
    public async Task<Submission> PromoteAsync(
        EntityId draftId,
        EntityId reviewerId,
        EntityId contributorId,
        CancellationToken cancellationToken = default)
    {
        var draft = await _db.AiDrafts.FirstOrDefaultAsync(d => d.Id == draftId, cancellationToken)
            ?? throw new InvalidReferenceException($"AI draft {draftId} not found.");

        if (draft.Status != AiDraftStatus.Accepted)
        {
            throw new DomainValidationException("Only an accepted AI draft can be promoted to review.");
        }

        var (type, payloadJson, targetId) = MapSuggestion(draft);
        var sourceIds = SourceIdsOf(draft);

        return await _contributions.SubmitAsync(
            contributorId, type, $"AI draft {draft.Id} ({draft.Kind})", payloadJson, sourceIds, targetId);
    }

    public async Task<AiDraft?> GetDraftAsync(EntityId id, CancellationToken cancellationToken = default) =>
        await _db.AiDrafts.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public async Task<IReadOnlyList<AiDraft>> GetReviewQueueAsync(CancellationToken cancellationToken = default) =>
        await _db.AiDrafts
            .Where(d => d.Status == AiDraftStatus.Draft)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);

    private async Task<AiDraft> StoreDraftAsync(
        AiDraftKind kind, EntityId inputReferenceId, string inputText,
        string model, string modelVersion, string suggestionJson, double confidence, string promptRef,
        CancellationToken cancellationToken)
    {
        var draft = new AiDraft(kind, inputReferenceId, inputText, model, modelVersion, suggestionJson, confidence, DateTimeOffset.UtcNow, promptRef);
        _db.AiDrafts.Add(draft);
        await _db.SaveChangesAsync(cancellationToken);
        return draft;
    }

    private static (SubmissionType Type, string PayloadJson, EntityId? TargetId) MapSuggestion(AiDraft draft)
    {
        var node = JsonNode.Parse(draft.SuggestionJson)
            ?? throw new DomainValidationException("AI draft suggestion is not valid JSON.");

        switch (draft.Kind)
        {
            case AiDraftKind.EntityExtraction:
                var name = node["detectedName"]?.GetValue<string>()
                    ?? throw new DomainValidationException("Entity-extraction suggestion is missing a detected name.");
                return (SubmissionType.Person, JsonSerializer.Serialize(new { summary = name }), null);

            case AiDraftKind.RelationshipSuggestion:
                // Suggestion is already in the relationship payload shape; pass through.
                return (SubmissionType.Relationship, draft.SuggestionJson, null);

            case AiDraftKind.NameNormalization:
            case AiDraftKind.Translation:
                var value = node["suggestedRomanization"]?.GetValue<string>()
                    ?? throw new DomainValidationException("Name suggestion is missing a romanization.");
                var payload = JsonSerializer.Serialize(new
                {
                    language = "eng",
                    script = node["script"]?.GetValue<string>() ?? "latin",
                    romanization = "derived",
                    value,
                    isPrimary = false
                });
                return (draft.Kind == AiDraftKind.NameNormalization ? SubmissionType.Name : SubmissionType.Translation, payload, draft.InputReferenceId);

            case AiDraftKind.SourceSummary:
                var title = node["suggestedRomanization"]?.GetValue<string>() ?? node["rawName"]?.GetValue<string>()
                    ?? throw new DomainValidationException("Source-summary suggestion is missing a title.");
                return (SubmissionType.Source, JsonSerializer.Serialize(new { title }), null);

            case AiDraftKind.DuplicateDetection:
            case AiDraftKind.DateConflict:
                throw new DomainValidationException(
                    $"AI suggestions of kind {draft.Kind} are review-only and cannot be promoted to a contribution.");

            default:
                throw new DomainValidationException($"Unsupported AI draft kind {draft.Kind}.");
        }
    }

    private static IReadOnlyList<EntityId> SourceIdsOf(AiDraft draft)
    {
        if (draft.Kind == AiDraftKind.RelationshipSuggestion)
        {
            var node = JsonNode.Parse(draft.SuggestionJson);
            var ids = node?["sourceIds"]?.AsArray()
                .Select(n => EntityId.From(n!.GetValue<Guid>()))
                .ToList();
            return ids ?? new List<EntityId>();
        }

        // Other kinds anchor their provenance to the source they were derived from.
        return new[] { draft.InputReferenceId };
    }
}
