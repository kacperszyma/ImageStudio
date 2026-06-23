using GenerationManager.Contracts;
using Microsoft.EntityFrameworkCore;
using Wallet.Contracts;

namespace GenerationManager;

internal interface IStaleJobReconciler
{
    Task ReconcileAsync(CancellationToken ct = default);
}

/// <summary>
/// Releases jobs stranded in <see cref="GenerationJobStatus.Pending"/> — those
/// whose provider webhook never arrived (lost delivery, crash before the request
/// id was recorded, endpoint down past the provider's retry budget). Without
/// this, their frozen funds would stay frozen forever.
/// </summary>
internal sealed class StaleJobReconciler(
    GenerationManagerDbContext db,
    IWalletService walletService,
    IGenerationNotifier notifier) : IStaleJobReconciler
{
    private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(10);

    public async Task ReconcileAsync(CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow - Timeout;

        var stale = await db.Jobs
            .Where(j => j.Status == GenerationJobStatus.Pending && j.CreatedAt < cutoff)
            .ToListAsync(ct);

        foreach (var job in stale)
        {
            // Same Pending guard as CompleteJobAsync: a webhook that settles a
            // job first flips its status, so this sweep won't touch it (and a
            // job expired here is no longer Pending, so a late webhook no-ops).
            await walletService.UnfreezeAsync(job.Id);
            job.Status = GenerationJobStatus.Expired;
            job.CompletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);

            await notifier.FailedAsync(job.UserId, job.Id);
        }
    }
}
