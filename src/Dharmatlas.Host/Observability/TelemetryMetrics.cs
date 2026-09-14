using System.Diagnostics.Metrics;

namespace Dharmatlas.Host.Observability;

/// <summary>Vendor-neutral, low-cardinality application measurements.</summary>
public static class TelemetryMetrics
{
    public const string MeterName = "Dharmatlas.Host";
    public const string RequestsTotal = "dharmatlas_http_requests_total";
    public const string RequestDurationMs = "dharmatlas_http_request_duration_ms";
    public const string HttpServerDurationSeconds = "dharmatlas_http_server_duration_seconds";
    public const string ReadinessFailuresTotal = "dharmatlas_readiness_failures_total";
    public const string SlowQueriesTotal = "dharmatlas_slow_queries_total";
    public const string ImportRejectionsTotal = "dharmatlas_import_rejections_total";
    public const string ModerationDecisionsTotal = "dharmatlas_moderation_decisions_total";
    public const string AiDraftsTotal = "dharmatlas_ai_drafts_total";
    public const string ExportJobsTotal = "dharmatlas_export_jobs_total";

    public static readonly Meter Meter = new(MeterName, "1.0.0");
    public static readonly Counter<long> Requests = Meter.CreateCounter<long>(RequestsTotal);
    public static readonly Histogram<double> RequestDuration = Meter.CreateHistogram<double>(RequestDurationMs, "ms");
    public static readonly Histogram<double> HttpServerDuration = Meter.CreateHistogram<double>(HttpServerDurationSeconds, "s");
    public static readonly Counter<long> ReadinessFailures = Meter.CreateCounter<long>(ReadinessFailuresTotal);
    public static readonly Counter<long> SlowQueries = Meter.CreateCounter<long>(SlowQueriesTotal);
    public static readonly Counter<long> ImportRejections = Meter.CreateCounter<long>(ImportRejectionsTotal);
    public static readonly Counter<long> ModerationDecisions = Meter.CreateCounter<long>(ModerationDecisionsTotal);
    public static readonly Counter<long> AiDrafts = Meter.CreateCounter<long>(AiDraftsTotal);
    public static readonly Counter<long> ExportJobs = Meter.CreateCounter<long>(ExportJobsTotal);

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
        HttpServerDuration.Record(elapsedMs / 1000.0, new KeyValuePair<string, object?>("route", route));
    }

    public static void RecordSlowQuery(string route) =>
        SlowQueries.Add(1, new KeyValuePair<string, object?>("route", route));

    public static void RecordImportRejection(string reason) =>
        ImportRejections.Add(1, new KeyValuePair<string, object?>("reason", reason));

    public static void RecordModerationDecision(string decision) =>
        ModerationDecisions.Add(1, new KeyValuePair<string, object?>("decision", decision));

    public static void RecordAiDraft(string kind) =>
        AiDrafts.Add(1, new KeyValuePair<string, object?>("kind", kind));

    public static void RecordExportJob(string outcome) =>
        ExportJobs.Add(1, new KeyValuePair<string, object?>("outcome", outcome));

    public static void RecordReadinessFailure()
    {
        Interlocked.Increment(ref ReadinessFailureCount);
        ReadinessFailures.Add(1);
    }
}
