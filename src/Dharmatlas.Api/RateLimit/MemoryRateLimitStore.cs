using System.Collections.Concurrent;

namespace Dharmatlas.Api.RateLimit;

/// <summary>Single-instance sliding-window store. Bucket and key are combined
/// so search, write, and export tiers keep independent budgets per client.</summary>
public sealed class MemoryRateLimitStore : IRateLimitStore
{
    private readonly ConcurrentDictionary<string, Queue<DateTimeOffset>> _hits = new();

    public bool Allow(string bucket, string key, int limit, TimeSpan window, DateTimeOffset now)
    {
        var queue = _hits.GetOrAdd(BucketKey(bucket, key), _ => new Queue<DateTimeOffset>());
        lock (queue)
        {
            Prune(queue, now, window);
            if (queue.Count >= Math.Max(1, limit))
            {
                return false;
            }

            queue.Enqueue(now);
            return true;
        }
    }

    public int Remaining(string bucket, string key, int limit, TimeSpan window, DateTimeOffset now)
    {
        if (!_hits.TryGetValue(BucketKey(bucket, key), out var queue))
        {
            return Math.Max(1, limit);
        }

        lock (queue)
        {
            Prune(queue, now, window);
            return Math.Max(0, Math.Max(1, limit) - queue.Count);
        }
    }

    private static void Prune(Queue<DateTimeOffset> queue, DateTimeOffset now, TimeSpan window)
    {
        while (queue.Count > 0 && now - queue.Peek() > window)
        {
            queue.Dequeue();
        }
    }

    private static string BucketKey(string bucket, string key) => bucket + "\u001f" + key;
}
