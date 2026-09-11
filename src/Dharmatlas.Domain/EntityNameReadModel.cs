using System.Globalization;
using System.Text;
using Dharmatlas.Domain.Entities;

namespace Dharmatlas.Domain;

/// <summary>Shared validation and deterministic projection rules for entity names.</summary>
public static class EntityNameReadModel
{
    public const int MaxLanguageLength = 16;
    public const int MaxScriptLength = 32;
    public const int MaxRomanizationLength = 32;
    public const int MaxValueLength = 512;

    public static IReadOnlyList<EntityName> Order(IEnumerable<EntityName> names) => names
        .OrderByDescending(n => n.IsPrimary)
        .ThenBy(n => Normalize(n.Language), StringComparer.Ordinal)
        .ThenBy(n => Normalize(n.Script), StringComparer.Ordinal)
        .ThenBy(n => Normalize(n.Romanization), StringComparer.Ordinal)
        .ThenBy(n => Normalize(n.Value), StringComparer.Ordinal)
        .ThenBy(n => n.Id.ToString(), StringComparer.Ordinal)
        .ToList();

    public static string? CanonicalName(IEnumerable<EntityName> names) =>
        Order(names).FirstOrDefault()?.Value;

    public static void Validate(IEnumerable<EntityName> names)
    {
        var list = names.ToList();
        var primaryLanguages = new HashSet<string>(StringComparer.Ordinal);
        var normalizedNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (var name in list)
        {
            Require(name.Language, "language", MaxLanguageLength);
            Require(name.Script, "script", MaxScriptLength);
            Require(name.Romanization, "romanization", MaxRomanizationLength, allowEmpty: true);
            Require(name.Value, "value", MaxValueLength);

            var language = Normalize(name.Language);
            var key = string.Join('\u001f', language, Normalize(name.Script),
                Normalize(name.Romanization), Normalize(name.Value));
            if (!normalizedNames.Add(key))
            {
                throw new DomainValidationException(
                    $"Entity names must be unique after normalization; duplicate '{name.Value}'.");
            }

            if (name.IsPrimary && !primaryLanguages.Add(language))
            {
                throw new DomainValidationException(
                    $"Entity already has a primary name for language '{name.Language}'.");
            }
        }
    }

    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var decomposed = value.Trim().Normalize(NormalizationForm.FormD).ToLowerInvariant();
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static void Require(string value, string field, int maxLength, bool allowEmpty = false)
    {
        if ((!allowEmpty && string.IsNullOrWhiteSpace(value)) || value.Length > maxLength)
        {
            throw new DomainValidationException(
                $"Entity name {field} must {(allowEmpty ? "be empty or " : "be non-empty and ")}be at most {maxLength} characters.");
        }
    }
}
