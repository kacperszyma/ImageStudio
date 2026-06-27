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
    ILogger<OutboxDispatcher> logger) : IOutboxDispatcher
{
    public async Task DispatchPendingAsync(CancellationToken ct = default)
    {
        var pending = await db.Outbox
            .Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(ct);

        foreach (var message in pending)
        {
            try
            {
                await DispatchAsync(message, ct);
                message.ProcessedAt = DateTime.UtcNow;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Leave it unprocessed; the next pass retries. Don't let one poison
                // message stall the rest.
                message.Attempts++;
                logger.LogError(ex,
                    "Outbox dispatch failed for message {MessageId} (job {JobId}), attempt {Attempts}.",
                    message.Id, message.JobId, message.Attempts);
            }
            await db.SaveChangesAsync(ct);
        }
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
