using Generation.Contracts;

namespace Generation;

internal sealed class GenerationService(IGenerationProvider provider) : IGenerationService
{
    public List<ModelDto> GetModels() =>
        ImageModel.All.Select(m => new ModelDto(m.Slug, m.CreditCost)).ToList();

    public long GetCost(string modelSlug) =>
        ImageModel.FromString(modelSlug).CreditCost;

    public async Task<GenerationJobDto> RunAsync(Guid jobId, string modelSlug, string prompt)
    {
        var imageUrl = await provider.SubmitJobAsync(jobId, modelSlug, prompt);
        return new GenerationJobDto(jobId, imageUrl);
    }
}
