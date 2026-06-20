using Generation.Contracts;

namespace Generation;

internal sealed class GenerationService(IGenerationProvider provider, GenerationDbContext db) : IGenerationService
{
    public List<ModelDto> GetModels() =>
        ImageModel.All.Select(m => new ModelDto(m.Slug, m.CreditCost)).ToList();

    public long GetCost(string modelSlug) =>
        ImageModel.FromString(modelSlug).CreditCost;

    public async Task<GenerationJobDto> RunAsync(Guid jobId, Guid userId, string modelSlug, string prompt)
    {
        var imageUrl = await provider.SubmitJobAsync(jobId, modelSlug, prompt);

        db.Generations.Add(new GenerationInstance
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ImageModel = modelSlug,
            Prompt = prompt,
            ResultUrl = imageUrl
        });
        var result = await db.SaveChangesAsync();

        return new GenerationJobDto(jobId, imageUrl);
    }

    public async Task<IReadOnlyCollection<GenerationDetails>> GetGenerationHistory(Guid userId)
    {
        return db.Generations.Where(g => g.UserId == userId).Select(g => new GenerationDetails(g.ImageModel, g.Prompt, g.ResultUrl)).ToList();
    }
}
