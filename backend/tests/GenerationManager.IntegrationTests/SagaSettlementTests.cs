using FluentAssertions;
using GenerationManager;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Wallet;

namespace GenerationManager.IntegrationTests;

/// <summary>
/// Exercises the cross-module billing saga across its weak seam — partial failure
/// of a multi-step settlement — now that settlement goes through a transactional
/// outbox: <see cref="GenerationManagerService.CompleteJobAsync"/> records the
/// outcome + intent atomically, and <see cref="OutboxDispatcher"/> applies the
/// side effects, retrying until each sticks.
/// </summary>
[Collection("saga-db")] 
public sealed class SagaSettlementTests(SagaDbFixture db)
{
    private static readonly FakeGenerationQueryService QueryService = new();

    private GenerationManagerService NewManager(FakeGenerationService gen) =>
        new(gen, QueryService, db.CreateWalletService(), db.CreateManagerContext(), db.ManagerMetrics);

    private OutboxDispatcher NewDispatcher(FakeGenerationService gen, RecordingNotifier notifier) =>
        new(db.CreateManagerContext(), db.CreateWalletService(), gen, notifier, db.ManagerMetrics,
            NullLogger<OutboxDispatcher>.Instance);

    [Fact]
    public async Task Completed_job_records_its_image_and_notifies_user_even_if_first_artifact_write_fails()
    {
        var userId = Guid.NewGuid();
        await db.CreateWalletService().EnsureAccountAsync(userId);

        var gen = new FakeGenerationService { Cost = 50, NextRequestId = "req-1" };
        var notifier = new RecordingNotifier();

        // Request #1 — user enqueues a generation. Funds get frozen.
        var jobId = await NewManager(gen).GenerateAsync(userId, "flux-schnell", "a cat");

        // Request #2 — the webhook settles the job: status flip + outbox row commit
        // together. No side effects happen here.
        await NewManager(gen).CompleteJobAsync("req-1", "http://img/cat.png", success: true);

        // Dispatch pass #1 — the charge commits, but recording the image throws.
        // The message is left unprocessed for the next pass.
        gen.FailNextArtifactWrite = true;
        await NewDispatcher(gen, notifier).DispatchPendingAsync();

        // Dispatch pass #2 — the retry. Charge is an idempotent no-op; the image is
        // recorded and the user notified.
        await NewDispatcher(gen, notifier).DispatchPendingAsync();

        await using var ctx = db.CreateManagerContext();
        var job = await ctx.Jobs.SingleAsync(j => j.Id == jobId);
        job.Status.Should().Be(GenerationJobStatus.Completed);
        (await db.CreateWalletService().GetBalanceAsync(userId))
            .Should().Be(WalletAccount.StartingBalance - 50);

        // The outbox healed the side effects the old inline path would have lost:
        gen.RecordedImageFor("req-1").Should().Be("http://img/cat.png");
        notifier.CompletedCount.Should().Be(1);

        // ...exactly once: the message is now marked processed.
        ctx.Outbox.Count(m => m.JobId == jobId && m.ProcessedAt == null).Should().Be(0);
    }

    [Fact]
    public async Task Stale_reconciler_refunds_a_job_whose_webhook_never_arrived()
    {
        // With the outbox, a charge only happens via the dispatcher AFTER a Completed
        // decision, so a Pending job is always still frozen (never charged). The
        // reconciler's job is therefore simply to refund genuinely abandoned jobs.
        var userId = Guid.NewGuid();
        await db.CreateWalletService().EnsureAccountAsync(userId);

        var gen = new FakeGenerationService { Cost = 50, NextRequestId = "req-stuck" };

        var jobId = await NewManager(gen).GenerateAsync(userId, "flux-schnell", "a dog");
        await BackdateAsync(jobId, DateTime.UtcNow.AddMinutes(-15)); // past the 10-min cutoff

        // Funds are frozen (debited, hold active) — not charged.
        (await db.CreateWalletService().GetBalanceAsync(userId))
            .Should().Be(WalletAccount.StartingBalance - 50);

        // The reconciler expires it and enqueues the refund+notify.
        await new StaleJobReconciler(db.CreateManagerContext(), db.ManagerMetrics).ReconcileAsync();

        // The dispatcher applies them.
        var notifier = new RecordingNotifier();
        await NewDispatcher(gen, notifier).DispatchPendingAsync();

        await using var ctx = db.CreateManagerContext();
        var job = await ctx.Jobs.SingleAsync(j => j.Id == jobId);
        job.Status.Should().Be(GenerationJobStatus.Expired);

        // The user is made whole and told it failed — never left out of pocket.
        (await db.CreateWalletService().GetBalanceAsync(userId))
            .Should().Be(WalletAccount.StartingBalance);
        notifier.FailedCount.Should().Be(1);
    }

    private async Task BackdateAsync(Guid jobId, DateTime createdAt)
    {
        await using var ctx = db.CreateManagerContext();
        var job = await ctx.Jobs.SingleAsync(j => j.Id == jobId);
        job.CreatedAt = createdAt;
        await ctx.SaveChangesAsync();
    }
}
