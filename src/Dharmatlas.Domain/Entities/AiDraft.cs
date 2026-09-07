namespace Dharmatlas.Domain.Entities;

/// <summary>Kinds of AI-assisted suggestion the curation pipeline can produce.</summary>
public enum AiDraftKind
{
    EntityExtraction,
    RelationshipSuggestion,
    SourceSummary,
    Translation,
    NameNormalization,
    DuplicateDetection,
    DateConflict
}

/// <summary>
/// Lifecycle of an AI draft. It can never publish on its own: <see cref="Accepted"/>
/// only promotes it into the human contribution-review queue, where a separate
/// reviewer approval is still required before any published record changes.
/// </summary>
public enum AiDraftStatus
{
    /// <summary>Awaiting a human reviewer decision. Not publicly visible.</summary>
    Draft,

    /// <summary>A reviewer accepted the suggestion and promoted it to a pending submission.</summary>
    Accepted,

    /// <summary>A reviewer rejected the suggestion outright.</summary>
    Rejected
}

/// <summary>The human decision a reviewer can record on an AI draft.</summary>
public enum AiDecisionType
{
    Accept,
    Reject,
    ReturnForCorrection
}

/// <summary>
/// One human decision on an AI draft. Stored immutably so the full decision trail
/// is inspectable; the decision type drives the draft's status transition.
/// </summary>
public sealed record AiReviewDecision
{
    public EntityId Id { get; init; } = EntityId.New();
    public EntityId ReviewerId { get; init; }
    public AiDecisionType Decision { get; init; }
    public string Reason { get; init; }
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// When the reviewer edited the machine suggestion before accepting, the edited
    /// JSON is recorded alongside the original so both are auditable.
    /// </summary>
    public string? EditedSuggestionJson { get; init; }

    public AiReviewDecision(
        EntityId reviewerId,
        AiDecisionType decision,
        string reason,
        DateTimeOffset timestamp,
        string? editedSuggestionJson = null)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainValidationException("An AI review decision requires a reason.");
        }

        ReviewerId = reviewerId;
        Decision = decision;
        Reason = reason;
        Timestamp = timestamp;
        EditedSuggestionJson = editedSuggestionJson;
    }
}

/// <summary>
/// A single AI-produced suggestion. Immutable once recorded: the original input
/// text, model identity, and generated suggestion are preserved verbatim so the
/// draft can be audited and reproduced. It is labeled "AI Draft" in the UI and
/// cannot directly become a published entity.
/// </summary>
public sealed record AiDraft
{
    public EntityId Id { get; init; } = EntityId.New();
    public AiDraftKind Kind { get; init; }

    /// <summary>The source or record the job operated on (provenance anchor).</summary>
    public EntityId InputReferenceId { get; init; }

    /// <summary>The exact supplied material the model saw. Never altered after creation.</summary>
    public string InputText { get; init; }

    /// <summary>Model identifier, e.g. "dharmatlas.rule-extractor".</summary>
    public string Model { get; init; }

    /// <summary>Model version, e.g. "1.0.0".</summary>
    public string ModelVersion { get; init; }

    /// <summary>Optional recipe/template reference that produced the suggestion.</summary>
    public string? PromptRef { get; init; }

    /// <summary>The generated suggestion, serialized. Never altered after creation.</summary>
    public string SuggestionJson { get; init; }

    /// <summary>Model-reported confidence in [0, 1].</summary>
    public double Confidence { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
    public AiDraftStatus Status { get; init; } = AiDraftStatus.Draft;
    public IReadOnlyList<AiReviewDecision> Decisions { get; init; } = Array.Empty<AiReviewDecision>();

    public AiDraft(
        AiDraftKind kind,
        EntityId inputReferenceId,
        string inputText,
        string model,
        string modelVersion,
        string suggestionJson,
        double confidence,
        DateTimeOffset createdAt,
        string? promptRef = null)
    {
        if (confidence < 0 || confidence > 1)
        {
            throw new DomainValidationException("AI confidence must be between 0 and 1.");
        }

        if (string.IsNullOrWhiteSpace(inputText))
        {
            throw new DomainValidationException("An AI draft requires the supplied input text.");
        }

        if (string.IsNullOrWhiteSpace(suggestionJson))
        {
            throw new DomainValidationException("An AI draft requires a suggestion.");
        }

        Kind = kind;
        InputReferenceId = inputReferenceId;
        InputText = inputText;
        Model = model;
        ModelVersion = modelVersion;
        PromptRef = promptRef;
        SuggestionJson = suggestionJson;
        Confidence = confidence;
        CreatedAt = createdAt;
    }

    /// <summary>Record a reviewer decision, returning a new draft with updated status.</summary>
    public AiDraft WithDecision(AiReviewDecision decision)
    {
        if (Status is not AiDraftStatus.Draft)
        {
            throw new DomainValidationException(
                $"A decided AI draft cannot be re-decided (status: {Status}).");
        }

        var status = decision.Decision switch
        {
            AiDecisionType.Accept => AiDraftStatus.Accepted,
            AiDecisionType.Reject => AiDraftStatus.Rejected,
            AiDecisionType.ReturnForCorrection => AiDraftStatus.Draft,
            _ => throw new DomainValidationException($"Unsupported AI decision {decision.Decision}.")
        };

        return this with
        {
            Status = status,
            Decisions = Decisions.Append(decision).ToList()
        };
    }
}
