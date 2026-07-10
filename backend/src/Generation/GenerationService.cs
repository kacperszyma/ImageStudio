using Generation.Contracts;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Generation;

internal sealed class GenerationService(IGenerationProvider provider, ICloudBucket bucket, GenerationDbContext db)
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

    public async Task<string> CompleteGenerationAsync(string requestId, string imageUrl)
    {
        var generation = await db.Generations.FirstOrDefaultAsync(g => g.FalRequestId == requestId);
        if (generation is null)
            return imageUrl; // unknown — nothing to key a bucket object off, fall back to the provider's own URL

        if (generation.ResultUrl is not null)
            return await bucket.GetUrl(generation.ResultUrl); // already recorded — idempotent, just re-sign

        // Deterministic key: a retried call (partial failure — upload succeeded
        // but the SaveChangesAsync below didn't) just overwrites the same object
        // instead of leaking an orphaned one under a fresh name.
        var key = $"generations/{generation.Id}.png";
        await using (var imageData = await provider.DownloadImageAsync(imageUrl))
            await bucket.Upload(key, imageData);

        generation.ResultUrl = key;
        await db.SaveChangesAsync();

        return await bucket.GetUrl(key);
    }

    public async Task<GenerationSummary?> GetDetailsByRequestIdAsync(string falRequestId)
    {
        var g = await db.Generations.FirstOrDefaultAsync(g => g.FalRequestId == falRequestId);
        if (g is null) return null;

        var imageUrl = g.ResultUrl is null ? null : await bucket.GetUrl(g.ResultUrl);
        return new GenerationSummary(g.ImageModel, g.Prompt, imageUrl, g.CreditCost);
    }

    public async Task<IReadOnlyDictionary<string, GenerationSummary>> GetSummariesByRequestIdsAsync(IEnumerable<string> falRequestIds)
    {
        var ids = falRequestIds.ToList();
        var generations = await db.Generations
            .Where(g => g.FalRequestId != null && ids.Contains(g.FalRequestId!))
            .ToListAsync();

        // Signing means a URL per stored key — can't be done inside the EF query
        // above, so materialize first and mint URLs here.
        var summaries = new Dictionary<string, GenerationSummary>();
        foreach (var g in generations)
        {
            var imageUrl = g.ResultUrl is null ? null : await bucket.GetUrl(g.ResultUrl);
            summaries[g.FalRequestId!] = new GenerationSummary(g.ImageModel, g.Prompt, imageUrl, g.CreditCost);
        }
        return summaries;
    }
}
