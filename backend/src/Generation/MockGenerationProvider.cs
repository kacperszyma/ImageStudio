namespace Generation;
using Generation.Contracts;

internal sealed class MockGenerationProvider : IGenerationProvider
{
    public async Task<string> SubmitJobAsync(string modelSlug, string prompt)
    {
        await Task.Delay(5000);
        return "mock-id";
    }

    public GenerationCallback ParseCallback(byte[] body) =>
        throw new NotSupportedException("The mock provider completes synchronously and has no webhook callback.");
}
