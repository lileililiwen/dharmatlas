namespace Dharmatlas.Host.Observability;

public static class ObservabilityEndpoints
{
    public static void MapObservability(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/metrics", () => Results.Text(
            TelemetryMetrics.PrometheusSnapshot(), "text/plain; version=0.0.4"));
    }
}
