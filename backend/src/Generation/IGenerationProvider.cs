namespace Generation;

internal interface IGenerationProvider
{
    Task<string> SubmitJobAsync(Guid jobId, string modelSlug, string prompt);
}
