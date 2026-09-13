namespace Dharmatlas.Api.RateLimit;

/// <summary>
/// Public rate-limiter facade. Delegates to the configured
/// <see cref="IRateLimitStore"/> (in-memory by default, Postgres-backed when
/// <c>Dharmatlas:RateLimit:Mode=Postgres</c>) on the search tier so existing
/// consumers keep a stable single-key API while tiers share one store.
/// </summary>
public sealed class RateLimiter
{
    private readonly IRateLimitStore _store;
    private readonly int _limit;
    private readonly TimeSpan _window;

    public RateLimiter(int limit = ApiConstants.RateLimitPerMinute, TimeSpan? window = null)
        : this(new MemoryRateLimitStore(), limit, window)
    {
    }

    public RateLimiter(IRateLimitStore store, int limit = ApiConstants.RateLimitPerMinute, TimeSpan? window = null)
    {
        _store = store;
        _limit = Math.Max(1, limit);
        _window = window ?? TimeSpan.FromMinutes(1);
    }

    /// <summary>Records a hit for <paramref name="key"/> and returns whether it is allowed.</summary>
    public bool Allow(string key, DateTimeOffset now) =>
        _store.Allow(RateLimitPolicy.SearchTier, key, _limit, _window, now);

    /// <summary>Remaining requests in the current window for <paramref name="key"/>.</summary>
    public int Remaining(string key, DateTimeOffset now) =>
        _store.Remaining(RateLimitPolicy.SearchTier, key, _limit, _window, now);
}
