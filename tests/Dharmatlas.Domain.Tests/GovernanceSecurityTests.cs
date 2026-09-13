using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Dharmatlas.Api.RateLimit;
using Dharmatlas.Host.Auth;
using Dharmatlas.Host.Security;
using Dharmatlas.Host.Startup;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Dharmatlas.Domain.Tests;

public sealed class GovernanceSecurityTests
{
    private const string Issuer = "https://issuer.example";
    private const string Audience = "atlas";
    private const string Kid = "rotation-key-1";

    // ---- OIDC reference handler ----

    [Fact]
    public async Task Oidc_valid_token_maps_subject_and_roles()
    {
        using var rsa = RSA.Create(2048);
        var handler = CreateHandler(rsa, JwksDocument(rsa, Kid), AllowUnsigned: false);
        var token = Mint(rsa, Kid, Subject: "user-1", AudienceValue: Audience, Expires: Hours(1), Roles: new[] { "contributor" });

        var result = await AuthenticateAsync(handler, token);

        Assert.True(result.Succeeded);
        Assert.Equal("user-1", result.Principal!.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        Assert.Equal("user-1", result.Principal.FindFirst("sub")!.Value);
        Assert.Contains(result.Principal.FindAll(ClaimTypes.Role), c => c.Value == "contributor");
    }

    [Fact]
    public async Task Oidc_reviewer_role_maps_and_unknown_roles_are_ignored()
    {
        using var rsa = RSA.Create(2048);
        var handler = CreateHandler(rsa, JwksDocument(rsa, Kid), AllowUnsigned: false);
        var token = Mint(rsa, Kid, Subject: "reviewer-1", AudienceValue: Audience, Expires: Hours(1), Roles: new[] { "reviewer", "admin" });

        var result = await AuthenticateAsync(handler, token);

        Assert.True(result.Succeeded);
        Assert.Contains(result.Principal!.FindAll(ClaimTypes.Role), c => c.Value == "reviewer");
        Assert.DoesNotContain(result.Principal.FindAll(ClaimTypes.Role), c => c.Value == "admin");
    }

    [Fact]
    public async Task Oidc_expired_token_fails()
    {
        using var rsa = RSA.Create(2048);
        var handler = CreateHandler(rsa, JwksDocument(rsa, Kid), AllowUnsigned: false);
        var token = Mint(rsa, Kid, Subject: "user-1", AudienceValue: Audience, Expires: Hours(-1), Roles: new[] { "contributor" });

        var result = await AuthenticateAsync(handler, token);

        Assert.False(result.Succeeded);
        Assert.Contains("expired", result.Failure!.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Oidc_wrong_audience_fails()
    {
        using var rsa = RSA.Create(2048);
        var handler = CreateHandler(rsa, JwksDocument(rsa, Kid), AllowUnsigned: false);
        var token = Mint(rsa, Kid, Subject: "user-1", AudienceValue: "another-app", Expires: Hours(1), Roles: new[] { "contributor" });

        var result = await AuthenticateAsync(handler, token);

        Assert.False(result.Succeeded);
        Assert.Contains("audience", result.Failure!.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Oidc_wrong_issuer_fails()
    {
        using var rsa = RSA.Create(2048);
        var handler = CreateHandler(rsa, JwksDocument(rsa, Kid), AllowUnsigned: false);
        var token = Mint(rsa, Kid, Subject: "user-1", AudienceValue: Audience, Expires: Hours(1), Roles: new[] { "contributor" }, IssuerValue: "https://evil.example");

        var result = await AuthenticateAsync(handler, token);

        Assert.False(result.Succeeded);
        Assert.Contains("issuer", result.Failure!.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Oidc_tampered_signature_fails()
    {
        using var rsa = RSA.Create(2048);
        var handler = CreateHandler(rsa, JwksDocument(rsa, Kid), AllowUnsigned: false);
        var token = Mint(rsa, Kid, Subject: "user-1", AudienceValue: Audience, Expires: Hours(1), Roles: new[] { "contributor" });
        var tampered = token[..^4] + (token[^4] == 'A' ? "BBBB" : "AAAA");

        var result = await AuthenticateAsync(handler, tampered);

        Assert.False(result.Succeeded);
        Assert.Contains("signature", result.Failure!.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Oidc_unsigned_tokens_rejected_unless_dev_loopback_allows_them()
    {
        using var rsa = RSA.Create(2048);
        var strict = CreateHandler(rsa, JwksDocument(rsa, Kid), AllowUnsigned: false);
        var permissive = CreateHandler(rsa, JwksDocument(rsa, Kid), AllowUnsigned: true);
        var unsigned = UnsignedToken(Subject: "dev-1", AudienceValue: Audience, Expires: Hours(1));

        Assert.False((await AuthenticateAsync(strict, unsigned)).Succeeded);
        var allowed = await AuthenticateAsync(permissive, unsigned);
        Assert.True(allowed.Succeeded);
        Assert.Equal("dev-1", allowed.Principal!.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    }

    [Fact]
    public async Task Oidc_missing_authorization_header_yields_no_result()
    {
        using var rsa = RSA.Create(2048);
        var handler = CreateHandler(rsa, JwksDocument(rsa, Kid), AllowUnsigned: false);
        var context = new DefaultHttpContext();

        await handler.InitializeAsync(new AuthenticationScheme("Dharmatlas", null, typeof(OidcReferenceHandler)), context);
        var result = await handler.AuthenticateAsync();

        Assert.False(result.Succeeded);
        Assert.Null(result.Failure);
    }

    [Fact]
    public async Task Jwks_unknown_kid_refreshes_once_and_rotation_is_picked_up()
    {
        using var oldKey = RSA.Create(2048);
        using var rotatedKey = RSA.Create(2048);
        var fetches = 0;
        var documents = new Queue<string>(new[]
        {
            JwksDocument(oldKey, "old-key"),
            JwksDocument(rotatedKey, Kid),
        });
        var provider = new JwksKeyProvider(cancellationToken =>
        {
            fetches++;
            return Task.FromResult(documents.Dequeue());
        }, TimeSpan.FromHours(1));

        var key = await provider.GetKeyAsync(Kid);

        Assert.NotNull(key);
        Assert.Equal(2, fetches);
    }

    [Fact]
    public async Task Jwks_still_unknown_after_refresh_returns_null()
    {
        var fetches = 0;
        using var rsa = RSA.Create(2048);
        var document = JwksDocument(rsa, Kid);
        var provider = new JwksKeyProvider(cancellationToken =>
        {
            fetches++;
            return Task.FromResult(document);
        }, TimeSpan.FromHours(1));

        Assert.Null(await provider.GetKeyAsync("no-such-key"));
        Assert.Equal(2, fetches);
    }

    // ---- Fail-closed startup ----

    [Fact]
    public void Startup_missing_database_names_the_key()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();

        var error = Assert.Throws<InvalidOperationException>(() => HostConfigurationValidator.Validate(configuration));

        Assert.Contains("DHARMATLAS_DATABASE_CONNECTION", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Startup_oidc_mode_without_parameters_names_the_keys()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Dharmatlas"] = "Host=localhost;Database=test",
                ["Dharmatlas:Auth:Mode"] = "oidc",
            })
            .Build();

        var error = Assert.Throws<InvalidOperationException>(() => HostConfigurationValidator.Validate(configuration));

        Assert.Contains("DHARMATLAS_OIDC_ISSUER", error.Message, StringComparison.Ordinal);
        Assert.Contains("DHARMATLAS_OIDC_AUDIENCE", error.Message, StringComparison.Ordinal);
        Assert.Contains("DHARMATLAS_OIDC_JWKS_URI", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Startup_dev_loopback_outside_development_is_refused()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Dharmatlas"] = "Host=localhost;Database=test",
                ["Dharmatlas:Auth:Mode"] = "dev-loopback",
            })
            .Build();

        var error = Assert.Throws<InvalidOperationException>(() => HostConfigurationValidator.Validate(configuration));

        Assert.Contains("dev-loopback", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ---- Security headers ----

    [Fact]
    public async Task Security_headers_are_emitted_without_hsts_on_plain_http()
    {
        var (context, _) = await InvokeHeadersAsync(scheme: "http", origin: null, allowedOrigins: null);

        Assert.Equal(SecurityHeadersMiddleware.DefaultCsp, context.Response.Headers["Content-Security-Policy"].ToString());
        Assert.Equal("nosniff", context.Response.Headers["X-Content-Type-Options"].ToString());
        Assert.Equal("SAMEORIGIN", context.Response.Headers["X-Frame-Options"].ToString());
        Assert.False(context.Response.Headers.ContainsKey("Strict-Transport-Security"));
        Assert.False(context.Response.Headers.ContainsKey("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task Security_headers_pin_hsts_on_https_and_echo_allowlisted_origins()
    {
        var (context, _) = await InvokeHeadersAsync(scheme: "https", origin: "https://atlas.example", allowedOrigins: "https://atlas.example");

        Assert.Equal("max-age=31536000; includeSubDomains", context.Response.Headers["Strict-Transport-Security"].ToString());
        Assert.Equal("https://atlas.example", context.Response.Headers["Access-Control-Allow-Origin"].ToString());
    }

    [Fact]
    public async Task Security_headers_never_echo_unlisted_origins()
    {
        var (context, _) = await InvokeHeadersAsync(scheme: "https", origin: "https://evil.example", allowedOrigins: "https://atlas.example");

        Assert.False(context.Response.Headers.ContainsKey("Access-Control-Allow-Origin"));
    }

    // ---- Tiered rate limits ----

    [Fact]
    public void Rate_limit_tiers_route_export_write_and_search_separately()
    {
        Assert.Equal(RateLimitPolicy.ExportTier, RateLimitPolicy.TierFor("/api/v1/export"));
        Assert.Equal(RateLimitPolicy.WriteTier, RateLimitPolicy.TierFor("/api/v1/contributions"));
        Assert.Equal(RateLimitPolicy.WriteTier, RateLimitPolicy.TierFor("/api/v1/reviews/abc"));
        Assert.Equal(RateLimitPolicy.SearchTier, RateLimitPolicy.TierFor("/api/v1/search"));

        var options = new RateLimitOptions();
        Assert.Equal(10, RateLimitPolicy.LimitFor(RateLimitPolicy.ExportTier, options));
        Assert.Equal(30, RateLimitPolicy.LimitFor(RateLimitPolicy.WriteTier, options));
        Assert.Equal(60, RateLimitPolicy.LimitFor(RateLimitPolicy.SearchTier, options));
    }

    [Fact]
    public void Rate_limit_burst_is_throttled_per_tier_with_independent_budgets()
    {
        var store = new MemoryRateLimitStore();
        var window = TimeSpan.FromMinutes(1);
        var now = DateTimeOffset.UtcNow;

        Assert.True(store.Allow("export", "client", 1, window, now));
        Assert.False(store.Allow("export", "client", 1, window, now));
        Assert.Equal(0, store.Remaining("export", "client", 1, window, now));

        // Other tiers and clients keep their own budgets.
        Assert.True(store.Allow("search", "client", 1, window, now));
        Assert.True(store.Allow("export", "other", 1, window, now));

        // The window sliding forward restores the budget.
        Assert.True(store.Allow("export", "client", 1, window, now + TimeSpan.FromMinutes(2)));
    }

    [Fact]
    public void Rate_limit_postgres_mode_without_connection_fails_closed()
    {
        var options = new RateLimitOptions { Mode = "Postgres" };

        Assert.NotEmpty(options.MissingKeys(connectionString: null));
        Assert.Empty(options.MissingKeys(connectionString: "Host=db;Database=atlas"));
        Assert.Empty(new RateLimitOptions().MissingKeys(connectionString: null));
    }

    [Fact]
    public async Task Rate_limit_burst_over_http_returns_retry_after_and_no_partial_export()
    {
        using var client = new BurstHostFactory().CreateClient();

        var first = await client.GetAsync("/api/v1/meta");
        var second = await client.GetAsync("/api/v1/meta");
        var third = await client.GetAsync("/api/v1/meta");

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal((HttpStatusCode)429, third.StatusCode);
        Assert.True(third.Headers.Contains("Retry-After"));

        _ = await client.GetAsync("/api/v1/export");
        var deniedExport = await client.GetAsync("/api/v1/export");
        Assert.Equal((HttpStatusCode)429, deniedExport.StatusCode);
        Assert.Empty(await deniedExport.Content.ReadAsByteArrayAsync());
    }

    // ---- Open governance files ----

    [Fact]
    public void Governance_files_exist_and_readme_links_them_with_data_license()
    {
        var root = FindRepositoryRoot();
        foreach (var file in new[] { "LICENSE", "CONTRIBUTING.md", "CODE_OF_CONDUCT.md", "SECURITY.md" })
        {
            Assert.True(File.Exists(Path.Combine(root, file)), $"Missing {file}");
        }

        var readme = File.ReadAllText(Path.Combine(root, "README.md"));
        foreach (var file in new[] { "LICENSE", "CONTRIBUTING.md", "CODE_OF_CONDUCT.md", "SECURITY.md" })
        {
            Assert.Contains(file, readme, StringComparison.Ordinal);
        }

        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "data", "seed", "v2", "manifest.json")));
        Assert.Equal("CC-BY-4.0", manifest.RootElement.GetProperty("metadata").GetProperty("license").GetString());
    }

    private sealed class BurstHostFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("ConnectionStrings:Dharmatlas", "Host=127.0.0.1;Port=1;Database=test;Username=test;Password=test");
            builder.UseSetting("Dharmatlas:RateLimit:SearchPerMinute", "2");
            builder.UseSetting("Dharmatlas:RateLimit:ExportPerMinute", "1");
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        for (var depth = 0; depth < 10 && directory is not null; depth++, directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "LICENSE"))
                && File.Exists(Path.Combine(directory.FullName, "Dharmatlas.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("Repository root with LICENSE and Dharmatlas.slnx was not found.");
    }

    private static OidcReferenceHandler CreateHandler(RSA rsa, string jwks, bool AllowUnsigned)
    {
        var options = new OidcOptions
        {
            Mode = OidcOptions.OidcMode,
            Issuer = Issuer,
            Audience = Audience,
            JwksUri = "https://issuer.example/.well-known/jwks.json",
            AllowUnsignedTokens = AllowUnsigned,
        };
        var provider = new JwksKeyProvider(_ => Task.FromResult(jwks), TimeSpan.FromHours(1));
        return new OidcReferenceHandler(
            new StaticOptionsMonitor(),
            NullLoggerFactory.Instance,
            UrlEncoder.Default,
            options,
            provider);
    }

    private static async Task<AuthenticateResult> AuthenticateAsync(OidcReferenceHandler handler, string token)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = "Bearer " + token;
        await handler.InitializeAsync(new AuthenticationScheme("Dharmatlas", null, typeof(OidcReferenceHandler)), context);
        return await handler.AuthenticateAsync();
    }

    private static string Mint(RSA rsa, string kid, string Subject, string AudienceValue, long Expires, string[] Roles, string? IssuerValue = null)
    {
        var header = Base64UrlEncode(JsonSerializer.Serialize(new { alg = "RS256", kid, typ = "JWT" }));
        var payload = Base64UrlEncode(JsonSerializer.Serialize(new
        {
            iss = IssuerValue ?? Issuer,
            aud = new[] { AudienceValue, "other" },
            exp = Expires,
            sub = Subject,
            roles = Roles,
        }));
        var signed = Encoding.ASCII.GetBytes(header + "." + payload);
        var signature = Base64UrlEncode(rsa.SignData(signed, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
        return header + "." + payload + "." + signature;
    }

    private static string UnsignedToken(string Subject, string AudienceValue, long Expires)
    {
        var header = Base64UrlEncode(JsonSerializer.Serialize(new { alg = "none", typ = "JWT" }));
        var payload = Base64UrlEncode(JsonSerializer.Serialize(new
        {
            iss = Issuer,
            aud = AudienceValue,
            exp = Expires,
            sub = Subject,
            roles = new[] { "contributor" },
        }));
        return header + "." + payload + ".";
    }

    private static string JwksDocument(RSA rsa, string kid)
    {
        var parameters = rsa.ExportParameters(false);
        return JsonSerializer.Serialize(new
        {
            keys = new[]
            {
                new { kty = "RSA", kid, n = Base64UrlEncode(parameters.Modulus!), e = Base64UrlEncode(parameters.Exponent!) },
            },
        });
    }

    private static long Hours(double hours) =>
        new DateTimeOffset(DateTime.UtcNow.AddHours(hours)).ToUnixTimeSeconds();

    private static string Base64UrlEncode(byte[] value) =>
        Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static string Base64UrlEncode(string value) => Base64UrlEncode(Encoding.UTF8.GetBytes(value));

    private static async Task<(HttpContext Context, bool NextCalled)> InvokeHeadersAsync(string scheme, string? origin, string? allowedOrigins)
    {
        var settings = new Dictionary<string, string?>();
        if (allowedOrigins is not null)
        {
            settings["Dharmatlas:Cors:AllowedOrigins"] = allowedOrigins;
        }

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var nextCalled = false;
        var middleware = new SecurityHeadersMiddleware(
            _ => { nextCalled = true; return Task.CompletedTask; }, configuration);
        var context = new DefaultHttpContext();
        context.Request.Scheme = scheme;
        if (origin is not null)
        {
            context.Request.Headers.Origin = origin;
        }

        await middleware.InvokeAsync(context);
        return (context, nextCalled);
    }

    private sealed class StaticOptionsMonitor : IOptionsMonitor<AuthenticationSchemeOptions>
    {
        public AuthenticationSchemeOptions CurrentValue => Get(null);
        public AuthenticationSchemeOptions Get(string? name) => new();
        public IDisposable OnChange(Action<AuthenticationSchemeOptions, string?> listener) => NullDisposable.Instance;
    }

    private sealed class NullDisposable : IDisposable
    {
        public static readonly NullDisposable Instance = new();
        public void Dispose() { }
    }
}
