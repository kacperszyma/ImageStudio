using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;

namespace Generation.Fal;

internal sealed record EnqueueResponse(
    [property: JsonPropertyName("request_id")] string RequestId,
    [property: JsonPropertyName("gateway_request_id")] string GatewayRequestId);

internal sealed class FalClient
{
    private readonly HttpClient _httpClient;
    private readonly string _webhookUrl;

    public FalClient(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://queue.fal.run/");
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Key {config["FAL_API_KEY"]}");
        _webhookUrl = config["FAL_WEBHOOK_URL"]
            ?? throw new InvalidOperationException("FAL_WEBHOOK_URL is not configured.");
    }

    internal async Task<EnqueueResponse> EnqueueGenerationAsync(ImageModel model, string prompt)
    {
        var requestUri = $"{model.FalModelId}?fal_webhook={Uri.EscapeDataString(_webhookUrl)}";

        using HttpResponseMessage response =
            await _httpClient.PostAsJsonAsync(requestUri, new { prompt });

        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<EnqueueResponse>())
            ?? throw new InvalidOperationException("Fal returned an empty enqueue response.");
    }
}
