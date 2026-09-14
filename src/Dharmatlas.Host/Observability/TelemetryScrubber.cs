using System.Text.RegularExpressions;

namespace Dharmatlas.Host.Observability;

/// <summary>
/// PII scrub for telemetry. Source text, contributor email, and authentication
/// material must never leave the host in spans, logs, or error reports.
/// </summary>
public static partial class TelemetryScrubber
{
    /// <summary>Property keys that are identity or content bearing and must be dropped.</summary>
    public static readonly IReadOnlySet<string> DeniedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "email",
        "contributor_email",
        "claim_text",
        "claimtext",
        "source_text",
        "sourcetext",
        "authorization",
        "auth",
        "sub",
        "nameidentifier",
        "token",
        "password",
        "cookie",
        "set-cookie",
    };

    [GeneratedRegex(@"[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}", RegexOptions.Compiled)]
    private static partial Regex EmailPattern();

    /// <summary>Redacts email addresses in free text. Null-safe.</summary>
    public static string? ScrubMessage(string? message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return message;
        }

        return EmailPattern().Replace(message, "[redacted-email]");
    }

    /// <summary>
    /// Returns a scrubbed copy of telemetry properties: denied keys are dropped,
    /// remaining string values are email-redacted. Never returns PII-bearing keys.
    /// </summary>
    public static IReadOnlyDictionary<string, string?> ScrubProperties(IEnumerable<KeyValuePair<string, string?>> properties)
    {
        var scrubbed = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in properties)
        {
            if (DeniedKeys.Contains(key))
            {
                continue;
            }

            scrubbed[key] = ScrubMessage(value);
        }

        return scrubbed;
    }

    /// <summary>Scrubbed error payload: redacted message plus scrubbed data entries.</summary>
    public static (string? Message, IReadOnlyDictionary<string, string?> Data) ScrubException(Exception exception)
    {
        var message = ScrubMessage(exception.Message);
        var data = exception.Data.Keys
            .OfType<object>()
            .Select(key => new KeyValuePair<string, string?>(
                Convert.ToString(key, System.Globalization.CultureInfo.InvariantCulture) ?? "unknown",
                exception.Data[key]?.ToString()))
            .ToArray();
        return (message, ScrubProperties(data));
    }
}
