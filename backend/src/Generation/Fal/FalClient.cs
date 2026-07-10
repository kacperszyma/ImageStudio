using System.Diagnostics;
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
    private readonly FalMetrics _metrics;

    public FalClient(HttpClient httpClient, IConfiguration config, FalMetrics metrics)
    {
        _httpClient = httpClient;
        // Defaults to real Fal; point at a local fake-Fal server for E2E testing.
        _httpClient.BaseAddress = new Uri(config["FAL_QUEUE_URL"] ?? "https://queue.fal.run/");
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Key {config["FAL_API_KEY"]}");
        _webhookUrl = config["FAL_WEBHOOK_URL"]
            ?? throw new InvalidOperationException("FAL_WEBHOOK_URL is not configured.");
        _metrics = metrics;
    }

    internal async Task<EnqueueResponse> EnqueueGenerationAsync(ImageModel model, string prompt)
    {
        var requestUri = $"{model.FalModelId}?fal_webhook={Uri.EscapeDataString(_webhookUrl)}";
        var start = Stopwatch.GetTimestamp();

        try
        {
            using HttpResponseMessage response =
                await _httpClient.PostAsJsonAsync(requestUri, new { prompt });

            response.EnsureSuccessStatusCode();

            var result = (await response.Content.ReadFromJsonAsync<EnqueueResponse>())
                ?? throw new InvalidOperationException("Fal returned an empty enqueue response.");
            _metrics.EnqueueSucceeded(Stopwatch.GetElapsedTime(start));
            return result;
        }
        catch
        {
            _metrics.EnqueueFailed(Stopwatch.GetElapsedTime(start));
            throw;
        }
    }

    /// <summary>Downloads a generated image from the URL Fal's webhook reported.
    /// Note: this goes out on the same client as the enqueue call, so Fal's API
    /// key rides along as a header — harmless against Fal's own domain, but worth
    /// knowing if that URL ever points somewhere else.</summary>
    internal Task<Stream> DownloadImageAsync(string imageUrl) =>
        _httpClient.GetStreamAsync(imageUrl);
}
