using System.Buffers.Text;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using NSec.Cryptography;
using SharedKernel;

namespace Generation.Fal;

/// <summary>
/// Verifies the ED25519 signature Fal attaches to every webhook. The header
/// values and body are untrusted input on their own; a passing verify is the
/// proof Fal signed exactly these bytes, because only Fal holds the private key.
/// See https://fal.ai/docs/model-endpoints/webhooks.
/// </summary>
internal sealed class FalWebhookVerifier(IHttpClientFactory httpClientFactory, IConfiguration config)
{
    private const long TimestampLeewaySeconds = 300;
    private static readonly TimeSpan KeyCacheTtl = TimeSpan.FromHours(1);

    // Defaults to real Fal; point at a local fake-Fal server for E2E testing.
    private readonly string _jwksUrl =
        config["FAL_JWKS_URL"] ?? "https://rest.fal.ai/.well-known/jwks.json";
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private volatile PublicKey[] _keys = [];
    private DateTimeOffset _keysExpireAt = DateTimeOffset.MinValue;

    public async Task VerifyAsync(WebhookRequest request)
    {
        var requestId = Require(request, "X-Fal-Webhook-Request-Id");
        var userId = Require(request, "X-Fal-Webhook-User-Id");
        var timestamp = Require(request, "X-Fal-Webhook-Timestamp");
        var signatureHex = Require(request, "X-Fal-Webhook-Signature");

        // Reject stale (and so replay-able) deliveries. The timestamp is itself
        // signed, so it can't be forged forward — this just bounds the window.
        if (!long.TryParse(timestamp, out var sentAt) ||
            Math.Abs(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - sentAt) > TimestampLeewaySeconds)
            throw new WebhookVerificationException("Fal webhook timestamp missing or outside the allowed window.");

        // Rebuild the exact message Fal signed: the header values as received
        // plus a hash of the body as received, joined by newlines.
        var bodyHashHex = Convert.ToHexStringLower(SHA256.HashData(request.Body));
        var message = Encoding.UTF8.GetBytes($"{requestId}\n{userId}\n{timestamp}\n{bodyHashHex}");

        byte[] signature;
        try
        {
            signature = Convert.FromHexString(signatureHex);
        }
        catch (FormatException)
        {
            throw new WebhookVerificationException("Fal webhook signature is not valid hex.");
        }

        // Try cached keys first; on a miss, refresh once in case Fal rotated.
        if (await AnyKeyVerifiesAsync(message, signature, forceRefresh: false))
            return;
        if (await AnyKeyVerifiesAsync(message, signature, forceRefresh: true))
            return;

        throw new WebhookVerificationException("Fal webhook signature did not verify against any known key.");
    }

    private static string Require(WebhookRequest request, string header) =>
        request.Headers.TryGetValue(header, out var value) && !string.IsNullOrEmpty(value)
            ? value
            : throw new WebhookVerificationException($"Fal webhook is missing the {header} header.");

    private async Task<bool> AnyKeyVerifiesAsync(byte[] message, byte[] signature, bool forceRefresh)
    {
        var keys = await GetKeysAsync(forceRefresh);
        foreach (var key in keys)
        {
            if (SignatureAlgorithm.Ed25519.Verify(key, message, signature))
                return true;
        }
        return false;
    }

    private async Task<PublicKey[]> GetKeysAsync(bool forceRefresh)
    {
        if (!forceRefresh && _keys.Length > 0 && DateTimeOffset.UtcNow < _keysExpireAt)
            return _keys;

        await _refreshLock.WaitAsync();
        try
        {
            // Another caller may have refreshed while we waited on the lock.
            if (!forceRefresh && _keys.Length > 0 && DateTimeOffset.UtcNow < _keysExpireAt)
                return _keys;

            using var client = httpClientFactory.CreateClient();
            var jwks = await client.GetFromJsonAsync<Jwks>(_jwksUrl)
                ?? throw new WebhookVerificationException("Fal JWKS endpoint returned no keys.");

            _keys = jwks.Keys
                .Where(k => k.X is not null)
                .Select(k => PublicKey.Import(
                    SignatureAlgorithm.Ed25519,
                    Base64Url.DecodeFromChars(k.X),
                    KeyBlobFormat.RawPublicKey))
                .ToArray();
            _keysExpireAt = DateTimeOffset.UtcNow + KeyCacheTtl;
            return _keys;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private sealed record Jwks([property: JsonPropertyName("keys")] IReadOnlyList<Jwk> Keys);
    private sealed record Jwk([property: JsonPropertyName("x")] string X);
}
