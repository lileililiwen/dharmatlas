using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Dharmatlas.Host.Auth;

/// <summary>
/// Local-development loopback identity. NON-PRODUCTION ONLY: it maps fixed
/// loopback subjects to roles without cryptographic verification so frontend
/// and contribution flows can be exercised locally. It is registered only when
/// <c>DHARMATLAS_AUTH_MODE=dev-loopback</c>; any other mode (including the
/// default <c>unconfigured</c>) never trusts request headers and denies
/// anonymous writes by failing closed.
/// </summary>
public sealed class DevLoopbackHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string DevSubjectHeader = "X-Dev-Subject";
    public const string DevRoleHeader = "X-Dev-Role";

    public DevLoopbackHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var subject = Request.Headers[DevSubjectHeader].ToString().Trim();
        if (string.IsNullOrWhiteSpace(subject))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, subject),
            new("sub", subject),
        };
        foreach (var role in Request.Headers[DevRoleHeader].ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var normalized = role.ToLowerInvariant();
            if (normalized is "contributor" or "reviewer")
            {
                claims.Add(new Claim(ClaimTypes.Role, normalized));
            }
        }

        var identity = new ClaimsIdentity(claims, "DevLoopback");
        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
    }
}
