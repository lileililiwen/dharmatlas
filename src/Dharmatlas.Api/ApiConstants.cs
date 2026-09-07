namespace Dharmatlas.Api;

/// <summary>
/// Shared constants for the versioned public read-only API and bulk export. These
/// values appear in responses, cache/rate-limit headers, and the export snapshot
/// metadata so consumers can detect schema and licensing at a glance.
/// </summary>
public static class ApiConstants
{
    /// <summary>API version segment used in routes such as /api/v1/....</summary>
    public const string ApiVersion = "v1";

    /// <summary>Route prefix for every public read-only endpoint.</summary>
    public const string RoutePrefix = "/api/v1";

    /// <summary>Machine-readable schema version of the API/export payloads.</summary>
    public const string SchemaVersion = "1.0.0";

    /// <summary>License applied to published data; the export carries this verbatim.</summary>
    public const string License = "CC-BY-4.0";

    /// <summary>Human-readable license location.</summary>
    public const string LicenseUrl = "https://creativecommons.org/licenses/by/4.0/";

    /// <summary>Default page size for list endpoints.</summary>
    public const int DefaultPageSize = 20;

    /// <summary>Maximum page size a client may request; larger values are clamped.</summary>
    public const int MaxPageSize = 100;

    /// <summary>Cache-Control max-age (seconds) applied to read responses.</summary>
    public const int CacheMaxAgeSeconds = 3600;

    /// <summary>Requests per minute permitted per client before a 429 response.</summary>
    public const int RateLimitPerMinute = 60;
}
