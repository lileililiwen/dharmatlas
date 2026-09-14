namespace Dharmatlas.Host.Observability;

/// <summary>
/// Vendor-neutral error-report hook (Sentry-compatible shape). Implementations
/// MUST scrub before transmission; see <see cref="TelemetryScrubber"/>.
/// Configure the Sentry DSN via <c>DHARMATLAS_SENTRY_DSN</c>; when unset the
/// host logs scrubbed errors locally and transmits nothing.
/// </summary>
public interface IErrorReporter
{
    void Report(Exception exception, string correlationId, string route);
}

/// <summary>Scrub-then-log reporter. Transmission to Sentry is deployment opt-in.</summary>
public sealed class LoggingErrorReporter : IErrorReporter
{
    private readonly ILogger<LoggingErrorReporter> _logger;

    public LoggingErrorReporter(ILogger<LoggingErrorReporter> logger) => _logger = logger;

    public static string? SentryDsn =>
        Environment.GetEnvironmentVariable("DHARMATLAS_SENTRY_DSN");

    public void Report(Exception exception, string correlationId, string route)
    {
        var (message, data) = TelemetryScrubber.ScrubException(exception);
        using (_logger.BeginScope(new Dictionary<string, object?>
               {
                   ["correlation_id"] = correlationId,
                   ["http_route"] = route,
               }))
        {
            _logger.LogError("Unhandled request error: {ErrorMessage} (data keys: {DataKeys})",
                message, string.Join(",", data.Keys));
        }
    }
}
