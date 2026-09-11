using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Dharmatlas.Host;

/// <summary>
/// Safe default until a deployment supplies its provider-specific authentication
/// handler. It never trusts request headers or invents an identity; deployments
/// replace the scheme with their verified OIDC, SAML, or gateway integration.
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
