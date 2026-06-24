using Generation.Contracts;
using SharedKernel;
namespace Generation;


internal interface IGenerationProvider
{
    Task<string> SubmitJobAsync(string modelSlug, string prompt);
    Task<GenerationCallback> ParseCallbackAsync(WebhookRequest request);
}
