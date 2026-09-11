using Dharmatlas.Api.Models;

namespace Dharmatlas.Api;

/// <summary>
/// Describes the public API surface for the /meta endpoint and documentation. The
/// endpoint list is the single source of truth for what the versioned API exposes.
/// </summary>
public static class ApiMeta
{
    public static ApiMetaView Describe() => new()
    {
        ApiVersion = ApiConstants.ApiVersion,
        SchemaVersion = ApiConstants.SchemaVersion,
        License = ApiConstants.License,
        LicenseUrl = ApiConstants.LicenseUrl,
        CacheMaxAgeSeconds = ApiConstants.CacheMaxAgeSeconds,
        RateLimitPerMinute = ApiConstants.RateLimitPerMinute,
        Endpoints = Endpoints()
    };

    public static IReadOnlyList<EndpointView> Endpoints() => new List<EndpointView>
    {
        new() { Method = "GET", Path = "/api/v1/search", Description = "Multilingual entity search (filter: q, type, region, bounded limit)." },
        new() { Method = "GET", Path = "/api/v1/timeline", Description = "Uncertainty-aware event exploration (filter: year, region, category, bounded limit)." },
        new() { Method = "GET", Path = "/api/v1/map", Description = "Time-filtered geographic features (filter: year, viewport, type, region)." },
        new() { Method = "GET", Path = "/api/v1/persons/{id}", Description = "A single published person by stable id." },
        new() { Method = "GET", Path = "/api/v1/persons", Description = "List published persons (filter: name; bounded pagination)." },
        new() { Method = "GET", Path = "/api/v1/events/{id}", Description = "A single published event with date bounds, location, participants, sources." },
        new() { Method = "GET", Path = "/api/v1/events", Description = "List published events (filter: fromYear, toYear, region, category, placeId)." },
        new() { Method = "GET", Path = "/api/v1/places/{id}", Description = "A single published place with geography, kind, activity, sources." },
        new() { Method = "GET", Path = "/api/v1/places", Description = "List published places (filter: region, kind)." },
        new() { Method = "GET", Path = "/api/v1/texts/{id}", Description = "A single published text with language and sources." },
        new() { Method = "GET", Path = "/api/v1/texts", Description = "List published texts (filter: language)." },
        new() { Method = "GET", Path = "/api/v1/institutions/{id}", Description = "A single published institution with evidence." },
        new() { Method = "GET", Path = "/api/v1/institutions", Description = "List published institutions." },
        new() { Method = "GET", Path = "/api/v1/traditions/{id}", Description = "A single published tradition with evidence." },
        new() { Method = "GET", Path = "/api/v1/traditions", Description = "List published traditions." },
        new() { Method = "GET", Path = "/api/v1/claims/{id}", Description = "A single published source-backed claim." },
        new() { Method = "GET", Path = "/api/v1/claims", Description = "List published claims (filter: subjectId)." },
        new() { Method = "GET", Path = "/api/v1/relationships", Description = "List relationships (filter: from, to, type, minCertainty)." },
        new() { Method = "GET", Path = "/api/v1/sources/{id}", Description = "A single published source record." },
        new() { Method = "GET", Path = "/api/v1/export", Description = "Reproducible bulk snapshot (schema version, license, sources)." },
        new() { Method = "GET", Path = "/api/v1/meta", Description = "API metadata: version, license, endpoints." }
    };
}
