using Generation.Contracts;
namespace Generation;


internal interface IGenerationProvider
{
    Task<string> SubmitJobAsync(string modelSlug, string prompt);
    GenerationCallback ParseCallback(byte[] body);
}
