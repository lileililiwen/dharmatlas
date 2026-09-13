using System.Security.Cryptography;
using System.Text.Json;

namespace Dharmatlas.Host.Auth;

/// <summary>
/// Fetches and caches a JWKS document with periodic rotation. Keys are looked
/// up by <c>kid</c>; the cached document is refreshed after
/// <see cref="OidcOptions.JwksRefreshInterval"/> or when a <c>kid</c> is
/// unknown, so rotated provider keys are picked up without a restart.
/// Only RSA (<c>kty=RSA</c>) keys are supported by this reference.
/// </summary>
public sealed class JwksKeyProvider
{
    private readonly Func<CancellationToken, Task<string>> _fetch;
    private readonly TimeSpan _refreshInterval;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Dictionary<string, RSA> _keys = new(StringComparer.Ordinal);
    private DateTimeOffset _fetchedAt = DateTimeOffset.MinValue;
    private bool _fetched;

    public JwksKeyProvider(Func<CancellationToken, Task<string>> fetch, TimeSpan refreshInterval)
    {
        _fetch = fetch;
        _refreshInterval = refreshInterval;
    }

    public static JwksKeyProvider FromHttp(HttpClient http, OidcOptions options)
    {
        ArgumentNullException.ThrowIfNull(options.JwksUri);
        var uri = options.JwksUri;
        return new JwksKeyProvider(
            async cancellationToken => await http.GetStringAsync(uri, cancellationToken),
            options.JwksRefreshInterval);
    }

    /// <summary>Returns the RSA key for <paramref name="kid"/>, or null when unknown.</summary>
    public async Task<RSA?> GetKeyAsync(string kid, CancellationToken cancellationToken = default)
    {
        await EnsureFreshAsync(force: false, cancellationToken);
        if (_keys.TryGetValue(kid, out var cached))
        {
            return cached;
        }

        // Unknown kid: refresh once in case the provider rotated keys.
        await EnsureFreshAsync(force: true, cancellationToken);
        return _keys.TryGetValue(kid, out var rotated) ? rotated : null;
    }

    private async Task EnsureFreshAsync(bool force, CancellationToken cancellationToken)
    {
        if (!force && _fetched && DateTimeOffset.UtcNow - _fetchedAt < _refreshInterval)
        {
            return;
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!force && _fetched && DateTimeOffset.UtcNow - _fetchedAt < _refreshInterval)
            {
                return;
            }

            var document = await _fetch(cancellationToken);
            var parsed = Parse(document);
            _keys.Clear();
            foreach (var (kid, rsa) in parsed)
            {
                if (_keys.TryGetValue(kid, out var prior))
                {
                    prior.Dispose();
                }

                _keys[kid] = rsa;
            }

            _fetched = true;
            _fetchedAt = DateTimeOffset.UtcNow;
        }
        finally
        {
            _gate.Release();
        }
    }

    internal static Dictionary<string, RSA> Parse(string document)
    {
        var result = new Dictionary<string, RSA>(StringComparer.Ordinal);
        using var json = JsonDocument.Parse(document);
        if (!json.RootElement.TryGetProperty("keys", out var keys) || keys.ValueKind != JsonValueKind.Array)
        {
            return result;
        }

        foreach (var key in keys.EnumerateArray())
        {
            if (!key.TryGetProperty("kid", out var kidElement)
                || !key.TryGetProperty("kty", out var ktyElement)
                || ktyElement.GetString() != "RSA"
                || !key.TryGetProperty("n", out var nElement)
                || !key.TryGetProperty("e", out var eElement))
            {
                continue;
            }

            var kid = kidElement.GetString();
            if (string.IsNullOrEmpty(kid) || result.ContainsKey(kid))
            {
                continue;
            }

            try
            {
                var rsa = RSA.Create();
                rsa.ImportParameters(new RSAParameters
                {
                    Modulus = Base64Url.Decode(nElement.GetString()!),
                    Exponent = Base64Url.Decode(eElement.GetString()!),
                });
                result[kid] = rsa;
            }
            catch (CryptographicException)
            {
                // Skip malformed keys; other keys in the set remain usable.
            }
            catch (FormatException)
            {
                // Skip keys with invalid base64url material.
            }
        }

        return result;
    }
}
