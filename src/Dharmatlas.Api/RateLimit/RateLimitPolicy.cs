using Microsoft.AspNetCore.Http;

namespace Dharmatlas.Api.RateLimit;

/// <summary>
/// Maps request paths to rate-limit tiers. Export and write routes carry
/// smaller budgets than interactive reads so bulk clients cannot starve
/// researchers, and a denied export serves 429 with no partial body.
/// </summary>
public static class RateLimitPolicy
{
    public const string SearchTier = "search";
    public const string WriteTier = "write";
    public const string ExportTier = "export";

    public static string TierFor(PathString path)
    {
        var value = path.Value ?? string.Empty;
        if (value.EndsWith("/export", StringComparison.OrdinalIgnoreCase))
        {
            return ExportTier;
        }

        if (value.Contains("/contributions", StringComparison.OrdinalIgnoreCase)
            || value.Contains("/reviews", StringComparison.OrdinalIgnoreCase))
        {
            return WriteTier;
        }

        return SearchTier;
    }

    public static int LimitFor(string tier, RateLimitOptions options) => tier switch
    {
        ExportTier => options.ExportPerMinute,
        WriteTier => options.WritePerMinute,
        _ => options.SearchPerMinute,
    };
}
