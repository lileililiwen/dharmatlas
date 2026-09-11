using Dharmatlas.Domain;
using Dharmatlas.Domain.Entities;
using Dharmatlas.Domain.ValueObjects;

namespace Dharmatlas.Import;

public sealed record SeedValidationResult
{
    public required IReadOnlyList<string> Errors { get; init; }
    public bool IsValid => Errors.Count == 0;
}

public static class SeedManifestValidator
{
    private static readonly HashSet<string> Regions = new(StringComparer.OrdinalIgnoreCase)
    {
        "India", "Central Asia", "China", "Sri Lanka", "Tibet", "Korea", "Japan"
    };

    public static SeedValidationResult Validate(SeedManifest manifest)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(manifest.SchemaVersion))
            errors.Add("schemaVersion must not be empty.");
        if (manifest.Metadata is null || string.IsNullOrWhiteSpace(manifest.Metadata.License) || string.IsNullOrWhiteSpace(manifest.Metadata.LicenseUrl))
            errors.Add("metadata requires license and licenseUrl.");

        var sourceIds = ValidateIds(manifest.Sources.Select((source, i) => (source.Id, $"sources[{i}].id")), "source", errors);
        var entityIds = ValidateIds(manifest.Entities.Select((entity, i) => (entity.Id, $"entities[{i}].id")), "entity", errors);
        ValidateIds(manifest.Names.Select((name, i) => (name.Id, $"names[{i}].id")), "name", errors);
        ValidateIds(manifest.Claims.Select((claim, i) => (claim.Id, $"claims[{i}].id")), "claim", errors);
        ValidateIds(manifest.Relationships.Select((relationship, i) => (relationship.Id, $"relationships[{i}].id")), "relationship", errors);

        foreach (var (source, index) in manifest.Sources.Select((value, index) => (value, index)))
        {
            if (string.IsNullOrWhiteSpace(source.Title)) errors.Add($"sources[{index}].title must not be empty.");
        }

        foreach (var (entity, index) in manifest.Entities.Select((value, index) => (value, index)))
        {
            var path = $"entities[{index}]";
            if (!Enum.TryParse<EntityType>(entity.Type, true, out _)) errors.Add($"{path}.type '{entity.Type}' is unsupported.");
            if (!string.IsNullOrWhiteSpace(entity.Region) && !Regions.Contains(entity.Region)) errors.Add($"{path}.region '{entity.Region}' is unsupported.");
            ValidateReferences(entity.SourceIds, sourceIds, $"{path}.sourceIds", errors);
            ValidateCoordinates(entity, path, errors);
            ValidateDate(entity.Date, $"{path}.date", errors);
            ValidateDate(entity.Activity, $"{path}.activity", errors);
            ValidateCertainty(entity.Certainty, $"{path}.certainty", errors);
            var placeId = Guid.Empty;
            if (entity.PlaceId is not null && !TryId(entity.PlaceId, out placeId)) errors.Add($"{path}.placeId '{entity.PlaceId}' is not a valid ID.");
            else if (entity.PlaceId is not null && !entityIds.Contains(placeId)) errors.Add($"{path}.placeId references unknown entity '{entity.PlaceId}'.");
        }

        var primaryByLanguage = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var normalizedNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (name, index) in manifest.Names.Select((value, index) => (value, index)))
        {
            var path = $"names[{index}]";
            if (!entityIds.Contains(ParseOrEmpty(name.EntityId))) errors.Add($"{path}.entityId references an unknown entity.");
            if (string.IsNullOrWhiteSpace(name.Language) || string.IsNullOrWhiteSpace(name.Script) || string.IsNullOrWhiteSpace(name.Value)) errors.Add($"{path} requires language, script, and value.");
            if (name.Language.Length > EntityNameReadModel.MaxLanguageLength) errors.Add($"{path}.language exceeds {EntityNameReadModel.MaxLanguageLength} characters.");
            if (name.Script.Length > EntityNameReadModel.MaxScriptLength) errors.Add($"{path}.script exceeds {EntityNameReadModel.MaxScriptLength} characters.");
            if (name.Romanization.Length > EntityNameReadModel.MaxRomanizationLength) errors.Add($"{path}.romanization exceeds {EntityNameReadModel.MaxRomanizationLength} characters.");
            if (name.Value.Length > EntityNameReadModel.MaxValueLength) errors.Add($"{path}.value exceeds {EntityNameReadModel.MaxValueLength} characters.");
            var normalizedKey = string.Join('\u001f', name.EntityId.ToLowerInvariant(), EntityNameReadModel.Normalize(name.Language), EntityNameReadModel.Normalize(name.Script), EntityNameReadModel.Normalize(name.Romanization), EntityNameReadModel.Normalize(name.Value));
            if (!normalizedNames.Add(normalizedKey)) errors.Add($"{path} duplicates a name after normalization.");
            if (name.IsPrimary && !primaryByLanguage.Add($"{name.EntityId}:{name.Language}")) errors.Add($"{path} duplicates a primary name for entity/language.");
        }

        foreach (var (claim, index) in manifest.Claims.Select((value, index) => (value, index)))
        {
            var path = $"claims[{index}]";
            if (!entityIds.Contains(ParseOrEmpty(claim.SubjectEntityId))) errors.Add($"{path}.subjectEntityId references an unknown entity.");
            if (string.IsNullOrWhiteSpace(claim.Statement)) errors.Add($"{path}.statement must not be empty.");
            ValidateCertainty(claim.Certainty, $"{path}.certainty", errors);
            ValidateReferences(claim.SourceIds, sourceIds, $"{path}.sourceIds", errors);
        }

        foreach (var (relationship, index) in manifest.Relationships.Select((value, index) => (value, index)))
        {
            var path = $"relationships[{index}]";
            var from = ParseOrEmpty(relationship.FromEntityId);
            var to = ParseOrEmpty(relationship.ToEntityId);
            if (!entityIds.Contains(from)) errors.Add($"{path}.fromEntityId references an unknown entity.");
            if (!entityIds.Contains(to)) errors.Add($"{path}.toEntityId references an unknown entity.");
            if (from != Guid.Empty && from == to) errors.Add($"{path} cannot reference the same entity at both endpoints.");
            if (string.IsNullOrWhiteSpace(relationship.Type)) errors.Add($"{path}.type must not be empty.");
            ValidateCertainty(relationship.Certainty, $"{path}.certainty", errors);
            ValidateReferences(relationship.SourceIds, sourceIds, $"{path}.sourceIds", errors);
        }

        return new SeedValidationResult { Errors = errors };
    }

    private static HashSet<Guid> ValidateIds(IEnumerable<(string Id, string Path)> values, string kind, List<string> errors)
    {
        var result = new HashSet<Guid>();
        foreach (var (raw, path) in values)
        {
            if (!TryId(raw, out var id)) { errors.Add($"{path} '{raw}' is not a valid {kind} ID."); continue; }
            if (!result.Add(id)) errors.Add($"{path} duplicates {kind} ID '{raw}'.");
        }
        return result;
    }

    private static void ValidateReferences(IEnumerable<string> rawIds, HashSet<Guid> known, string path, List<string> errors)
    {
        foreach (var (raw, index) in rawIds.Select((value, index) => (value, index)))
        {
            if (!TryId(raw, out var id) || !known.Contains(id)) errors.Add($"{path}[{index}] references unknown source '{raw}'.");
        }
    }

    private static void ValidateCoordinates(SeedEntity entity, string path, List<string> errors)
    {
        if (entity.Latitude is null != (entity.Longitude is null)) errors.Add($"{path} must provide both latitude and longitude.");
        if (entity.Latitude is < -90 or > 90) errors.Add($"{path}.latitude is outside -90..90.");
        if (entity.Longitude is < -180 or > 180) errors.Add($"{path}.longitude is outside -180..180.");
    }

    private static void ValidateDate(SeedDate? date, string path, List<string> errors)
    {
        if (date is null) return;
        if (!Enum.TryParse<HistoricalDateKind>(date.Kind, true, out var kind)) errors.Add($"{path}.kind '{date.Kind}' is unsupported.");
        if (string.IsNullOrWhiteSpace(date.DisplayExpression)) errors.Add($"{path}.displayExpression must not be empty.");
        if (date.Lower is not null && date.Upper is not null && date.Lower > date.Upper) errors.Add($"{path} has inverted normalized bounds.");
        if (kind == HistoricalDateKind.Interval && (date.Lower is null || date.Upper is null)) errors.Add($"{path} interval requires lower and upper bounds.");
        if (kind is HistoricalDateKind.Approximate or HistoricalDateKind.OpenInterval or HistoricalDateKind.Traditional && date.Lower is null && date.Upper is null) errors.Add($"{path} requires at least one normalized bound.");
    }

    private static void ValidateCertainty(string? raw, string path, List<string> errors)
    {
        if (raw is not null && !CertaintyParser.IsSupported(raw)) errors.Add($"{path} '{raw}' is unsupported.");
    }

    private static bool TryId(string raw, out Guid id) => Guid.TryParse(raw, out id);
    private static Guid ParseOrEmpty(string raw) => TryId(raw, out var id) ? id : Guid.Empty;
}
