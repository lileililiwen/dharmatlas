using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Dharmatlas.Host;

/// <summary>
/// Fail-closed default when no <c>DHARMATLAS_AUTH_MODE</c> is configured. It
/// never trusts request headers or invents an identity, so anonymous writes
/// are denied without creating state. Production deployments set
/// <c>DHARMATLAS_AUTH_MODE=oidc</c> with the OIDC parameters to enable the
/// <see cref="Auth.OidcReferenceHandler"/> reference integration.
/// </summary>
public sealed class UnconfiguredAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public UnconfiguredAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync() =>
        Task.FromResult(AuthenticateResult.NoResult());
}
