using Generation.Contracts;
using SharedKernel;
namespace Generation;


internal interface IGenerationProvider
{
    Task<string> SubmitJobAsync(string modelSlug, string prompt);
    Task<GenerationCallback> ParseCallbackAsync(WebhookRequest request);

    /// <summary>Fetches the generated image's bytes from the provider so they can be
    /// re-hosted in our own storage.</summary>
    Task<Stream> DownloadImageAsync(string imageUrl);
}
