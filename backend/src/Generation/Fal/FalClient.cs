using Microsoft.Extensions.Configuration;

namespace Generation.Fal;

internal sealed class FalClient
{
    private readonly HttpClient _httpClient;

    public FalClient(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://queue.fal.run/fal-ai/");
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Key {config["FAL_API_KEY"]}");
    }
        

}
