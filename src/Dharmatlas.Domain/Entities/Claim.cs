using Dharmatlas.Domain.ValueObjects;

namespace Dharmatlas.Domain.Entities;

/// <summary>
/// Lifecycle state of a historical claim. A Draft may be unsourced while under
/// review; a Published claim must cite at least one source.
/// </summary>
public enum ClaimStatus
{
    Draft,
    Published,
    Rejected,
    Private
}

/// <summary>
/// A single historical assertion, stored independently from entities and linked
/// to one or more sources with one certainty value. Unsourced, non-draft claims
/// are not eligible for publication (project-foundation contract).
/// </summary>
public sealed record Claim
{
    public EntityId Id { get; init; } = EntityId.New();
    public string Statement { get; init; }
    public Certainty Certainty { get; init; }
    public ClaimStatus Status { get; init; }
    public IReadOnlyList<EntityId> SourceIds { get; init; }
    public EntityId? SubjectEntityId { get; init; }
    public ClaimInterpretation Interpretation { get; init; }
    public string? SourceLocator { get; init; }

    private Claim(
        string statement,
        Certainty certainty,
        ClaimStatus status,
        IReadOnlyList<EntityId> sourceIds,
        EntityId? subjectEntityId,
        ClaimInterpretation interpretation,
        string? sourceLocator)
    {
        if (string.IsNullOrWhiteSpace(statement))
        {
            throw new DomainValidationException("A claim requires a statement.");
        }

        if (status == ClaimStatus.Published && (sourceIds is null || sourceIds.Count == 0))
        {
            throw new DomainValidationException(
                "A published claim must link to at least one source.");
        }

        Statement = statement;
        Certainty = certainty;
        Status = status;
        SourceIds = sourceIds ?? Array.Empty<EntityId>();
        SubjectEntityId = subjectEntityId;
        Interpretation = interpretation;
        SourceLocator = string.IsNullOrWhiteSpace(sourceLocator) ? null : sourceLocator.Trim();
    }

    /// <summary>Creates an unpublished draft; sources are optional.</summary>
    public static Claim Draft(
        string statement,
        Certainty certainty,
        IReadOnlyList<EntityId>? sourceIds = null,
        EntityId? subjectEntityId = null,
        ClaimInterpretation interpretation = ClaimInterpretation.Historical,
        string? sourceLocator = null) =>
        new(statement, certainty, ClaimStatus.Draft, sourceIds ?? Array.Empty<EntityId>(), subjectEntityId, interpretation, sourceLocator);

    /// <summary>Creates a publishable claim; at least one source is required.</summary>
    public static Claim Publish(
        string statement,
        Certainty certainty,
        IReadOnlyList<EntityId> sourceIds,
        EntityId? subjectEntityId = null,
        ClaimInterpretation interpretation = ClaimInterpretation.Historical,
        string? sourceLocator = null)
    {
        if (sourceIds is null || sourceIds.Count == 0)
        {
            throw new DomainValidationException(
                "A published claim must link to at least one source.");
        }

        return new(statement, certainty, ClaimStatus.Published, sourceIds, subjectEntityId, interpretation, sourceLocator);
    }

    public bool IsPublishable => Status == ClaimStatus.Published || SourceIds.Count > 0;

    /// <summary>Validates invariants before a claim crosses a persistence boundary.</summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Statement))
        {
            throw new DomainValidationException("A claim requires a statement.");
        }

        if (Status == ClaimStatus.Published && SourceIds.Count == 0)
        {
            throw new DomainValidationException("A published claim must link to at least one source.");
        }

        if (SourceIds.Distinct().Count() != SourceIds.Count)
        {
            throw new DomainValidationException("A claim cannot repeat a source reference.");
        }

        if (SourceLocator is { Length: > 256 })
        {
            throw new DomainValidationException("A claim source locator must be at most 256 characters.");
        }
    }

    /// <summary>Rejects source references absent from the supplied known set.</summary>
    public void ValidateReferences(IReadOnlySet<EntityId> knownSources)
    {
        foreach (var sourceId in SourceIds)
        {
            if (!knownSources.Contains(sourceId))
            {
                throw new InvalidReferenceException($"Claim references unknown source {sourceId}.");
            }
        }
    }
}
