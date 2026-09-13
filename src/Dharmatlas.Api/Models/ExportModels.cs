namespace Dharmatlas.Api.Models;

/// <summary>
/// A reproducible bulk snapshot of the published dataset. Records are ordered by
/// stable id so the same data always serializes identically; the only time-varying
/// field is <see cref="ExportSnapshot.GeneratedAt"/>. A downloaded snapshot remains
/// sufficient to identify records, relationships, and their sources even if the
/// hosted service is unavailable.
/// </summary>
public sealed record ExportSnapshot
{
    public required string SchemaVersion { get; init; }
    public required string DatasetRevision { get; init; }
    /// <summary>SHA-256 of the snapshot with this field omitted.</summary>
    public required string Checksum { get; init; }
    public required string License { get; init; }
    public required string LicenseUrl { get; init; }
    public required DateTimeOffset GeneratedAt { get; init; }
    public required ExportIndex Index { get; init; }
    public required IReadOnlyList<ExportEntity> Entities { get; init; }
    public required IReadOnlyList<ExportRelationship> Relationships { get; init; }
    public required IReadOnlyList<ExportSource> Sources { get; init; }
    public required IReadOnlyList<ExportClaim> Claims { get; init; }
}

/// <summary>Counts summarizing the snapshot contents.</summary>
public sealed record ExportIndex
{
    public int Entities { get; init; }
    public int Relationships { get; init; }
    public int Sources { get; init; }
    public int Claims { get; init; }
    public required IReadOnlyDictionary<string, int> ByType { get; init; }
}

/// <summary>A source in the export. Never includes private contributor data.</summary>
public sealed record ExportSource
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public string? Author { get; init; }
    public string? Date { get; init; }
    public string? Identifier { get; init; }
    public string? Tier { get; init; }
}

/// <summary>A published source-backed assertion in a bulk export.</summary>
public sealed record ExportClaim
{
    public required string Id { get; init; }
    public string? SubjectEntityId { get; init; }
    public required string Statement { get; init; }
    public required string Certainty { get; init; }
    public required string Interpretation { get; init; }
    public string? SourceLocator { get; init; }
    public required IReadOnlyList<string> SourceIds { get; init; }
}

/// <summary>An edge between two entities in the export.</summary>
public sealed record ExportRelationship
{
    public required string Id { get; init; }
    public required string From { get; init; }
    public required string To { get; init; }
    public required string Type { get; init; }
    public required string Certainty { get; init; }
    public required IReadOnlyList<string> SourceIds { get; init; }
}

/// <summary>
/// A published entity in the export. The shape is flat so heterogeneous entity
/// kinds serialize as one list; type-specific fields are null for irrelevant kinds.
/// </summary>
public sealed record ExportEntity
{
    public required string Id { get; init; }
    public required string Type { get; init; }
    public required string CanonicalName { get; init; }
    public required IReadOnlyList<NameView> Names { get; init; }
    public string? Summary { get; init; }
    public string? Region { get; init; }
    public string? Certainty { get; init; }
    public DateView? Activity { get; init; }
    public DateView? When { get; init; }
    public string? Category { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public string? ModernName { get; init; }
    public string? Kind { get; init; }
    public string? OriginalLanguage { get; init; }
    public required IReadOnlyList<string> SourceIds { get; init; }
}
