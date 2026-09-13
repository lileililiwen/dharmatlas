using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Dharmatlas.Host.Auth;

/// <summary>
/// Reference OIDC JWT-bearer handler. Validates <c>iss</c>, <c>aud</c>, and
/// <c>exp</c>, verifies the RS256 signature against the provider JWKS (with
/// rotation via <see cref="JwksKeyProvider"/>), and maps the stable OIDC
/// <c>sub</c> to <see cref="ClaimTypes.NameIdentifier"/> plus <c>sub</c> so
/// the existing contributor mapping resolves without trusting client headers.
/// Unsigned tokens are rejected unless explicitly allowed for local
/// development. Deployments needing features beyond this reference (refresh
/// tokens, multi-issuer, token caching) should substitute a hardened OIDC
/// library and keep this handler's fail-closed contract.
/// </summary>
public sealed class OidcReferenceHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly OidcOptions _options;
    private readonly JwksKeyProvider _keys;
    private readonly TimeProvider _clock;

    public OidcReferenceHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        OidcOptions oidcOptions,
        JwksKeyProvider keys,
        TimeProvider? clock = null) : base(options, logger, encoder)
    {
        _options = oidcOptions;
        _keys = keys;
        _clock = clock ?? TimeProvider.System;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var header = Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(header))
        {
            return AuthenticateResult.NoResult();
        }

        if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.Fail("Unsupported authorization scheme.");
        }

        var outcome = await ValidateAsync(header["Bearer ".Length..].Trim(), Context.RequestAborted);
        if (!outcome.Ok)
        {
            return AuthenticateResult.Fail(outcome.Failure!);
        }

        var identity = new ClaimsIdentity(outcome.Claims!, "OidcReference");
        return AuthenticateResult.Success(new AuthenticationTicket(
            new ClaimsPrincipal(identity), Scheme.Name));
    }

    internal async Task<(bool Ok, string? Failure, List<Claim>? Claims)> ValidateAsync(
        string token, CancellationToken cancellationToken)
    {
        var parts = token.Split('.');
        if (parts.Length != 3)
        {
            return (false, "Malformed token.", null);
        }

        JsonDocument headerJson;
        JsonDocument payloadJson;
        try
        {
            headerJson = JsonDocument.Parse(Encoding.UTF8.GetString(Base64Url.Decode(parts[0])));
            payloadJson = JsonDocument.Parse(Encoding.UTF8.GetString(Base64Url.Decode(parts[1])));
        }
        catch (FormatException)
        {
            return (false, "Malformed token encoding.", null);
        }
        catch (JsonException)
        {
            return (false, "Malformed token claims.", null);
        }

        using (headerJson)
        using (payloadJson)
        {
            var header = headerJson.RootElement;
            var payload = payloadJson.RootElement;
            var alg = header.TryGetProperty("alg", out var algElement) ? algElement.GetString() : null;

            if (string.Equals(alg, "none", StringComparison.OrdinalIgnoreCase))
            {
                if (!_options.AllowUnsignedTokens)
                {
                    return (false, "Unsigned tokens are not accepted.", null);
                }
            }
            else if (string.Equals(alg, "RS256", StringComparison.Ordinal))
            {
                if (!header.TryGetProperty("kid", out var kidElement) || string.IsNullOrEmpty(kidElement.GetString()))
                {
                    return (false, "Token key id is missing.", null);
                }

                var rsa = await _keys.GetKeyAsync(kidElement.GetString()!, cancellationToken);
                if (rsa is null)
                {
                    return (false, "Token key is unknown.", null);
                }

                byte[] signature;
                try
                {
                    signature = Base64Url.Decode(parts[2]);
                }
                catch (FormatException)
                {
                    return (false, "Malformed token signature.", null);
                }

                var signed = Encoding.ASCII.GetBytes(parts[0] + "." + parts[1]);
                if (!rsa.VerifyData(signed, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1))
                {
                    return (false, "Token signature is invalid.", null);
                }
            }
            else
            {
                return (false, "Unsupported token algorithm.", null);
            }

            if (!string.IsNullOrWhiteSpace(_options.Issuer)
                && (!payload.TryGetProperty("iss", out var iss) || iss.GetString() != _options.Issuer))
            {
                return (false, "Token issuer is not trusted.", null);
            }

            if (!string.IsNullOrWhiteSpace(_options.Audience) && !AudienceMatches(payload, _options.Audience!))
            {
                return (false, "Token audience is not trusted.", null);
            }

            if (!payload.TryGetProperty("exp", out var exp) || exp.ValueKind != JsonValueKind.Number
                || !exp.TryGetInt64(out var expSeconds))
            {
                return (false, "Token expiry is missing.", null);
            }

            var now = _clock.GetUtcNow().ToUnixTimeSeconds();
            if (expSeconds <= now - 60)
            {
                return (false, "Token has expired.", null);
            }

            if (!payload.TryGetProperty("sub", out var sub) || string.IsNullOrWhiteSpace(sub.GetString()))
            {
                return (false, "Token subject is missing.", null);
            }

            var subject = sub.GetString()!;
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, subject),
                new("sub", subject),
            };
            foreach (var role in ReadRoles(payload, _options.RoleClaim))
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            return (true, null, claims);
        }
    }

    private static bool AudienceMatches(JsonElement payload, string audience)
    {
        if (!payload.TryGetProperty("aud", out var aud))
        {
            return false;
        }

        return aud.ValueKind switch
        {
            JsonValueKind.String => string.Equals(aud.GetString(), audience, StringComparison.Ordinal),
            JsonValueKind.Array => aud.EnumerateArray().Any(
                entry => entry.ValueKind == JsonValueKind.String
                    && string.Equals(entry.GetString(), audience, StringComparison.Ordinal)),
            _ => false,
        };
    }

    internal static IEnumerable<string> ReadRoles(JsonElement payload, string roleClaim)
    {
        if (!payload.TryGetProperty(roleClaim, out var roles))
        {
            yield break;
        }

        if (roles.ValueKind == JsonValueKind.String)
        {
            var single = NormalizeRole(roles.GetString());
            if (single is not null) yield return single;
            yield break;
        }

        if (roles.ValueKind != JsonValueKind.Array)
        {
            yield break;
        }

        foreach (var entry in roles.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.String) continue;
            var role = NormalizeRole(entry.GetString());
            if (role is not null) yield return role;
        }
    }

    private static string? NormalizeRole(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var normalized = raw.Trim().ToLowerInvariant();
        return normalized is "contributor" or "reviewer" ? normalized : null;
    }
}
