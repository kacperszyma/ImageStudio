using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace GenerationManager;

internal interface IStaleJobReconciler
{
    Task ReconcileAsync(CancellationToken ct = default);
}

/// <summary>
/// Releases jobs stranded in <see cref="GenerationJobStatus.Pending"/> — those
/// whose provider webhook never arrived (lost delivery, crash before the request
/// id was recorded, endpoint down past the provider's retry budget). Such a job
/// was never charged (the charge only happens via the outbox after a Completed
/// decision), so its funds are still frozen and must be refunded.
/// </summary>
internal sealed class StaleJobReconciler(GenerationManagerDbContext db, GenerationManagerMetrics metrics) : IStaleJobReconciler
{
    private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(10);

    public async Task ReconcileAsync(CancellationToken ct = default)
    {
        // This is the only span in the sweep: it runs on its own timer with no
        // request behind it, so without this it's invisible in Tempo entirely.
        using var activity = GenerationManagerActivitySource.Instance.StartActivity("outbox.reconcile_stale_jobs");

        var cutoff = DateTime.UtcNow - Timeout;

        var stale = await db.Jobs
            .Where(j => j.Status == GenerationJobStatus.Pending && j.CreatedAt < cutoff)
            .ToListAsync(ct);

        activity?.SetTag("expired_count", stale.Count);

        foreach (var job in stale)
        {
            // Same Pending guard as CompleteJobAsync: a webhook that settles a
            // job first flips its status, so this sweep won't touch it (and a
            // job expired here is no longer Pending, so a late webhook no-ops).
            // Flip status and enqueue the refund+notify in one transaction; the
            // dispatcher applies them, just like a failed settlement.
            job.Status = GenerationJobStatus.Expired;
            job.CompletedAt = DateTime.UtcNow;
            db.Outbox.Add(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                JobId = job.Id,
                Payload = JsonSerializer.Serialize(new SettlePayload(Success: false, ImageUrl: null)),
                CreatedAt = DateTime.UtcNow,
                TraceParent = activity?.Id,
            });
            await db.SaveChangesAsync(ct);

            metrics.JobSettled("expired", job.CompletedAt.Value - job.CreatedAt);
        }
    }
}
