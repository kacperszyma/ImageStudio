namespace Generation;
using Generation.Contracts;
using SharedKernel;

internal sealed class MockGenerationProvider : IGenerationProvider
{
    public async Task<string> SubmitJobAsync(string modelSlug, string prompt)
    {
        await Task.Delay(5000);
        return "mock-id";
    }

    public Task<GenerationCallback> ParseCallbackAsync(WebhookRequest request) =>
        throw new NotSupportedException("The mock provider completes synchronously and has no webhook callback.");
}
