using System.Diagnostics;
using System.Text.Json;
using Generation.Contracts;
using GenerationManager.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Wallet.Contracts;

namespace GenerationManager;

internal interface IOutboxDispatcher
{
    Task DispatchPendingAsync(CancellationToken ct = default);
}

/// <summary>
/// Drains the outbox: for each undispatched settlement it applies the side effects
/// — charge or refund the funds, record the image, notify the user — then marks the
/// message done. Every effect is idempotent, so a message that fails partway is
/// safe to retry on the next pass (at-least-once delivery). A failing message is
/// left unprocessed and does not block the others.
/// </summary>
internal sealed class OutboxDispatcher(
    GenerationManagerDbContext db,
    IWalletService walletService,
    IGenerationService generationService,
    IGenerationNotifier notifier,
    GenerationManagerMetrics metrics,
    ILogger<OutboxDispatcher> logger) : IOutboxDispatcher
{
    public async Task DispatchPendingAsync(CancellationToken ct = default)
    {
        var pending = await db.Outbox
            .Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(ct);

        metrics.OutboxBacklog(pending.Count);
        if (pending.Count == 0) return; // the gauge above already says nothing's happening

        // Only span passes that do real work: this runs every 5s forever, and an
        // empty tick has no causal story worth a trace — the gauge covers it.
        using var tick = GenerationManagerActivitySource.Instance.StartActivity("outbox.dispatch_tick");
        tick?.SetTag("pending_count", pending.Count);

        int processedCount = 0, failedCount = 0;
        foreach (var message in pending)
        {
            // Link back to the trace that enqueued this message (a webhook
            // request, or a reconciliation sweep) so the two show up as related
            // in Tempo even though the original trace ended long ago.
            var links = ActivityContext.TryParse(message.TraceParent, null, out var origin)
                ? new[] { new ActivityLink(origin) }
                : null;
            using var activity = GenerationManagerActivitySource.Instance.StartActivity(
                "outbox.dispatch_message", ActivityKind.Internal, tick?.Context ?? default, links: links);
            activity?.SetTag("message_id", message.Id.ToString());
            activity?.SetTag("job_id", message.JobId.ToString());

            try
            {
                await DispatchAsync(message, ct);
                message.ProcessedAt = DateTime.UtcNow;
                metrics.OutboxMessageDispatched(message.ProcessedAt.Value - message.CreatedAt);
                activity?.SetTag("outcome", "dispatched");
                processedCount++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Leave it unprocessed; the next pass retries. Don't let one poison
                // message stall the rest.
                message.Attempts++;
                metrics.OutboxDispatchFailed();
                activity?.SetTag("outcome", "failed");
                failedCount++;
                logger.LogError(ex,
                    "Outbox dispatch failed for message {MessageId} (job {JobId}), attempt {Attempts}.",
                    message.Id, message.JobId, message.Attempts);
            }
            await db.SaveChangesAsync(ct);
        }

        tick?.SetTag("processed_count", processedCount);
        tick?.SetTag("failed_count", failedCount);

        if (failedCount > 0)
            logger.LogWarning(
                "Outbox dispatch tick processed {ProcessedCount} message(s), {FailedCount} failed.",
                processedCount, failedCount);
    }

    private async Task DispatchAsync(OutboxMessage message, CancellationToken ct)
    {
        var job = await db.Jobs.FirstOrDefaultAsync(j => j.Id == message.JobId, ct);
        if (job is null) return; // job vanished — nothing to settle

        var payload = JsonSerializer.Deserialize<SettlePayload>(message.Payload)
            ?? throw new InvalidOperationException($"Outbox message {message.Id} has an empty payload.");

        if (payload.Success)
        {
            // Defaults to the provider's own URL; overwritten below once the image
            // has actually been re-hosted in our storage.
            var displayUrl = payload.ImageUrl ?? "";

            if (job.FalRequestId is not null && payload.ImageUrl is not null)
            {
                var savedUrl = await TrySaveImageAsync(job, payload.ImageUrl);
                if (savedUrl is null)
                {
                    // Image never made it to storage after a retry — this generation
                    // can't be delivered. Unwind it the same way a provider-reported
                    // failure is handled: refund the frozen credits, tell the user,
                    // done. No further outbox retries — the message is still marked
                    // processed by the caller once this method returns.
                    Activity.Current?.SetTag("job_outcome", "image_save_failed");
                    metrics.ImageSaveFailed();
                    metrics.JobSettled("image_save_failed", DateTime.UtcNow - job.CreatedAt);

                    await walletService.UnfreezeAsync(job.Id);
                    job.Status = GenerationJobStatus.Failed;
                    job.CompletedAt = DateTime.UtcNow;
                    await notifier.FailedAsync(job.UserId, job.Id);
                    return;
                }
                displayUrl = savedUrl;
            }

            await walletService.ChargeFrozenAsync(job.Id);
            await notifier.CompletedAsync(job.UserId, job.Id, displayUrl);
        }
        else
        {
            await walletService.UnfreezeAsync(job.Id);
            await notifier.FailedAsync(job.UserId, job.Id);
        }
    }

    /// <summary>One retry on top of the first attempt — enough to ride out a transient
    /// blip talking to the provider or the bucket without leaving the job to retry
    /// forever on the outbox's own 5s loop. Returns the URL to show the user, or
    /// null once both attempts are exhausted.</summary>
    private async Task<string?> TrySaveImageAsync(GenerationJob job, string imageUrl)
    {
        for (var attempt = 1; attempt <= 2; attempt++)
        {
            try
            {
                var url = await generationService.CompleteGenerationAsync(job.FalRequestId!, imageUrl);
                Activity.Current?.SetTag("image_save_attempts", attempt);
                return url;
            }
            catch (Exception ex)
            {
                if (attempt == 2)
                {
                    logger.LogWarning(ex, "Saving image for job {JobId} failed after {Attempts} attempts; giving up.", job.Id, attempt);
                    Activity.Current?.SetTag("image_save_attempts", attempt);
                    return null;
                }
                metrics.ImageSaveRetried();
                logger.LogWarning(ex, "Saving image for job {JobId} failed on attempt {Attempt}; retrying once.", job.Id, attempt);
            }
        }
        return null; // unreachable — loop always returns from inside
    }
}
