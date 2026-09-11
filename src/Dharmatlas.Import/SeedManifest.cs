using System.Text.Json;

namespace Dharmatlas.Import;

public sealed record SeedManifest
{
    public required string SchemaVersion { get; init; }
    public required SeedMetadata Metadata { get; init; }
    public IReadOnlyList<SeedSource> Sources { get; init; } = Array.Empty<SeedSource>();
    public IReadOnlyList<SeedEntity> Entities { get; init; } = Array.Empty<SeedEntity>();
    public IReadOnlyList<SeedName> Names { get; init; } = Array.Empty<SeedName>();
    public IReadOnlyList<SeedClaim> Claims { get; init; } = Array.Empty<SeedClaim>();
    public IReadOnlyList<SeedRelationship> Relationships { get; init; } = Array.Empty<SeedRelationship>();
}

public sealed record SeedMetadata
{
    public required string License { get; init; }
    public required string LicenseUrl { get; init; }
    public string? DatasetVersion { get; init; }
    public string? EditorialNote { get; init; }
}

public sealed record SeedSource
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public string? Author { get; init; }
    public string? Date { get; init; }
    public string? PublisherOrCollection { get; init; }
    public string? Identifier { get; init; }
    public string? LicenseNote { get; init; }
}

public sealed record SeedEntity
{
    public required string Id { get; init; }
    public required string Type { get; init; }
    public string? Summary { get; init; }
    public string? Region { get; init; }
    public string? ModernName { get; init; }
    public string? Kind { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public string? OriginalLanguage { get; init; }
    public string? InstitutionalForm { get; init; }
    public string? PlaceId { get; init; }
    public SeedDate? Activity { get; init; }
    public SeedDate? Date { get; init; }
    public string? Category { get; init; }
    public string? Certainty { get; init; }
    public IReadOnlyList<string> SourceIds { get; init; } = Array.Empty<string>();
}

public sealed record SeedName
{
    public required string Id { get; init; }
    public required string EntityId { get; init; }
    public required string Language { get; init; }
    public required string Script { get; init; }
    public required string Romanization { get; init; }
    public required string Value { get; init; }
    public bool IsPrimary { get; init; }
}

public sealed record SeedDate
{
    public required string Kind { get; init; }
    public required string DisplayExpression { get; init; }
    public int? Lower { get; init; }
    public int? Upper { get; init; }
}

public sealed record SeedClaim
{
    public required string Id { get; init; }
    public required string SubjectEntityId { get; init; }
    public required string Statement { get; init; }
    public required string Certainty { get; init; }
    public IReadOnlyList<string> SourceIds { get; init; } = Array.Empty<string>();
}

public sealed record SeedRelationship
{
    public required string Id { get; init; }
    public required string FromEntityId { get; init; }
    public required string ToEntityId { get; init; }
    public required string Type { get; init; }
    public required string Certainty { get; init; }
    public IReadOnlyList<string> SourceIds { get; init; } = Array.Empty<string>();
}

public static class SeedManifestJson
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public static SeedManifest Parse(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<SeedManifest>(json, Options)
                ?? throw new SeedManifestException("Seed manifest is empty.");
        }
        catch (JsonException ex)
        {
            throw new SeedManifestException("Seed manifest is not valid JSON.", ex);
        }
    }

    public static string Serialize(SeedManifest manifest) => JsonSerializer.Serialize(manifest, Options);
}

public sealed class SeedManifestException : Exception
{
    public SeedManifestException(string message) : base(message) { }
    public SeedManifestException(string message, Exception innerException) : base(message, innerException) { }
}
