namespace Dharmatlas.Api.RateLimit;

/// <summary>
/// Sliding-window rate-limit storage. The in-memory implementation is the
/// default for single-instance deployments; the Postgres implementation
/// shares budgets across host instances. Both fail closed on misconfiguration
/// (see <see cref="RateLimitOptions.MissingKeys"/>).
/// </summary>
public interface IRateLimitStore
{
    bool Allow(string bucket, string key, int limit, TimeSpan window, DateTimeOffset now);
    int Remaining(string bucket, string key, int limit, TimeSpan window, DateTimeOffset now);
}
