using Generation.Contracts;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Generation;

internal sealed class GenerationService(IGenerationProvider provider, GenerationDbContext db) : IGenerationService
{
    public List<ModelDto> GetModels() =>
        ImageModel.All.Select(m => new ModelDto(m.Slug, m.CreditCost)).ToList();

    public long GetCost(string modelSlug) =>
        ImageModel.FromString(modelSlug).CreditCost;

    public async Task<string> SubmitAsync(Guid userId, string modelSlug, string prompt)
    {
        var requestId = await provider.SubmitJobAsync(modelSlug, prompt);

        db.Generations.Add(new GenerationInstance
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ImageModel = modelSlug,
            Prompt = prompt,
            FalRequestId = requestId,
            // ResultUrl stays null until the provider's callback arrives.
        });
        await db.SaveChangesAsync();

        return requestId;
    }

    public Task<GenerationCallback> ParseCallbackAsync(WebhookRequest request) =>
        provider.ParseCallbackAsync(request);

    public async Task CompleteGenerationAsync(string requestId, string imageUrl)
    {
        var generation = await db.Generations.FirstOrDefaultAsync(g => g.FalRequestId == requestId);
        if (generation is null || generation.ResultUrl is not null)
            return; // unknown or already recorded — idempotent against webhook retries

        generation.ResultUrl = imageUrl;
        await db.SaveChangesAsync();
    }

    public async Task<IReadOnlyCollection<GenerationDetails>> GetGenerationHistory(Guid userId)
    {
        return await db.Generations
            .Where(g => g.UserId == userId)
            .Select(g => new GenerationDetails(g.ImageModel, g.Prompt, g.ResultUrl))
            .ToListAsync();
    }
}
