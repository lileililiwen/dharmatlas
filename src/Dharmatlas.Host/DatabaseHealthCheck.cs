using Dharmatlas.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Dharmatlas.Host.Observability;

namespace Dharmatlas.Host;

public sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly DharmatlasDbContext _db;

    public DatabaseHealthCheck(DharmatlasDbContext db) => _db = db;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _db.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy("PostgreSQL is reachable.")
                : Unhealthy("PostgreSQL is not reachable.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Unhealthy("PostgreSQL connectivity check failed.", ex);
        }
    }

    private static HealthCheckResult Unhealthy(string message, Exception? exception = null)
    {
        TelemetryMetrics.RecordReadinessFailure();
        return HealthCheckResult.Unhealthy(message, exception);
    }
}
