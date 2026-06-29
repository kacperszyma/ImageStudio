using Generation.Contracts;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Generation;

internal sealed class GenerationService(IGenerationProvider provider, GenerationDbContext db)
    : IGenerationService, IGenerationQueryService, IGenerationWebhook
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
            CreditCost = ImageModel.FromString(modelSlug).CreditCost,
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

    public async Task<GenerationSummary?> GetDetailsByRequestIdAsync(string falRequestId)
    {
        var g = await db.Generations.FirstOrDefaultAsync(g => g.FalRequestId == falRequestId);
        return g is null ? null : new GenerationSummary(g.ImageModel, g.Prompt, g.ResultUrl, g.CreditCost);
    }

    public async Task<IReadOnlyDictionary<string, GenerationSummary>> GetSummariesByRequestIdsAsync(IEnumerable<string> falRequestIds)
    {
        var ids = falRequestIds.ToList();
        return await db.Generations
            .Where(g => g.FalRequestId != null && ids.Contains(g.FalRequestId!))
            .ToDictionaryAsync(g => g.FalRequestId!, g => new GenerationSummary(g.ImageModel, g.Prompt, g.ResultUrl, g.CreditCost));
    }
}
