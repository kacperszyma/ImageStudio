using System.Buffers.Text;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Generation.Fal;
using Microsoft.Extensions.Configuration;
using NSec.Cryptography;
using SharedKernel;
using NSecKey = NSec.Cryptography.Key;

namespace Generation.UnitTests;

public class FalWebhookVerifierTests
{
    private const string JwksUrl = "https://fake/.well-known/jwks.json";

    [Fact]
    public async Task Accepts_a_correctly_signed_webhook()
    {
        var signer = new Signer();
        var verifier = signer.Verifier();
        var request = signer.Sign(Body);

        var act = () => verifier.VerifyAsync(request);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Rejects_when_the_body_was_tampered_with()
    {
        var signer = new Signer();
        var verifier = signer.Verifier();
        var request = signer.Sign(Body);

        // Same headers/signature, but a body the signature never covered.
        var tampered = new WebhookRequest(
            Encoding.UTF8.GetBytes("""{"status":"OK","evil":true}"""), request.Headers);

        await verifier.Invoking(v => v.VerifyAsync(tampered))
            .Should().ThrowAsync<WebhookVerificationException>();
    }

    [Fact]
    public async Task Rejects_a_signature_from_a_different_key()
    {
        var verifier = new Signer().Verifier();      // serves key A
        var request = new Signer().Sign(Body);       // signed with key B

        await verifier.Invoking(v => v.VerifyAsync(request))
            .Should().ThrowAsync<WebhookVerificationException>();
    }

    [Fact]
    public async Task Rejects_a_stale_timestamp()
    {
        var signer = new Signer();
        var verifier = signer.Verifier();
        var request = signer.Sign(Body, DateTimeOffset.UtcNow.AddMinutes(-10));

        await verifier.Invoking(v => v.VerifyAsync(request))
            .Should().ThrowAsync<WebhookVerificationException>();
    }

    [Fact]
    public async Task Rejects_a_missing_header()
    {
        var signer = new Signer();
        var verifier = signer.Verifier();
        var request = signer.Sign(Body);
        var headers = new Dictionary<string, string>(request.Headers, StringComparer.OrdinalIgnoreCase);
        headers.Remove("X-Fal-Webhook-Signature");

        await verifier.Invoking(v => v.VerifyAsync(new WebhookRequest(request.Body, headers)))
            .Should().ThrowAsync<WebhookVerificationException>();
    }

    [Fact]
    public async Task Rejects_a_non_hex_signature()
    {
        var signer = new Signer();
        var verifier = signer.Verifier();
        var request = signer.Sign(Body);
        var headers = new Dictionary<string, string>(request.Headers, StringComparer.OrdinalIgnoreCase)
        {
            ["X-Fal-Webhook-Signature"] = "not-hex",
        };

        await verifier.Invoking(v => v.VerifyAsync(new WebhookRequest(request.Body, headers)))
            .Should().ThrowAsync<WebhookVerificationException>();
    }

    private static readonly byte[] Body = Encoding.UTF8.GetBytes(
        """{"request_id":"abc","status":"OK","payload":{"images":[{"url":"http://x/y.png"}]}}""");

    /// Mints an Ed25519 key, signs requests exactly as fake-fal/real Fal do,
    /// and builds a verifier whose JWKS endpoint serves this key's public half.
    private sealed class Signer
    {
        private readonly NSecKey _key = NSecKey.Create(SignatureAlgorithm.Ed25519);
        private const string UserId = "local-user";

        public WebhookRequest Sign(byte[] body, DateTimeOffset? at = null)
        {
            var requestId = "abc";
            var timestamp = (at ?? DateTimeOffset.UtcNow).ToUnixTimeSeconds().ToString();
            var bodyHashHex = Convert.ToHexStringLower(SHA256.HashData(body));
            var message = Encoding.UTF8.GetBytes($"{requestId}\n{UserId}\n{timestamp}\n{bodyHashHex}");
            var signatureHex = Convert.ToHexString(SignatureAlgorithm.Ed25519.Sign(_key, message));

            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["X-Fal-Webhook-Request-Id"] = requestId,
                ["X-Fal-Webhook-User-Id"] = UserId,
                ["X-Fal-Webhook-Timestamp"] = timestamp,
                ["X-Fal-Webhook-Signature"] = signatureHex,
            };
            return new WebhookRequest(body, headers);
        }

        public FalWebhookVerifier Verifier()
        {
            var publicRaw = _key.PublicKey.Export(KeyBlobFormat.RawPublicKey);
            var jwks = $$"""{"keys":[{"kty":"OKP","crv":"Ed25519","x":"{{Base64Url.EncodeToString(publicRaw)}}"}]}""";

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["FAL_JWKS_URL"] = JwksUrl })
                .Build();
            return new FalWebhookVerifier(new StubHttpClientFactory(jwks), config);
        }
    }

    private sealed class StubHttpClientFactory(string jwksJson) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(new StubHandler(jwksJson));
    }

    private sealed class StubHandler(string jwksJson) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jwksJson, Encoding.UTF8, "application/json"),
            });
    }
}
