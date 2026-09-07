using System.Collections.Concurrent;

namespace Dharmatlas.Api.RateLimit;

/// <summary>
/// A simple fixed-window per-key rate limiter. Intentionally minimal: it protects
/// the read-only service from abuse without requiring infrastructure (no Redis,
/// no distributed state). Ordinary research use is unaffected because the default
/// window permits far more requests than a single researcher issues.
/// </summary>
public sealed class RateLimiter
{
    private readonly int _limit;
    private readonly TimeSpan _window;
    private readonly ConcurrentDictionary<string, Queue<DateTimeOffset>> _hits = new();

    public RateLimiter(int limit = ApiConstants.RateLimitPerMinute, TimeSpan? window = null)
    {
        _limit = Math.Max(1, limit);
        _window = window ?? TimeSpan.FromMinutes(1);
    }

    /// <summary>Records a hit for <paramref name="key"/> and returns whether it is allowed.</summary>
    public bool Allow(string key, DateTimeOffset now)
    {
        var queue = _hits.GetOrAdd(key, _ => new Queue<DateTimeOffset>());
        lock (queue)
        {
            while (queue.Count > 0 && now - queue.Peek() > _window)
            {
                queue.Dequeue();
            }

            if (queue.Count >= _limit)
            {
                return false;
            }

            queue.Enqueue(now);
            return true;
        }
    }

    /// <summary>Remaining requests in the current window for <paramref name="key"/>.</summary>
    public int Remaining(string key, DateTimeOffset now)
    {
        if (!_hits.TryGetValue(key, out var queue))
        {
            return _limit;
        }

        lock (queue)
        {
            while (queue.Count > 0 && now - queue.Peek() > _window)
            {
                queue.Dequeue();
            }

            return Math.Max(0, _limit - queue.Count);
        }
    }
}
