using Microsoft.Extensions.Configuration;

namespace Dharmatlas.Api.RateLimit;

/// <summary>
/// Tiered rate-limit budgets. Search, write, and export endpoints have
/// separate per-key budgets so bulk export bursts cannot starve interactive
/// research traffic and vice versa.
/// </summary>
public sealed class RateLimitOptions
{
    public const string MemoryMode = "Memory";
    public const string PostgresMode = "Postgres";

    public string Mode { get; init; } = MemoryMode;
    public int SearchPerMinute { get; init; } = ApiConstants.RateLimitPerMinute;
    public int WritePerMinute { get; init; } = 30;
    public int ExportPerMinute { get; init; } = 10;

    public static RateLimitOptions Bind(IConfiguration configuration) => new()
    {
        Mode = configuration["Dharmatlas:RateLimit:Mode"] ?? MemoryMode,
        SearchPerMinute = ParseLimit(configuration["Dharmatlas:RateLimit:SearchPerMinute"], ApiConstants.RateLimitPerMinute),
        WritePerMinute = ParseLimit(configuration["Dharmatlas:RateLimit:WritePerMinute"], 30),
        ExportPerMinute = ParseLimit(configuration["Dharmatlas:RateLimit:ExportPerMinute"], 10),
    };

    /// <summary>Names the missing configuration keys for the selected mode.</summary>
    public string[] MissingKeys(string? connectionString)
    {
        if (string.Equals(Mode, PostgresMode, StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(connectionString))
        {
            return new[] { "ConnectionStrings:Dharmatlas | DHARMATLAS_DATABASE_CONNECTION" };
        }

        return Array.Empty<string>();
    }

    private static int ParseLimit(string? raw, int fallback) =>
        int.TryParse(raw, out var value) && value >= 1 ? value : fallback;
}
