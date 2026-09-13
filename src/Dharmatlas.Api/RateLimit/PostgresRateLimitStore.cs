using Npgsql;

namespace Dharmatlas.Api.RateLimit;

/// <summary>
/// Postgres-backed sliding-window store so every host instance behind a load
/// balancer shares one budget per client. Hits live in a dedicated
/// <c>rate_limit_hits</c> table created with <c>IF NOT EXISTS</c> at startup;
/// it holds no contributor or content data, only bucket/key/timestamp rows
/// pruned on every decision. Semantics are approximate under concurrent
/// writers (two instances may each admit a final request in the same instant),
/// which errs toward availability and never toward serving partial exports.
/// </summary>
public sealed class PostgresRateLimitStore : IRateLimitStore
{
    public const string Table = "rate_limit_hits";

    private readonly string _connectionString;

    public PostgresRateLimitStore(string connectionString) => _connectionString = connectionString;

    public static async Task EnsureTableAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            $"CREATE TABLE IF NOT EXISTS {Table} (bucket TEXT NOT NULL, key TEXT NOT NULL, hit_at TIMESTAMPTZ NOT NULL); " +
            $"CREATE INDEX IF NOT EXISTS IX_{Table}_bucket_key_hit ON {Table} (bucket, key, hit_at);", connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public bool Allow(string bucket, string key, int limit, TimeSpan window, DateTimeOffset now)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();
        var cutoff = now - window;
        using var transaction = connection.BeginTransaction();
        Prune(connection, transaction, bucket, key, cutoff);
        var count = Count(connection, transaction, bucket, key, cutoff);
        if (count >= Math.Max(1, limit))
        {
            transaction.Commit();
            return false;
        }

        Insert(connection, transaction, bucket, key, now);
        transaction.Commit();
        return true;
    }

    public int Remaining(string bucket, string key, int limit, TimeSpan window, DateTimeOffset now)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();
        var cutoff = now - window;
        using var transaction = connection.BeginTransaction();
        Prune(connection, transaction, bucket, key, cutoff);
        var count = Count(connection, transaction, bucket, key, cutoff);
        transaction.Commit();
        return Math.Max(0, Math.Max(1, limit) - (int)count);
    }

    private static void Prune(NpgsqlConnection connection, NpgsqlTransaction transaction, string bucket, string key, DateTimeOffset cutoff)
    {
        using var command = new NpgsqlCommand($"DELETE FROM {Table} WHERE bucket = @bucket AND key = @key AND hit_at <= @cutoff;", connection, transaction);
        command.Parameters.AddWithValue("bucket", bucket);
        command.Parameters.AddWithValue("key", key);
        command.Parameters.AddWithValue("cutoff", cutoff);
        command.ExecuteNonQuery();
    }

    private static long Count(NpgsqlConnection connection, NpgsqlTransaction transaction, string bucket, string key, DateTimeOffset cutoff)
    {
        using var command = new NpgsqlCommand($"SELECT COUNT(*) FROM {Table} WHERE bucket = @bucket AND key = @key AND hit_at > @cutoff;", connection, transaction);
        command.Parameters.AddWithValue("bucket", bucket);
        command.Parameters.AddWithValue("key", key);
        command.Parameters.AddWithValue("cutoff", cutoff);
        return (long)command.ExecuteScalar()!;
    }

    private static void Insert(NpgsqlConnection connection, NpgsqlTransaction transaction, string bucket, string key, DateTimeOffset now)
    {
        using var command = new NpgsqlCommand($"INSERT INTO {Table} (bucket, key, hit_at) VALUES (@bucket, @key, @now);", connection, transaction);
        command.Parameters.AddWithValue("bucket", bucket);
        command.Parameters.AddWithValue("key", key);
        command.Parameters.AddWithValue("now", now);
        command.ExecuteNonQuery();
    }
}
