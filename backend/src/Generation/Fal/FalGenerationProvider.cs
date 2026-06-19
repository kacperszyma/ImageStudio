namespace Generation.Fal;

internal sealed class FalGenerationProvider : IGenerationProvider
{
    public Task<string> SubmitJobAsync(Guid jobId, string modelSlug, string prompt) => throw new NotImplementedException();
}
