using System.Text.Json;
using Generation.Contracts;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace Generation.Fal;

internal sealed class FalGenerationProvider(
    FalClient falClient,
    FalWebhookVerifier verifier,
    FalMetrics metrics,
    ILogger<FalGenerationProvider> logger) : IGenerationProvider
{
    public async Task<string> SubmitJobAsync(string modelSlug, string prompt)
    {
        ImageModel model = ImageModel.FromString(modelSlug);
        EnqueueResponse enqueued = await falClient.EnqueueGenerationAsync(model, prompt);

        return enqueued.RequestId;
    }

    public async Task<GenerationCallback> ParseCallbackAsync(WebhookRequest request)
    {
        // Verify before decode so an unverified body can never become a callback.
        try
        {
            await verifier.VerifyAsync(request);
        }
        catch (WebhookVerificationException)
        {
            metrics.WebhookVerificationFailed();
            logger.LogWarning("Fal webhook signature verification failed.");
            throw;
        }
        return Decode(request.Body);
    }

    public Task<Stream> DownloadImageAsync(string imageUrl) =>
        falClient.DownloadImageAsync(imageUrl);

    private static GenerationCallback Decode(byte[] body)
    {
        var payload = JsonSerializer.Deserialize<FalWebhookPayload>(body)
            ?? throw new InvalidOperationException("Empty Fal webhook body.");

        var success = payload.Status == "OK";
        var imageUrl = success ? payload.Payload?.Images.FirstOrDefault()?.Url : null;

        return new GenerationCallback(payload.RequestId, success && imageUrl is not null, imageUrl);
    }
}
