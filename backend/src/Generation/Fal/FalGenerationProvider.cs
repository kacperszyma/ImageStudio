using System.Text.Json;
using Generation.Contracts;

namespace Generation.Fal;

internal sealed class FalGenerationProvider(FalClient falClient) : IGenerationProvider
{
    public async Task<string> SubmitJobAsync(string modelSlug, string prompt)
    {
        ImageModel model = ImageModel.FromString(modelSlug);
        EnqueueResponse enqueued = await falClient.EnqueueGenerationAsync(model, prompt);

        return enqueued.RequestId;
    }

    public GenerationCallback ParseCallback(byte[] body)
    {
        // TODO: verify X-Fal-Webhook-Signature over `body` before trusting it.
        var payload = JsonSerializer.Deserialize<FalWebhookPayload>(body)
            ?? throw new InvalidOperationException("Empty Fal webhook body.");

        var success = payload.Status == "OK";
        var imageUrl = success ? payload.Payload?.Images.FirstOrDefault()?.Url : null;

        return new GenerationCallback(payload.RequestId, success && imageUrl is not null, imageUrl);
    }
}
