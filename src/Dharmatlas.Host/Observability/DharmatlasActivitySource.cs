using System.Diagnostics;

namespace Dharmatlas.Host.Observability;

/// <summary>
/// Vendor-neutral trace source. Uses <see cref="ActivitySource"/> (W3C trace
/// context, OTel-compatible) so no vendor SDK is required in the host. The
/// OTLP exporter endpoint is deployment configuration
/// (<c>DHARMATLAS_OTLP_ENDPOINT</c>); this library only starts activities and
/// joins them to the existing <c>X-Correlation-ID</c>.
/// </summary>
public static class DharmatlasActivitySource
{
    public const string SourceName = "Dharmatlas.Host";

    public static readonly ActivitySource Source = new(SourceName, "1.0.0");

    /// <summary>OTLP exporter endpoint configured by the deployment, if any.</summary>
    public static string? OtlpEndpoint =>
        Environment.GetEnvironmentVariable("DHARMATLAS_OTLP_ENDPOINT");

    /// <summary>
    /// Starts a request activity tagged with the correlation ID so logs,
    /// traces, and error reports join on one key.
    /// </summary>
    public static Activity? StartRequestActivity(HttpContext context, string correlationId)
    {
        var activity = Source.StartActivity(
            $"{context.Request.Method} {context.Request.Path}",
            ActivityKind.Server);
        if (activity is null)
        {
            return null;
        }

        activity.SetTag("correlation_id", correlationId);
        activity.SetTag("http.method", context.Request.Method);
        activity.SetTag("http.route", context.GetEndpoint()?.DisplayName ?? context.Request.Path.ToString());
        return activity;
    }
}
