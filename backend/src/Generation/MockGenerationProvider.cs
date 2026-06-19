namespace Generation;

internal sealed class MockGenerationProvider : IGenerationProvider
{
    public async Task<string> SubmitJobAsync(Guid jobId, string modelSlug, string prompt)
    {
        await Task.Delay(10000);
        return "https://v3b.fal.media/files/b/0a9e6a3a/_cHxH1AIyaoNCN8vT_HYG.jpg";
    }
}
