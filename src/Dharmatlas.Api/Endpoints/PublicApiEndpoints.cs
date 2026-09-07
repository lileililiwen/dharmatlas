using Dharmatlas.Api.Models;
using Dharmatlas.Api.RateLimit;
using Dharmatlas.Api.Services;
using Dharmatlas.Domain;
using Dharmatlas.Domain.Entities;
using Dharmatlas.Domain.ValueObjects;
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

        group.MapGet("/export", async (ApiQueryService svc) => await Guard(async () =>
        {
            var snapshot = await svc.BuildExportAsync(DateTimeOffset.UtcNow);
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
        services.AddSingleton<RateLimiter>();
        return services;
    }

    private const int Status429TooManyRequests = 429;

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

    private static IResult NotFound(string id) =>
        Results.Json(ProblemDetailsView.From(ApiException.NotFound(id).Error), statusCode: 404);
}
