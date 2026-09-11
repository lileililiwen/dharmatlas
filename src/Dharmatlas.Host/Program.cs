using Dharmatlas.Api.Endpoints;
using Dharmatlas.Host;
using Dharmatlas.Map.Services;
using Dharmatlas.Persistence;
using Dharmatlas.Search.Services;
using Dharmatlas.Timeline.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Dharmatlas")
    ?? Environment.GetEnvironmentVariable("DHARMATLAS_DATABASE_CONNECTION");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Dharmatlas database configuration is missing. Set ConnectionStrings:Dharmatlas or DHARMATLAS_DATABASE_CONNECTION.");
}

builder.Services.AddDbContext<DharmatlasDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddScoped<ITimelineQueryService, TimelineQueryService>();
builder.Services.AddScoped<IMapQueryService, MapQueryService>();
builder.Services.AddScoped<ISearchQueryService, SearchQueryService>();
builder.Services.AddPublicApi();
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: new[] { "ready" });

var app = builder.Build();

app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
});
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
app.MapPublicApi();

if (app.Configuration.GetValue<bool>("Dharmatlas:ApplyMigrations") ||
    string.Equals(Environment.GetEnvironmentVariable("DHARMATLAS_APPLY_MIGRATIONS"), "true", StringComparison.OrdinalIgnoreCase))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<DharmatlasDbContext>().Database.MigrateAsync();
}

app.Run();

public partial class Program;
