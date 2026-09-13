using Dharmatlas.Host.Auth;

namespace Dharmatlas.Host.Startup;

/// <summary>
/// Fail-closed startup validation. No default credential is ever substituted:
/// a missing database connection string or a missing OIDC parameter in
/// <c>oidc</c> mode throws naming the absent key.
/// </summary>
public static class HostConfigurationValidator
{
    public static (string? ConnectionString, OidcOptions Oidc) Validate(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Dharmatlas")
            ?? Environment.GetEnvironmentVariable("DHARMATLAS_DATABASE_CONNECTION");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Dharmatlas database configuration is missing. Set ConnectionStrings:Dharmatlas or DHARMATLAS_DATABASE_CONNECTION.");
        }

        var oidc = OidcOptions.Bind(configuration);
        var missing = oidc.MissingKeys();
        if (missing.Length > 0)
        {
            throw new InvalidOperationException(
                $"Dharmatlas OIDC authentication is selected but misconfigured. Missing: {string.Join(", ", missing)}. " +
                "Set the named environment variables or switch DHARMATLAS_AUTH_MODE away from 'oidc'.");
        }

        if (string.Equals(oidc.Mode, OidcOptions.DevLoopbackMode, StringComparison.Ordinal)
            && !IsDevelopment(configuration))
        {
            throw new InvalidOperationException(
                "DHARMATLAS_AUTH_MODE=dev-loopback is a local-development stub and must not run outside Development. " +
                "Configure 'oidc' with DHARMATLAS_OIDC_ISSUER, DHARMATLAS_OIDC_AUDIENCE, and DHARMATLAS_OIDC_JWKS_URI.");
        }

        return (connectionString, oidc);
    }

    private static bool IsDevelopment(IConfiguration configuration) =>
        string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
            "Development", StringComparison.OrdinalIgnoreCase)
        || string.Equals(configuration["Environment"], "Development", StringComparison.OrdinalIgnoreCase)
        || string.Equals(Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT"),
            "Development", StringComparison.OrdinalIgnoreCase);
}
