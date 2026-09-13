namespace Dharmatlas.Domain.Entities;

/// <summary>
/// Editorial quality tier of a bibliographic source. Review surfaces show the
/// tier next to each claim; `traditional` sources never support a
/// `Documented` rendering. See docs/editorial-handbook.md.
/// </summary>
public enum SourceTier
{
    Primary,
    Scholarly,
    Traditional,
    Reference
}

/// <summary>Parses source tiers from external input and rejects unknown values.</summary>
public static class SourceTierParser
{
    private static readonly Dictionary<string, SourceTier> Normalized = new(StringComparer.OrdinalIgnoreCase)
    {
        ["primary"] = SourceTier.Primary,
        ["scholarly"] = SourceTier.Scholarly,
        ["traditional"] = SourceTier.Traditional,
        ["reference"] = SourceTier.Reference
    };

    public static SourceTier Parse(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || !Normalized.TryGetValue(raw.Trim(), out var tier))
        {
            throw new DomainValidationException(
                $"Unsupported source tier '{raw}'. Allowed: Primary, Scholarly, Traditional, Reference.");
        }

        return tier;
    }

    public static bool IsSupported(string raw) =>
        !string.IsNullOrWhiteSpace(raw) && Normalized.ContainsKey(raw.Trim());
}
