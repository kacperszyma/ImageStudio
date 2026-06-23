using System.Text.Json.Serialization;

namespace Generation.Fal;

/// <summary>
/// Body Fal POSTs to <c>fal_webhook</c> when a queued request finishes.
/// On success <see cref="Status"/> is "OK" and <see cref="Payload"/> holds the
/// model output; on failure it is "ERROR" with <see cref="Error"/> set.
/// </summary>
internal sealed record FalWebhookPayload(
    [property: JsonPropertyName("request_id")] string RequestId,
    [property: JsonPropertyName("gateway_request_id")] string GatewayRequestId,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("payload")] FalResult? Payload,
    [property: JsonPropertyName("error")] string? Error);

// Only Url is depended on. Everything else varies by model (some omit
// width/height/seed/nsfw entirely), so keep it all optional to stay robust.
internal sealed record FalResult(
    [property: JsonPropertyName("images")] IReadOnlyList<FalImage> Images,
    [property: JsonPropertyName("prompt")] string? Prompt,
    [property: JsonPropertyName("seed")] long? Seed,
    [property: JsonPropertyName("has_nsfw_concepts")] IReadOnlyList<bool>? HasNsfwConcepts);

internal sealed record FalImage(
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("width")] int? Width,
    [property: JsonPropertyName("height")] int? Height,
    [property: JsonPropertyName("content_type")] string? ContentType);
