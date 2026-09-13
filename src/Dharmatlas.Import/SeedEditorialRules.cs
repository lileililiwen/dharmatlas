namespace Dharmatlas.Import;

/// <summary>
/// Shared editorial rules from docs/editorial-handbook.md: neutral-tone deny
/// list, placeholder tokens, and the controlled source-tier vocabulary.
/// Pure string checks reused by the validator, the publication-readiness gate,
/// and the import CLI dry-run report.
/// </summary>
public static class SeedEditorialRules
{
    public static readonly IReadOnlyList<string> ToneDenyList = new[]
    {
        "greatest", "only true", "supreme", "perfect", "highest", "sole authentic"
    };

    public static readonly IReadOnlyList<string> PlaceholderTokens = new[]
    {
        "Seed Teacher", "Seed Monastic University", "Seed Discourse", "Seed Tradition"
    };

    public static IEnumerable<string> ToneViolations(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) yield break;
        foreach (var denied in ToneDenyList)
        {
            if (text.Contains(denied, StringComparison.OrdinalIgnoreCase))
                yield return denied;
        }
    }

    public static IEnumerable<string> PlaceholderHits(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) yield break;
        foreach (var token in PlaceholderTokens)
        {
            if (text.Contains(token, StringComparison.Ordinal))
                yield return token;
        }
    }
}
