namespace Dharmatlas.Domain.ValueObjects;

/// <summary>
/// Controlled historical-certainty vocabulary from the project-foundation
/// contract. A claim or relationship carries exactly one of these values; the
/// value must never be collapsed into an unqualified fact by the presentation
/// layer, and unsupported values are rejected at the domain boundary.
/// </summary>
public enum Certainty
{
    Documented,
    Probable,
    TraditionalAccount,
    Disputed,
    Unknown
}

/// <summary>
/// Parses certainty values from external input (API, import, AI extraction) and
/// rejects anything outside the controlled vocabulary.
/// </summary>
public static class CertaintyParser
{
    private static readonly Dictionary<string, Certainty> Normalized = new(StringComparer.OrdinalIgnoreCase)
    {
        ["documented"] = Certainty.Documented,
        ["probable"] = Certainty.Probable,
        ["traditional"] = Certainty.TraditionalAccount,
        ["traditional account"] = Certainty.TraditionalAccount,
        ["traditionalaccount"] = Certainty.TraditionalAccount,
        ["disputed"] = Certainty.Disputed,
        ["unknown"] = Certainty.Unknown
    };

    public static Certainty Parse(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new DomainValidationException("Certainty value must not be empty.");
        }

        if (!Normalized.TryGetValue(raw.Trim(), out var certainty))
        {
            throw new DomainValidationException(
                $"Unsupported certainty value '{raw}'. Allowed: Documented, Probable, Traditional Account, Disputed, Unknown.");
        }

        return certainty;
    }

    public static bool IsSupported(string raw) =>
        !string.IsNullOrWhiteSpace(raw) && Normalized.ContainsKey(raw.Trim());
}
