using System.Diagnostics;

namespace Dharmatlas.Host.Observability;

public sealed class RequestTelemetryMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestTelemetryMiddleware> _logger;

    public RequestTelemetryMiddleware(RequestDelegate next, ILogger<RequestTelemetryMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = Guid.TryParse(context.Request.Headers["X-Correlation-ID"], out var supplied)
            ? supplied.ToString("D")
            : Guid.NewGuid().ToString("D");
        context.Response.Headers["X-Correlation-ID"] = correlationId;
        var stopwatch = Stopwatch.StartNew();
        Exception? failure = null;

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            failure = ex;
            throw;
        }
        finally
        {
            stopwatch.Stop();
            var route = context.GetEndpoint()?.DisplayName ?? context.Request.Path.ToString();
            TelemetryMetrics.RecordRequest(context.Request.Method, route, context.Response.StatusCode, stopwatch.Elapsed.TotalMilliseconds);
            using (_logger.BeginScope(new Dictionary<string, object?>
                   {
                       ["correlation_id"] = correlationId,
                       ["http_method"] = context.Request.Method,
                       ["http_route"] = route,
                       ["http_status"] = context.Response.StatusCode,
                       ["duration_ms"] = Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2)
                   }))
            {
                if (failure is not null || context.Response.StatusCode >= 500)
                    _logger.LogError(failure, "HTTP request failed");
                else
                    _logger.LogInformation("HTTP request completed");
            }
        }
    }
}
