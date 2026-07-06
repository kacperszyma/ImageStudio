using System.Diagnostics;
using System.Text.Json;
using Generation.Contracts;
using GenerationManager.Contracts;
using Microsoft.EntityFrameworkCore;
using Wallet.Contracts;

namespace GenerationManager;

internal sealed class GenerationManagerService(
    IGenerationService generationService,
    IGenerationQueryService queryService,
    IWalletService walletService,
    GenerationManagerDbContext db,
    GenerationManagerMetrics metrics) : IGenerationManager
{
    public async Task<Guid> GenerateAsync(Guid userId, string modelSlug, string prompt)
    {
        using var activity = GenerationManagerActivitySource.Instance.StartActivity("generation.create");
        activity?.SetTag("model_slug", modelSlug);

        var jobId = Guid.NewGuid();
        activity?.SetTag("job_id", jobId.ToString());
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

            metrics.JobCreated(modelSlug);
            activity?.SetTag("outcome", "created");
            return jobId;
        }
        catch
        {
            await walletService.UnfreezeAsync(jobId);
            job.Status = GenerationJobStatus.Failed;
            job.CompletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            metrics.JobSubmitFailed();
            activity?.SetTag("outcome", "rollback");
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

        var succeeded = success && imageUrl is not null;

        // Record the outcome AND the intent to act on it in one manager-DB
        // transaction: the status flip and the outbox row commit together, so a
        // settled job can never be missing the side effects it implies. The actual
        // work (charge/refund, record image, notify) is drained from the outbox by
        // the dispatcher, retried until each sticks.
        job.Status = succeeded ? GenerationJobStatus.Completed : GenerationJobStatus.Failed;
        job.CompletedAt = DateTime.UtcNow;
        db.Outbox.Add(Settlement(job.Id, succeeded, imageUrl));
        await db.SaveChangesAsync();

        metrics.JobSettled(succeeded ? "completed" : "failed", job.CompletedAt.Value - job.CreatedAt);
    }

    private static OutboxMessage Settlement(Guid jobId, bool success, string? imageUrl) => new()
    {
        Id = Guid.NewGuid(),
        JobId = jobId,
        Payload = JsonSerializer.Serialize(new SettlePayload(success, imageUrl)),
        CreatedAt = DateTime.UtcNow,
        TraceParent = Activity.Current?.Id,
    };

    public async Task<IReadOnlyList<GenerationHistoryItem>> GetHistoryAsync(Guid userId, int? limit = null)
    {
        var query = db.Jobs
            .Where(j => j.UserId == userId && j.Status == GenerationJobStatus.Completed)
            .OrderByDescending(j => j.CreatedAt)
            .AsQueryable();

        if (limit is > 0) query = query.Take(limit.Value);

        var jobs = await query.ToListAsync();

        var requestIds = jobs
            .Where(j => j.FalRequestId is not null)
            .Select(j => j.FalRequestId!)
            .ToList();

        var summaries = await queryService.GetSummariesByRequestIdsAsync(requestIds);

        return jobs.Select(j =>
        {
            summaries.TryGetValue(j.FalRequestId ?? "", out var s);
            return new GenerationHistoryItem(j.Id, s?.ModelSlug, s?.Prompt, s?.ImageUrl, s?.CreditCost, j.Status.ToString(), j.CreatedAt);
        }).ToList();
    }

    public async Task<GenerationDetailDto?> GetDetailsAsync(Guid jobId, Guid userId)
    {
        // Scope by userId: a caller may only read their own jobs. A non-owner
        // gets the same null as a missing job, so ids can't be probed.
        var job = await db.Jobs.FirstOrDefaultAsync(j => j.Id == jobId && j.UserId == userId);
        if (job is null) return null;

        GenerationSummary? summary = null;
        if (job.FalRequestId is not null)
            summary = await queryService.GetDetailsByRequestIdAsync(job.FalRequestId);

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
