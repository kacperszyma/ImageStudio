using Generation.Contracts;
using GenerationManager.Contracts;
using Microsoft.EntityFrameworkCore;
using Wallet.Contracts;

namespace GenerationManager;

internal sealed class GenerationManagerService(
    IGenerationService generationService,
    IWalletService walletService,
    IGenerationNotifier notifier,
    GenerationManagerDbContext db) : IGenerationManager
{
    public async Task<Guid> GenerateAsync(Guid userId, string modelSlug, string prompt)
    {
        var jobId = Guid.NewGuid();
        var cost = generationService.GetCost(modelSlug);

        var job = new GenerationJob
        {
            Id = jobId,
            UserId = userId,
            Status = GenerationJobStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        db.Jobs.Add(job);
        await db.SaveChangesAsync();

        await walletService.FreezeFundsAsync(userId, cost, jobId, jobId.ToString());

        try
        {
            // Persist the provider's request id so the webhook can correlate
            // its callback back to this job.
            var requestId = await generationService.SubmitAsync(userId, modelSlug, prompt);
            job.FalRequestId = requestId;
            await db.SaveChangesAsync();

            return jobId;
        }
        catch
        {
            await walletService.UnfreezeAsync(jobId);
            job.Status = GenerationJobStatus.Failed;
            job.CompletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            throw;
        }
    }

    public async Task CompleteJobAsync(string requestId, string? imageUrl, bool success)
    {
        var job = await db.Jobs.FirstOrDefaultAsync(j => j.FalRequestId == requestId);

        // Unknown request, or already settled: no-op. This is what makes the
        // call idempotent against the provider's webhook retries.
        if (job is null || job.Status != GenerationJobStatus.Pending)
            return;

        if (success && imageUrl is not null)
        {
            await walletService.ChargeFrozenAsync(job.Id);
            job.Status = GenerationJobStatus.Completed;
            job.CompletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            // Generation owns the artifact; have it record the produced image.
            await generationService.CompleteGenerationAsync(requestId, imageUrl);

            await notifier.CompletedAsync(job.UserId, job.Id, imageUrl);
        }
        else
        {
            await walletService.UnfreezeAsync(job.Id);
            job.Status = GenerationJobStatus.Failed;
            job.CompletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            await notifier.FailedAsync(job.UserId, job.Id);
        }
    }

    public async Task<IReadOnlyList<GenerationHistoryItem>> GetHistoryAsync(Guid userId)
    {
        var jobs = await db.Jobs
            .Where(j => j.UserId == userId)
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync();

        var requestIds = jobs
            .Where(j => j.FalRequestId is not null)
            .Select(j => j.FalRequestId!)
            .ToList();

        var summaries = await generationService.GetSummariesByRequestIdsAsync(requestIds);

        return jobs.Select(j =>
        {
            summaries.TryGetValue(j.FalRequestId ?? "", out var s);
            return new GenerationHistoryItem(j.Id, s?.ModelSlug, s?.Prompt, s?.ImageUrl, s?.CreditCost, j.Status.ToString(), j.CreatedAt);
        }).ToList();
    }

    public async Task<GenerationDetailDto?> GetDetailsAsync(Guid jobId)
    {
        var job = await db.Jobs.FirstOrDefaultAsync(j => j.Id == jobId);
        if (job is null) return null;

        GenerationSummary? summary = null;
        if (job.FalRequestId is not null)
            summary = await generationService.GetDetailsByRequestIdAsync(job.FalRequestId);

        var walletDetails = await walletService.GetGenerationWalletDetailsAsync(jobId);

        var duration = job.CompletedAt.HasValue
            ? job.CompletedAt.Value - job.CreatedAt
            : (TimeSpan?)null;

        return new GenerationDetailDto(
            JobId: jobId,
            ModelSlug: summary?.ModelSlug,
            Prompt: summary?.Prompt,
            ImageUrl: summary?.ImageUrl,
            CreditCost: summary?.CreditCost,
            Status: job.Status.ToString(),
            CreatedAt: job.CreatedAt,
            CompletedAt: job.CompletedAt,
            Duration: duration,
            BalanceBefore: walletDetails?.BalanceBefore,
            BalanceAfter: walletDetails?.BalanceAfter);
    }
}
