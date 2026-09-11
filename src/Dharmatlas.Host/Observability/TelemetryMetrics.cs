using System.Diagnostics.Metrics;

namespace Dharmatlas.Host.Observability;

/// <summary>Vendor-neutral, low-cardinality application measurements.</summary>
public static class TelemetryMetrics
{
    public const string MeterName = "Dharmatlas.Host";
    public const string RequestsTotal = "dharmatlas_http_requests_total";
    public const string RequestDurationMs = "dharmatlas_http_request_duration_ms";
    public const string ReadinessFailuresTotal = "dharmatlas_readiness_failures_total";
    public const string SlowQueriesTotal = "dharmatlas_slow_queries_total";
    public const string ImportRejectionsTotal = "dharmatlas_import_rejections_total";
    public const string ModerationDecisionsTotal = "dharmatlas_moderation_decisions_total";
    public const string AiDraftsTotal = "dharmatlas_ai_drafts_total";
    public const string ExportJobsTotal = "dharmatlas_export_jobs_total";

    public static readonly Meter Meter = new(MeterName, "1.0.0");
    public static readonly Counter<long> Requests = Meter.CreateCounter<long>(RequestsTotal);
    public static readonly Histogram<double> RequestDuration = Meter.CreateHistogram<double>(RequestDurationMs, "ms");
    public static readonly Counter<long> ReadinessFailures = Meter.CreateCounter<long>(ReadinessFailuresTotal);

    public static string PrometheusSnapshot() =>
        $"# TYPE {RequestsTotal} counter\n{RequestsTotal} {RequestCount}\n" +
        $"# TYPE {ReadinessFailuresTotal} counter\n{ReadinessFailuresTotal} {ReadinessFailureCount}\n";

    private static long RequestCount;
    private static long ReadinessFailureCount;

    public static void RecordRequest(string method, string route, int status, double elapsedMs)
    {
        Interlocked.Increment(ref RequestCount);
        Requests.Add(1, new KeyValuePair<string, object?>("method", method), new("route", route), new("status", status));
        RequestDuration.Record(elapsedMs, new KeyValuePair<string, object?>("route", route));
    }

    public static void RecordReadinessFailure()
    {
        Interlocked.Increment(ref ReadinessFailureCount);
        ReadinessFailures.Add(1);
    }
}
