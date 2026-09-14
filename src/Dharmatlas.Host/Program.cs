using Dharmatlas.Api.Endpoints;
using Dharmatlas.Api.RateLimit;
using Dharmatlas.Host;
using Dharmatlas.Host.Auth;
using Dharmatlas.Host.Security;
using Dharmatlas.Host.Startup;
using Dharmatlas.Host.Observability;
using Dharmatlas.Host.Web;
using Dharmatlas.Map.Services;
using Dharmatlas.Persistence;
using Dharmatlas.Search.Services;
using Dharmatlas.Timeline.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Fail closed: missing database or OIDC keys throw naming the absent key.
var (connectionString, oidc) = HostConfigurationValidator.Validate(builder.Configuration);

builder.Services.AddDbContext<DharmatlasDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddSingleton(oidc);

if (string.Equals(oidc.Mode, OidcOptions.OidcMode, StringComparison.Ordinal))
{
    builder.Services.AddHttpClient();
    builder.Services.AddSingleton(provider =>
    {
        var http = provider.GetRequiredService<IHttpClientFactory>().CreateClient();
        return JwksKeyProvider.FromHttp(http, provider.GetRequiredService<OidcOptions>());
    });
    builder.Services.AddAuthentication("Dharmatlas")
        .AddScheme<AuthenticationSchemeOptions, OidcReferenceHandler>("Dharmatlas", _ => { });
}
else if (string.Equals(oidc.Mode, OidcOptions.DevLoopbackMode, StringComparison.Ordinal))
{
    // NON-PRODUCTION ONLY: local loopback identities for exercising
    // contribution flows. Rejected outside Development at startup.
    builder.Services.AddAuthentication("Dharmatlas")
        .AddScheme<AuthenticationSchemeOptions, DevLoopbackHandler>("Dharmatlas", _ => { });
}
else
{
    builder.Services.AddAuthentication("Dharmatlas")
        .AddScheme<AuthenticationSchemeOptions, UnconfiguredAuthenticationHandler>("Dharmatlas", _ => { });
}

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("contributor", policy => policy.RequireAuthenticatedUser());
    options.AddPolicy("reviewer", policy => policy.RequireAuthenticatedUser());
});
builder.Services.AddScoped<ITimelineQueryService, TimelineQueryService>();
builder.Services.AddScoped<IMapQueryService, MapQueryService>();
builder.Services.AddScoped<ISearchQueryService, SearchQueryService>();
builder.Services.AddSingleton<IErrorReporter, LoggingErrorReporter>();
builder.Services.AddPublicApi(builder.Configuration);

// Postgres-backed shared budgets when selected; fail closed without a
// connection string (already validated above, so the string is present here).
var rateLimits = RateLimitOptions.Bind(builder.Configuration);
var rateLimitMissing = rateLimits.MissingKeys(connectionString);
if (rateLimitMissing.Length > 0)
{
    throw new InvalidOperationException(
        $"Dharmatlas rate limiting is misconfigured. Missing: {string.Join(", ", rateLimitMissing)}.");
}

if (string.Equals(rateLimits.Mode, RateLimitOptions.PostgresMode, StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddSingleton<IRateLimitStore>(new PostgresRateLimitStore(connectionString!));
}

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: new[] { "ready" });

var app = builder.Build();

app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseMiddleware<RequestTelemetryMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
});
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
app.MapObservability();
app.MapPublicApi();
app.MapProductization();
app.MapFallback(ProductizationEndpoints.ServeIndexWithMetaAsync);

if (string.Equals(rateLimits.Mode, RateLimitOptions.PostgresMode, StringComparison.OrdinalIgnoreCase))
{
    await PostgresRateLimitStore.EnsureTableAsync(connectionString!);
}

if (app.Configuration.GetValue<bool>("Dharmatlas:ApplyMigrations") ||
    string.Equals(Environment.GetEnvironmentVariable("DHARMATLAS_APPLY_MIGRATIONS"), "true", StringComparison.OrdinalIgnoreCase))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<DharmatlasDbContext>().Database.MigrateAsync();
}

app.Run();

public partial class Program;
