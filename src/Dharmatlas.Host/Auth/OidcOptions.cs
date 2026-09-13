namespace Dharmatlas.Host.Auth;

/// <summary>
/// Configuration for the OIDC reference authentication handler. Values bind
/// from <c>Dharmatlas:Auth</c> configuration with <c>DHARMATLAS_*</c>
/// environment-variable overrides. No default credentials are substituted:
/// <c>oidc</c> mode with missing keys fails closed at startup.
/// </summary>
public sealed class OidcOptions
{
    public const string UnconfiguredMode = "unconfigured";
    public const string OidcMode = "oidc";
    public const string DevLoopbackMode = "dev-loopback";

    public string Mode { get; init; } = UnconfiguredMode;
    public string? Issuer { get; init; }
    public string? Audience { get; init; }
    public string? JwksUri { get; init; }
    public string RoleClaim { get; init; } = "roles";
    public TimeSpan JwksRefreshInterval { get; init; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Permits <c>alg=none</c> tokens. Development loopback only; never enable
    /// in production. The reference RS256 path always verifies signatures.
    /// </summary>
    public bool AllowUnsignedTokens { get; init; }

    public static OidcOptions Bind(IConfiguration configuration) => new()
    {
        Mode = (Environment.GetEnvironmentVariable("DHARMATLAS_AUTH_MODE")
            ?? configuration["Dharmatlas:Auth:Mode"]
            ?? UnconfiguredMode).Trim().ToLowerInvariant(),
        Issuer = Environment.GetEnvironmentVariable("DHARMATLAS_OIDC_ISSUER")
            ?? configuration["Dharmatlas:Auth:Oidc:Issuer"],
        Audience = Environment.GetEnvironmentVariable("DHARMATLAS_OIDC_AUDIENCE")
            ?? configuration["Dharmatlas:Auth:Oidc:Audience"],
        JwksUri = Environment.GetEnvironmentVariable("DHARMATLAS_OIDC_JWKS_URI")
            ?? configuration["Dharmatlas:Auth:Oidc:JwksUri"],
        RoleClaim = Environment.GetEnvironmentVariable("DHARMATLAS_OIDC_ROLE_CLAIM")
            ?? configuration["Dharmatlas:Auth:Oidc:RoleClaim"]
            ?? "roles",
        AllowUnsignedTokens = string.Equals(
            Environment.GetEnvironmentVariable("DHARMATLAS_ALLOW_UNSIGNED_TOKENS"),
            "true", StringComparison.OrdinalIgnoreCase),
    };

    /// <summary>Names the missing configuration keys for the selected mode.</summary>
    public string[] MissingKeys()
    {
        if (!string.Equals(Mode, OidcMode, StringComparison.Ordinal))
        {
            return Array.Empty<string>();
        }

        var missing = new List<string>(3);
        if (string.IsNullOrWhiteSpace(Issuer)) missing.Add("DHARMATLAS_OIDC_ISSUER");
        if (string.IsNullOrWhiteSpace(Audience)) missing.Add("DHARMATLAS_OIDC_AUDIENCE");
        if (string.IsNullOrWhiteSpace(JwksUri)) missing.Add("DHARMATLAS_OIDC_JWKS_URI");
        return missing.ToArray();
    }
}
