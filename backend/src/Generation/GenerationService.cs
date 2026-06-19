using Generation.Contracts;

namespace Generation;

public class GenerationService : IGenerationService
{
    public List<ModelDto> GetModels() =>
        ImageModel.All.Select(m => new ModelDto(m.Slug, m.CreditCost)).ToList();

    public long GetCost(string modelSlug) =>
        ImageModel.FromString(modelSlug).CreditCost;

    public Task<GenerationJobDto> RunAsync(Guid jobId, string modelSlug, string prompt)
    {
        // TODO: call fal.ai
        throw new NotImplementedException();
    }
}
