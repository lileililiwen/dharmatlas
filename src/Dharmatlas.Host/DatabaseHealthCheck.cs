using Dharmatlas.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

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
                : HealthCheckResult.Unhealthy("PostgreSQL is not reachable.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy("PostgreSQL connectivity check failed.", ex);
        }
    }
}
