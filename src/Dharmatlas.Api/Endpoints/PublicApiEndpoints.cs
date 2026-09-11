using Dharmatlas.Api.Models;
using Dharmatlas.Api.RateLimit;
using Dharmatlas.Api.Services;
using Dharmatlas.Contributions.Identity;
using Dharmatlas.Contributions.Models;
using Dharmatlas.Contributions.Services;
using Dharmatlas.Domain;
using Dharmatlas.Domain.Entities;
using Dharmatlas.Domain.ValueObjects;
using Dharmatlas.Map.Models;
using Dharmatlas.Map.Services;
using Dharmatlas.Search.Models;
using Dharmatlas.Search.Services;
using Dharmatlas.Timeline.Models;
using Dharmatlas.Timeline.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Dharmatlas.Api.Endpoints;

/// <summary>
/// Wires the versioned, read-only public API onto a route group. The mapping is the
/// single place that turns HTTP requests into <see cref="ApiQueryService"/> calls and
/// translates domain errors into problem responses. A group-level endpoint filter
/// applies cache headers and per-client rate limiting without touching the handlers.
/// </summary>
public static class PublicApiEndpoints
{
    public static RouteGroupBuilder MapPublicApi(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiConstants.RoutePrefix)
            .WithTags("public");

        group.MapGet("/search", async (
            ISearchQueryService svc, string? q, string? type, string? region, int? limit) => await Guard(async () =>
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                throw ApiException.BadRequest("Query parameter 'q' is required.", "Provide a multilingual name or alias.");
            }

            var types = ParseEnumList<EntityType>(type, "type");
            var result = await svc.QueryAsync(new SearchQuery
            {
                Term = q,
                Types = types,
                Region = region,
                Limit = BoundedLimit(limit)
            });
            return Results.Ok(new SearchResponseView
            {
                Term = result.Term,
                Hits = result.Hits.Select(h => new SearchHitView
                {
                    Id = h.Id.ToString(), Type = h.Type.ToString(), CanonicalName = h.CanonicalName,
                    MatchedName = h.MatchedName, MatchedForm = h.MatchedForm.ToString(), Region = h.Region,
                    ActivePeriod = h.ActivePeriod, Certainty = h.Certainty.ToString(), Score = h.Score,
                    DetailRoute = DetailRoute(h.Type, h.Id)
                }).ToList()
            });
        }));

        group.MapGet("/timeline", async (
            ITimelineQueryService svc, int? fromYear, int? toYear, string? category, string? region,
            bool? includeUnknownDates, int? limit) => await Guard(async () =>
        {
            ValidateYearRange(fromYear, toYear);
            var result = await svc.QueryAsync(new TimelineQuery
            {
                FromYear = fromYear, ToYear = toYear,
                Categories = Split(category), Regions = Split(region),
                IncludeUnknownDates = includeUnknownDates ?? false,
                Limit = BoundedLimit(limit)
            });
            return Results.Ok(new TimelineResponseView
            {
                Query = new TimelineQueryView { FromYear = fromYear, ToYear = toYear, Category = category, Region = region, IncludeUnknownDates = includeUnknownDates ?? false },
                Events = result.Events.Select(e => new TimelineEventView
                {
                    Id = e.Id.ToString(), Title = e.Title, DisplayDate = e.DisplayDate,
                    Certainty = e.Certainty.ToString(), Category = e.Category, Region = e.Region,
                    LinkedEntityIds = e.LinkedEntityIds.Select(i => i.ToString()).ToList(), DetailRoute = e.DetailRoute
                }).ToList()
            });
        }));

        group.MapGet("/map", async (
            IMapQueryService svc, int? year, string? type, string? region, bool? includeUnknownActivity,
            double? minLatitude, double? maxLatitude, double? minLongitude, double? maxLongitude) => await Guard(async () =>
        {
            if (year is null || year < -5000 || year > 3000)
            {
                throw ApiException.BadRequest("Map parameter 'year' must be between -5000 and 3000.");
            }

            ValidateBounds(minLatitude, maxLatitude, -90, 90, "latitude");
            ValidateBounds(minLongitude, maxLongitude, -180, 180, "longitude");
            var result = await svc.QueryAsync(new MapQuery
            {
                Year = year.Value, Types = ParseEnumList<MapFeatureType>(type, "type"), Region = region,
                IncludeUnknownActivity = includeUnknownActivity ?? false,
                MinLatitude = minLatitude, MaxLatitude = maxLatitude,
                MinLongitude = minLongitude, MaxLongitude = maxLongitude
            });
            return Results.Ok(new MapResponseView
            {
                Year = result.Query.Year,
                Features = result.Features.Select(f => new MapFeatureView
                {
                    Id = f.Id.ToString(), Type = f.Type.ToString(), Title = f.Title,
                    ActivityExpression = f.ActivityExpression, Certainty = f.Certainty.ToString(),
                    SourceIds = f.SourceIds.Select(i => i.ToString()).ToList(), Geometry = f.Geometry,
                    Kind = f.Kind, Region = f.Region, DetailRoute = f.DetailRoute
                }).ToList(),
                Viewport = new ViewportView
                {
                    MinLatitude = result.Viewport.MinLatitude, MaxLatitude = result.Viewport.MaxLatitude,
                    MinLongitude = result.Viewport.MinLongitude, MaxLongitude = result.Viewport.MaxLongitude
                }
            });
        }));

        group.MapGet("/institutions/{id}", async (ApiQueryService svc, string id) => await Guard(async () =>
        {
            var value = await svc.GetInstitutionAsync(ParseIdOrThrow(id));
            return value is null ? NotFound(id) : Results.Ok(value);
        }));
        group.MapGet("/institutions", async (ApiQueryService svc, int? limit, string? after) => await Guard(async () =>
            Results.Ok(await svc.ListSimpleEntitiesAsync(EntityType.Institution, new Paging(limit ?? ApiConstants.DefaultPageSize, after)))));
        group.MapGet("/traditions/{id}", async (ApiQueryService svc, string id) => await Guard(async () =>
        {
            var value = await svc.GetTraditionAsync(ParseIdOrThrow(id));
            return value is null ? NotFound(id) : Results.Ok(value);
        }));
        group.MapGet("/traditions", async (ApiQueryService svc, int? limit, string? after) => await Guard(async () =>
            Results.Ok(await svc.ListSimpleEntitiesAsync(EntityType.Tradition, new Paging(limit ?? ApiConstants.DefaultPageSize, after)))));
        group.MapGet("/claims/{id}", async (ApiQueryService svc, string id) => await Guard(async () =>
        {
            var value = await svc.GetClaimAsync(ParseIdOrThrow(id));
            return value is null ? NotFound(id) : Results.Ok(value);
        }));
        group.MapGet("/claims", async (ApiQueryService svc, string? subjectId, int? limit, string? after) => await Guard(async () =>
        {
            EntityId? subject = subjectId is null ? null : ParseIdOrThrow(subjectId);
            return Results.Ok(await svc.ListClaimsAsync(subject, new Paging(limit ?? ApiConstants.DefaultPageSize, after)));
        }));

        group.MapPost("/contributions", async (
            HttpContext http, IContributionActorResolver actors, IContributionService service,
            CreateContributionRequest request, CancellationToken cancellationToken) => await ContributionGuard(async () =>
        {
            var actor = await RequireActorAsync(http, actors, cancellationToken);
            var sourceIds = request.SourceIds.Select(ParseIdOrThrow).ToList();
            var targetId = request.TargetId is null ? (EntityId?)null : ParseIdOrThrow(request.TargetId);
            var submission = await service.SubmitAsync(actor, request.Type, request.Summary, request.PayloadJson, sourceIds, targetId, cancellationToken);
            return Results.Created($"{ApiConstants.RoutePrefix}/contributions/{submission.Id}", new { submission.Id, submission.Status, submission.CreatedAt, submission.Version });
        })).RequireAuthorization("contributor");

        group.MapGet("/contributions/mine", async (
            HttpContext http, IContributionActorResolver actors, IContributionService service, CancellationToken cancellationToken) => await ContributionGuard(async () =>
        {
            var actor = await RequireActorAsync(http, actors, cancellationToken);
            RequireContributor(actor);
            return Results.Ok(await service.GetContributorSubmissionsAsync(actor.ContributorId, cancellationToken));
        })).RequireAuthorization("contributor");

        group.MapGet("/contributions/review-queue", async (
            HttpContext http, IContributionActorResolver actors, IContributionService service, CancellationToken cancellationToken) => await ContributionGuard(async () =>
        {
            var actor = await RequireActorAsync(http, actors, cancellationToken);
            RequireReviewer(actor);
            return Results.Ok(await service.GetReviewerQueueAsync(cancellationToken));
        })).RequireAuthorization("reviewer");

        group.MapPost("/contributions/{id}/review", async (
            HttpContext http, IContributionActorResolver actors, IContributionService service, string id,
            ReviewContributionRequest request, CancellationToken cancellationToken) => await ContributionGuard(async () =>
        {
            var actor = await RequireActorAsync(http, actors, cancellationToken);
            var submission = await service.ReviewAsync(actor, ParseIdOrThrow(id), request.Decision, request.Reason, http.TraceIdentifier, cancellationToken);
            return Results.Ok(new { submission.Id, submission.Status, submission.Version });
        })).RequireAuthorization("reviewer");

        group.MapGet("/persons/{id}", async (ApiQueryService svc, string id) => await Guard(async () =>
        {
            var entityId = ParseIdOrThrow(id);
            var person = await svc.GetPersonAsync(entityId);
            return person is null ? NotFound(id) : Results.Ok(person);
        }));

        group.MapGet("/persons", async (
            ApiQueryService svc, string? name, int? limit, string? after) => await Guard(async () =>
        {
            var page = await svc.ListPersonsAsync(new PersonQuery(name, new Paging(limit ?? ApiConstants.DefaultPageSize, after)));
            return Results.Ok(page);
        }));

        group.MapGet("/events/{id}", async (ApiQueryService svc, string id) => await Guard(async () =>
        {
            var entityId = ParseIdOrThrow(id);
            var ev = await svc.GetEventAsync(entityId);
            return ev is null ? NotFound(id) : Results.Ok(ev);
        }));

        group.MapGet("/events", async (
            ApiQueryService svc, int? fromYear, int? toYear, string? region, string? category,
            string? placeId, int? limit, string? after) => await Guard(async () =>
        {
            EntityId? pid = placeId is null ? null : ParseIdOrThrow(placeId);
            var page = await svc.ListEventsAsync(new EventQuery(
                fromYear, toYear, region, category, pid, new Paging(limit ?? ApiConstants.DefaultPageSize, after)));
            return Results.Ok(page);
        }));

        group.MapGet("/places/{id}", async (ApiQueryService svc, string id) => await Guard(async () =>
        {
            var entityId = ParseIdOrThrow(id);
            var place = await svc.GetPlaceAsync(entityId);
            return place is null ? NotFound(id) : Results.Ok(place);
        }));

        group.MapGet("/places", async (
            ApiQueryService svc, string? region, string? kind, int? limit, string? after) => await Guard(async () =>
        {
            PlaceKind? parsedKind = null;
            if (!string.IsNullOrWhiteSpace(kind))
            {
                if (!Enum.TryParse<PlaceKind>(kind, ignoreCase: true, out var k))
                {
                    throw ApiException.BadRequest($"Unknown place kind '{kind}'.", "Use City, Monastery, ArchaeologicalSite, Region, Mountain, or Other.");
                }

                parsedKind = k;
            }

            var page = await svc.ListPlacesAsync(new PlaceQuery(region, parsedKind, new Paging(limit ?? ApiConstants.DefaultPageSize, after)));
            return Results.Ok(page);
        }));

        group.MapGet("/texts/{id}", async (ApiQueryService svc, string id) => await Guard(async () =>
        {
            var entityId = ParseIdOrThrow(id);
            var text = await svc.GetTextAsync(entityId);
            return text is null ? NotFound(id) : Results.Ok(text);
        }));

        group.MapGet("/texts", async (
            ApiQueryService svc, string? language, int? limit, string? after) => await Guard(async () =>
        {
            var page = await svc.ListTextsAsync(new TextQuery(language, new Paging(limit ?? ApiConstants.DefaultPageSize, after)));
            return Results.Ok(page);
        }));

        group.MapGet("/relationships", async (
            ApiQueryService svc, string? from, string? to, string? type, string? minCertainty,
            int? limit, string? after) => await Guard(async () =>
        {
            EntityId? fromId = from is null ? null : ParseIdOrThrow(from);
            EntityId? toId = to is null ? null : ParseIdOrThrow(to);
            Certainty? min = null;
            if (!string.IsNullOrWhiteSpace(minCertainty))
            {
                min = Enum.TryParse<Certainty>(minCertainty, ignoreCase: true, out var c)
                    ? c
                    : throw ApiException.BadRequest($"Unknown certainty '{minCertainty}'.");
            }

            var page = await svc.ListRelationshipsAsync(new RelationshipQuery(
                fromId, toId, type, min, new Paging(limit ?? ApiConstants.DefaultPageSize, after)));
            return Results.Ok(page);
        }));

        group.MapGet("/sources/{id}", async (ApiQueryService svc, string id) => await Guard(async () =>
        {
            var entityId = ParseIdOrThrow(id);
            var source = await svc.GetSourceAsync(entityId);
            return source is null ? NotFound(id) : Results.Ok(source);
        }));

        group.MapGet("/export", async (ApiQueryService svc, HttpContext http, string? format) => await Guard(async () =>
        {
            var snapshot = await svc.BuildExportAsync(DateTimeOffset.UtcNow);
            if (string.Equals(format, "gzip", StringComparison.OrdinalIgnoreCase))
            {
                http.Response.ContentType = "application/gzip";
                http.Response.Headers["Content-Disposition"] = "attachment; filename=dharmatlas-export.json.gz";
                await ExportDelivery.WriteCompressedAsync(http.Response.Body, snapshot, http.RequestAborted);
                return Results.Empty;
            }

            return Results.Ok(snapshot);
        }));

        group.MapGet("/meta", async (ApiQueryService svc) => Results.Ok(svc.GetMeta()));

        group.AddEndpointFilter(async (context, next) =>
        {
            var http = context.HttpContext;
            var limiter = http.RequestServices.GetRequiredService<RateLimiter>();
            var key = http.Connection.RemoteIpAddress?.ToString() ?? http.Request.Headers["X-Forwarded-For"].ToString() ?? "anonymous";
            var now = DateTimeOffset.UtcNow;

            var remaining = limiter.Remaining(key, now);
            http.Response.Headers["X-RateLimit-Limit"] = ApiConstants.RateLimitPerMinute.ToString();
            http.Response.Headers["X-RateLimit-Remaining"] = remaining.ToString();

            if (!limiter.Allow(key, now))
            {
                http.Response.Headers["Retry-After"] = "60";
                return Results.StatusCode(Status429TooManyRequests);
            }

            var result = await next(context);
            if (http.Response.StatusCode is >= 200 and < 300)
            {
                http.Response.Headers["Cache-Control"] = $"public, max-age={ApiConstants.CacheMaxAgeSeconds}";
            }

            return result;
        });

        return group;
    }

    /// <summary>Registers the read-only API services. The host must also register the DbContext.</summary>
    public static IServiceCollection AddPublicApi(this IServiceCollection services)
    {
        services.AddScoped<ApiQueryService>();
        services.AddScoped<IContributionService, ContributionService>();
        services.AddScoped<IContributionActorResolver, ContributionActorResolver>();
        services.AddSingleton<RateLimiter>();
        return services;
    }

    private const int Status429TooManyRequests = 429;

    private static int BoundedLimit(int? requested) =>
        Math.Clamp(requested ?? ApiConstants.DefaultPageSize, 1, ApiConstants.MaxPageSize);

    private static IReadOnlyList<string>? Split(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static IReadOnlyList<T>? ParseEnumList<T>(string? raw, string parameter)
        where T : struct, Enum
    {
        var values = Split(raw);
        if (values is null)
        {
            return null;
        }

        var parsed = new List<T>();
        foreach (var value in values)
        {
            if (!Enum.TryParse<T>(value, true, out var item))
            {
                throw ApiException.BadRequest($"Unknown {parameter} value '{value}'.");
            }

            parsed.Add(item);
        }

        return parsed;
    }

    private static void ValidateYearRange(int? from, int? to)
    {
        if (from is < -5000 or > 3000 || to is < -5000 or > 3000 || from > to)
        {
            throw ApiException.BadRequest("Year bounds must be between -5000 and 3000, with fromYear no greater than toYear.");
        }
    }

    private static void ValidateBounds(double? min, double? max, double lower, double upper, string name)
    {
        var minOutside = min.HasValue && (min.Value < lower || min.Value > upper);
        var maxOutside = max.HasValue && (max.Value < lower || max.Value > upper);
        if (minOutside || maxOutside || min > max)
        {
            throw ApiException.BadRequest($"Map {name} bounds are invalid.", $"Use values between {lower} and {upper}, with the minimum no greater than the maximum.");
        }
    }

    private static string DetailRoute(EntityType type, EntityId id) =>
        $"/{type.ToString().ToLowerInvariant()}s/{id}";

    private static EntityId ParseIdOrThrow(string raw)
    {
        if (!EntityId.TryParse(raw, out var id))
        {
            throw ApiException.BadRequest($"'{raw}' is not a valid entity id.", "Ids are stable GUIDs.");
        }

        return id;
    }

    private static async Task<IResult> Guard(Func<Task<IResult>> action)
    {
        try
        {
            return await action();
        }
        catch (ApiException ex)
        {
            return Results.Json(ProblemDetailsView.From(ex.Error), statusCode: ex.Error.Status);
        }
    }

    private static async Task<IResult> ContributionGuard(Func<Task<IResult>> action)
    {
        try { return await action(); }
        catch (DomainValidationException ex) { return Results.Json(ProblemDetailsView.From(ApiException.BadRequest(ex.Message).Error), statusCode: 400); }
        catch (InvalidReferenceException ex) { return Results.Json(ProblemDetailsView.From(ApiException.BadRequest(ex.Message).Error), statusCode: 400); }
        catch (UnauthorizedAccessException ex) { return Results.Json(ProblemDetailsView.From(new ApiError(403, "forbidden", ex.Message)), statusCode: 403); }
    }

    private static async Task<ContributionActor> RequireActorAsync(
        HttpContext http, IContributionActorResolver actors, CancellationToken cancellationToken)
    {
        return await actors.ResolveAsync(http.User, cancellationToken)
            ?? throw new UnauthorizedAccessException("The authenticated identity is not mapped to a contributor.");
    }

    private static void RequireContributor(ContributionActor actor)
    {
        if (!actor.IsContributor) throw new UnauthorizedAccessException("The authenticated actor is not a contributor.");
    }

    private static void RequireReviewer(ContributionActor actor)
    {
        if (!actor.IsReviewer) throw new UnauthorizedAccessException("The authenticated actor is not a reviewer.");
    }

    private static IResult NotFound(string id) =>
        Results.Json(ProblemDetailsView.From(ApiException.NotFound(id).Error), statusCode: 404);
}
