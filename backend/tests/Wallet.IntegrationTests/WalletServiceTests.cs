using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Wallet;
using Wallet.Contracts;

namespace Wallet.IntegrationTests;

[Collection("wallet-db")]
public class WalletServiceTests(WalletDbFixture db)
{
    // ── EnsureAccount ────────────────────────────────────────────────────────

    [Fact]
    public async Task EnsureAccount_creates_account_with_starting_balance()
    {
        var userId = Guid.NewGuid();

        await db.CreateService().EnsureAccountAsync(userId);

        var balance = await db.CreateService().GetBalanceAsync(userId);
        balance.Should().Be(WalletAccount.StartingBalance);
    }

    [Fact]
    public async Task EnsureAccount_is_idempotent()
    {
        var userId = Guid.NewGuid();
        var svc = db.CreateService();

        await svc.EnsureAccountAsync(userId);
        await svc.EnsureAccountAsync(userId); // second call must not throw or duplicate

        await using var ctx = db.CreateContext();
        var count = await ctx.Accounts.CountAsync(a => a.UserId == userId);
        count.Should().Be(1);
    }

    // ── TopUp ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task TopUp_increases_balance_and_writes_ledger_entry()
    {
        var userId = Guid.NewGuid();
        await db.CreateService().EnsureAccountAsync(userId);

        await db.CreateService().TopUpAsync(userId, 500, "topup-1");

        await using var ctx = db.CreateContext();
        var account = await ctx.Accounts.FindAsync(userId);
        account!.Balance.Should().Be(WalletAccount.StartingBalance + 500);

        var entry = await ctx.Ledger.SingleAsync(l => l.WalletId == userId);
        entry.Amount.Should().Be(500);
        entry.Type.Should().Be(TransactionType.TopUp);
        entry.BalanceAfter.Should().Be(account.Balance);
    }

    [Fact]
    public async Task TopUp_duplicate_idempotency_key_is_silent_noop()
    {
        var userId = Guid.NewGuid();
        await db.CreateService().EnsureAccountAsync(userId);

        await db.CreateService().TopUpAsync(userId, 100, "topup-idem");
        await db.CreateService().TopUpAsync(userId, 100, "topup-idem"); // same key, second delivery

        await using var ctx = db.CreateContext();
        var account = await ctx.Accounts.FindAsync(userId);
        account!.Balance.Should().Be(WalletAccount.StartingBalance + 100); // credited once only

        var entries = await ctx.Ledger.Where(l => l.WalletId == userId).ToListAsync();
        entries.Should().HaveCount(1);
    }

    // ── Freeze ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Freeze_reduces_balance_increases_frozen_and_creates_hold()
    {
        var userId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var svc = db.CreateService();
        await svc.EnsureAccountAsync(userId);

        await svc.FreezeFundsAsync(userId, 50, jobId, "freeze-1");

        await using var ctx = db.CreateContext();
        var account = await ctx.Accounts.FindAsync(userId);
        account!.Balance.Should().Be(WalletAccount.StartingBalance - 50);
        account.Frozen.Should().Be(50);

        var hold = await ctx.Holds.SingleAsync(h => h.PurchaseId == jobId);
        hold.Amount.Should().Be(50);
        hold.Status.Should().Be("active");

        var entry = await ctx.Ledger.SingleAsync(l => l.WalletId == userId);
        entry.Type.Should().Be(TransactionType.Freeze);
        entry.Amount.Should().Be(50);
    }

    [Fact]
    public async Task Freeze_throws_when_balance_insufficient()
    {
        var userId = Guid.NewGuid();
        var svc = db.CreateService();
        await svc.EnsureAccountAsync(userId);

        var act = () => svc.FreezeFundsAsync(userId, WalletAccount.StartingBalance + 1, Guid.NewGuid(), "freeze-broke");

        await act.Should().ThrowAsync<InsufficientFundsException>();

        // balance and frozen must be unchanged
        await using var ctx = db.CreateContext();
        var account = await ctx.Accounts.FindAsync(userId);
        account!.Balance.Should().Be(WalletAccount.StartingBalance);
        account.Frozen.Should().Be(0);
    }

    [Fact]
    public async Task Freeze_duplicate_idempotency_key_is_silent_noop()
    {
        var userId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var svc = db.CreateService();
        await svc.EnsureAccountAsync(userId);

        await svc.FreezeFundsAsync(userId, 10, jobId, "freeze-idem");
        await svc.FreezeFundsAsync(userId, 10, jobId, "freeze-idem");

        await using var ctx = db.CreateContext();
        var account = await ctx.Accounts.FindAsync(userId);
        account!.Balance.Should().Be(WalletAccount.StartingBalance - 10); // debited once
        ctx.Holds.Count(h => h.PurchaseId == jobId).Should().Be(1);
    }

    // ── Freeze → Charge ──────────────────────────────────────────────────────

    [Fact]
    public async Task Charge_after_freeze_zeroes_frozen_and_balance_stays_debited()
    {
        var userId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var svc = db.CreateService();
        await svc.EnsureAccountAsync(userId);

        await svc.FreezeFundsAsync(userId, 80, jobId, "freeze-charge");
        await svc.ChargeFrozenAsync(jobId);

        await using var ctx = db.CreateContext();
        var account = await ctx.Accounts.FindAsync(userId);
        account!.Balance.Should().Be(WalletAccount.StartingBalance - 80);
        account.Frozen.Should().Be(0);

        var hold = await ctx.Holds.SingleAsync(h => h.PurchaseId == jobId);
        hold.Status.Should().Be("charged");
        hold.ReleasedAt.Should().NotBeNull();

        var entries = await ctx.Ledger.Where(l => l.WalletId == userId).ToListAsync();
        entries.Should().HaveCount(2); // freeze + charge
        entries.Should().ContainSingle(e => e.Type == TransactionType.Charge);
    }

    [Fact]
    public async Task Charge_twice_is_idempotent()
    {
        var userId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var svc = db.CreateService();
        await svc.EnsureAccountAsync(userId);

        await svc.FreezeFundsAsync(userId, 30, jobId, "freeze-charge2");
        await svc.ChargeFrozenAsync(jobId);
        await svc.ChargeFrozenAsync(jobId); // second call — hold is no longer active

        await using var ctx = db.CreateContext();
        var account = await ctx.Accounts.FindAsync(userId);
        account!.Frozen.Should().Be(0); // not negative
        ctx.Ledger.Count(l => l.WalletId == userId && l.Type == TransactionType.Charge).Should().Be(1);
    }

    // ── Freeze → Unfreeze ────────────────────────────────────────────────────

    [Fact]
    public async Task Unfreeze_restores_balance_and_zeroes_frozen()
    {
        var userId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var svc = db.CreateService();
        await svc.EnsureAccountAsync(userId);

        await svc.FreezeFundsAsync(userId, 60, jobId, "freeze-unfreeze");
        await svc.UnfreezeAsync(jobId);

        await using var ctx = db.CreateContext();
        var account = await ctx.Accounts.FindAsync(userId);
        account!.Balance.Should().Be(WalletAccount.StartingBalance);
        account.Frozen.Should().Be(0);

        var hold = await ctx.Holds.SingleAsync(h => h.PurchaseId == jobId);
        hold.Status.Should().Be("released");
        hold.ReleasedAt.Should().NotBeNull();

        var entries = await ctx.Ledger.Where(l => l.WalletId == userId).ToListAsync();
        entries.Should().HaveCount(2); // freeze + unfreeze
        entries.Should().ContainSingle(e => e.Type == TransactionType.Unfreeze);
    }

    [Fact]
    public async Task Unfreeze_twice_is_idempotent()
    {
        var userId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var svc = db.CreateService();
        await svc.EnsureAccountAsync(userId);

        await svc.FreezeFundsAsync(userId, 20, jobId, "freeze-unfreeze2");
        await svc.UnfreezeAsync(jobId);
        await svc.UnfreezeAsync(jobId);

        await using var ctx = db.CreateContext();
        var account = await ctx.Accounts.FindAsync(userId);
        account!.Balance.Should().Be(WalletAccount.StartingBalance); // not above starting
        ctx.Ledger.Count(l => l.WalletId == userId && l.Type == TransactionType.Unfreeze).Should().Be(1);
    }

    // ── GetTransactions ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetTransactions_returns_entries_newest_first()
    {
        var userId = Guid.NewGuid();
        await db.CreateService().EnsureAccountAsync(userId);

        await db.CreateService().TopUpAsync(userId, 100, "tx-order-1");
        await db.CreateService().TopUpAsync(userId, 200, "tx-order-2");

        var txns = await db.CreateService().GetTransactionsAsync(userId);
        txns.Should().HaveCount(2);
        txns[0].Amount.Should().Be(200); // newest first
        txns[1].Amount.Should().Be(100);
    }

    // ── Ledger reconciliation ────────────────────────────────────────────────

    [Fact]
    public async Task BalanceAfter_in_ledger_always_matches_current_balance()
    {
        var userId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var svc = db.CreateService();
        await svc.EnsureAccountAsync(userId);

        await svc.TopUpAsync(userId, 300, "reconcile-topup");
        await svc.FreezeFundsAsync(userId, 100, jobId, "reconcile-freeze");
        await svc.ChargeFrozenAsync(jobId);

        await using var ctx = db.CreateContext();
        var finalBalance = (await ctx.Accounts.FindAsync(userId))!.Balance;
        var lastEntry = await ctx.Ledger
            .Where(l => l.WalletId == userId)
            .OrderByDescending(l => l.CreatedAt)
            .FirstAsync();

        lastEntry.BalanceAfter.Should().Be(finalBalance);
    }
}
