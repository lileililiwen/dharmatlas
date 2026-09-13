namespace Dharmatlas.Host.Security;

/// <summary>
/// Emits baseline security headers. Content-Security-Policy allows only same-
/// origin scripts, styles, images, and connections (the public client is a
/// same-origin Vite bundle; map tiles stay behind the list fallback until the
/// visual-parity change justifies wider sources). HSTS is emitted only when
/// the request already arrived over HTTPS so local HTTP development is not
/// pinned. Cross-origin writes remain denied by authentication; CORS only
/// relaxes same-policy reads for explicitly allowlisted origins.
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    public const string DefaultCsp =
        "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; " +
        "connect-src 'self'; font-src 'self'; object-src 'none'; base-uri 'self'; " +
        "frame-ancestors 'self'; form-action 'self'";

    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;

    public SecurityHeadersMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.Headers["Content-Security-Policy"] = DefaultCsp;
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

        if (context.Request.IsHttps)
        {
            context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
        }

        ApplyCors(context);

        await _next(context);
    }

    internal void ApplyCors(HttpContext context)
    {
        var origin = context.Request.Headers.Origin.ToString();
        if (string.IsNullOrWhiteSpace(origin))
        {
            return;
        }

        foreach (var allowed in AllowedOrigins(_configuration))
        {
            if (string.Equals(allowed, origin, StringComparison.OrdinalIgnoreCase))
            {
                context.Response.Headers["Access-Control-Allow-Origin"] = origin;
                context.Response.Headers["Vary"] = "Origin";
                break;
            }
        }
    }

    public static string[] AllowedOrigins(IConfiguration configuration)
    {
        var env = Environment.GetEnvironmentVariable("DHARMATLAS_CORS_ORIGINS");
        var raw = !string.IsNullOrWhiteSpace(env)
            ? env
            : configuration["Dharmatlas:Cors:AllowedOrigins"];
        if (string.IsNullOrWhiteSpace(raw))
        {
            return Array.Empty<string>();
        }

        return raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
