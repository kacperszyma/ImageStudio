namespace Generation.Fal;

internal sealed class FalGenerationProvider : IGenerationProvider
{
    public async Task<string> SubmitJobAsync(Guid jobId, string modelSlug, string prompt)
    {

        return "submited";
    }
}
