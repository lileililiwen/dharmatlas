using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Dharmatlas.Domain;
using Dharmatlas.Domain.Entities;
using Dharmatlas.Domain.ValueObjects;

namespace Dharmatlas.AI.Engine;

/// <summary>
/// Pure, deterministic AI curation jobs. Every job operates only on the material
/// it is handed: supplied source text, a supplied name, or supplied records. None
/// of them consult external state, invent facts, or mutate the data model, so they
/// are fully unit-testable and auditable. The "model" identity recorded with each
/// result is the deterministic rule set implemented here.
/// </summary>
public static partial class AiJobs
{
    /// <summary>Identifier recorded as the model that produced a suggestion.</summary>
    public const string DefaultModel = "dharmatlas.rule-extractor";

    /// <summary>Version of the rule set, recorded for reproducibility.</summary>
    public const string DefaultVersion = "1.0.0";

    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Extracts a person mention of the form "Name (c. YEAR CE)" from supplied text.
    /// Operates only on the supplied transcript; returns null when no mention matches.
    /// </summary>
    public static PersonExtraction? ExtractPerson(string sourceText)
    {
        if (string.IsNullOrWhiteSpace(sourceText))
        {
            return null;
        }

        var match = PersonPattern().Match(sourceText);
        if (!match.Success)
        {
            return null;
        }

        var name = match.Groups["name"].Value.Trim();
        var yearRaw = match.Groups["year"].Value;
        int? year = int.TryParse(yearRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var y) ? y : null;

        var confidence = year.HasValue ? 0.8 : 0.5;
        var suggestion = JsonSerializer.Serialize(
            new { detectedName = name, detectedYear = year, sourceText }, Options);

        return new PersonExtraction(suggestion, confidence);
    }

    /// <summary>
    /// Produces a romanization suggestion for a supplied name using a small,
    /// deterministic IAST-style substitution table. This is a reference rule set,
    /// not a trained transliteration model; it only transforms the supplied string.
    /// </summary>
    public static NameNormalization NormalizeName(string rawName, string script)
    {
        var suggested = ApplyIastRules(rawName);
        var confidence = string.Equals(suggested, rawName, StringComparison.Ordinal) ? 0.4 : 0.6;
        var suggestion = JsonSerializer.Serialize(
            new { rawName, script, suggestedRomanization = suggested }, Options);

        return new NameNormalization(suggestion, confidence);
    }

    /// <summary>
    /// Finds pairs of records that share a normalized canonical name. Pure comparison
    /// only: it never merges, links, or alters the records it is given.
    /// </summary>
    public static IReadOnlyList<DuplicatePair> DetectDuplicates(
        IReadOnlyList<(EntityId Id, string CanonicalName)> entities)
    {
        var pairs = new List<DuplicatePair>();
        for (var i = 0; i < entities.Count; i++)
        {
            for (var j = i + 1; j < entities.Count; j++)
            {
                if (Normalize(entities[i].CanonicalName) == Normalize(entities[j].CanonicalName))
                {
                    pairs.Add(new DuplicatePair(
                        entities[i].Id, entities[j].Id, entities[i].CanonicalName, entities[j].CanonicalName));
                }
            }
        }

        return pairs;
    }

    /// <summary>
    /// Finds pairs of dated records whose normalized bounds are disjoint (one ends
    /// before the other begins), indicating a possible contradiction. Pure and
    /// non-destructive: it only reports the pair, never changes either record.
    /// </summary>
    public static IReadOnlyList<DateConflictPair> DetectDateConflicts(
        IReadOnlyList<(EntityId Id, HistoricalDate When)> records)
    {
        var pairs = new List<DateConflictPair>();
        for (var i = 0; i < records.Count; i++)
        {
            for (var j = i + 1; j < records.Count; j++)
            {
                var a = records[i].When;
                var b = records[j].When;
                if (a is null || b is null)
                {
                    continue;
                }

                if (a.NormalizedUpperBound is { } au && b.NormalizedLowerBound is { } bl && au < bl)
                {
                    pairs.Add(ToConflict(records[i], records[j], au, bl));
                }
                else if (b.NormalizedUpperBound is { } bu && a.NormalizedLowerBound is { } al && bu < al)
                {
                    pairs.Add(ToConflict(records[j], records[i], bu, al));
                }
            }
        }

        return pairs;
    }

    private static DateConflictPair ToConflict(
        (EntityId Id, HistoricalDate When) earlier,
        (EntityId Id, HistoricalDate When) later,
        int earlierUpper,
        int laterLower) =>
        new(
            earlier.Id,
            later.Id,
            earlier.When.DisplayExpression,
            later.When.DisplayExpression,
            earlier.When.NormalizedLowerBound ?? earlierUpper,
            earlierUpper,
            laterLower,
            later.When.NormalizedUpperBound ?? laterLower);

    private static string Normalize(string name) =>
        string.Join(" ", name.Split((char[])[' ', '\t', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .ToLowerInvariant();

    // Minimal, explicit IAST-style substitution table. Deliberately small and
    // deterministic; a production system would swap in a real transliterator.
    private static string ApplyIastRules(string input)
    {
        var map = new Dictionary<string, string>
        {
            ["ā"] = "a", ["ī"] = "i", ["ū"] = "u", ["ṛ"] = "r", ["ṝ"] = "r",
            ["ḷ"] = "l", ["ḹ"] = "l", ["ē"] = "e", ["ō"] = "o",
            ["ṃ"] = "m", ["ḥ"] = "h", ["ñ"] = "n", ["ṅ"] = "n", ["ṇ"] = "n",
            ["ṭ"] = "t", ["ḍ"] = "d", ["ś"] = "s", ["ṣ"] = "s", ["ḻ"] = "l"
        };

        var result = input;
        foreach (var (from, to) in map)
        {
            result = result.Replace(from, to);
        }

        return result;
    }

    [GeneratedRegex(@"(?<name>[A-Z][\p{L}\.]+(\s+[A-Z][\p{L}\.]+)*)\s*\((?:c\.|circa|ca\.?)\s*(?<year>\d{1,4})\s*CE\)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex PersonPattern();

    /// <summary>Result of extracting a person mention from supplied text.</summary>
    public sealed record PersonExtraction(string SuggestionJson, double Confidence);

    /// <summary>Result of normalizing a supplied name.</summary>
    public sealed record NameNormalization(string SuggestionJson, double Confidence);

    /// <summary>A possible duplicate: two records that share a normalized name.</summary>
    public sealed record DuplicatePair(EntityId LeftId, EntityId RightId, string LeftName, string RightName);

    /// <summary>A possible date contradiction: two records with disjoint bounds.</summary>
    public sealed record DateConflictPair(
        EntityId LeftId, EntityId RightId,
        string LeftExpression, string RightExpression,
        int LeftLower, int LeftUpper, int RightLower, int RightUpper);
}
