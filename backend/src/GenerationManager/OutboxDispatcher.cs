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
        // The only span covering this pass: it's a background timer, not a
        // request, so nothing else in the auto-instrumented pipeline sees it.
        using var tick = GenerationManagerActivitySource.Instance.StartActivity("outbox.dispatch_tick");

        var pending = await db.Outbox
            .Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(ct);

        metrics.OutboxBacklog(pending.Count);
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
    }

    private async Task DispatchAsync(OutboxMessage message, CancellationToken ct)
    {
        var job = await db.Jobs.FirstOrDefaultAsync(j => j.Id == message.JobId, ct);
        if (job is null) return; // job vanished — nothing to settle

        var payload = JsonSerializer.Deserialize<SettlePayload>(message.Payload)
            ?? throw new InvalidOperationException($"Outbox message {message.Id} has an empty payload.");

        if (payload.Success)
        {
            await walletService.ChargeFrozenAsync(job.Id);
            if (job.FalRequestId is not null && payload.ImageUrl is not null)
                await generationService.CompleteGenerationAsync(job.FalRequestId, payload.ImageUrl);
            await notifier.CompletedAsync(job.UserId, job.Id, payload.ImageUrl ?? "");
        }
        else
        {
            await walletService.UnfreezeAsync(job.Id);
            await notifier.FailedAsync(job.UserId, job.Id);
        }
    }
}
