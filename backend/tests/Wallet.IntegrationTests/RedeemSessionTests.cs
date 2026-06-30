using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Wallet;
using Wallet.Contracts;

namespace Wallet.IntegrationTests;

/// <summary>
/// RedeemSessionAsync credits pebbles by fetching the Stripe session directly,
/// rather than waiting for a webhook. The same session-id idempotency key is used
/// in both paths, so whichever fires first wins and the other is a no-op.
/// </summary>
[Collection("wallet-db")]
public class RedeemSessionTests(WalletDbFixture db)
{
    // ── Happy path ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Redeem_credits_pebbles_for_a_paid_session()
    {
        var userId = Guid.NewGuid();
        await db.CreateService().EnsureAccountAsync(userId);

        const string sessionId = "cs_redeem_basic";
        const string packageId = "pebbles_200";
        var gateway = new FakeSessionGateway(new CheckoutCompletedEvent(sessionId, userId, packageId));

        await db.CreateService(gateway).RedeemSessionAsync(sessionId, userId);

        (await db.CreateService().GetBalanceAsync(userId))
            .Should().Be(WalletAccount.StartingBalance + 200);

        await using var ctx = db.CreateContext();
        ctx.Ledger.Count(l => l.WalletId == userId && l.Type == TransactionType.TopUp).Should().Be(1);
        ctx.Purchases.Count(p => p.ExternalPaymentId == sessionId).Should().Be(1);
    }

    // ── Idempotency ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Redeem_twice_credits_pebbles_exactly_once()
    {
        var userId = Guid.NewGuid();
        await db.CreateService().EnsureAccountAsync(userId);

        const string sessionId = "cs_redeem_idem";
        const string packageId = "pebbles_200";
        var gateway = new FakeSessionGateway(new CheckoutCompletedEvent(sessionId, userId, packageId));

        await db.CreateService(gateway).RedeemSessionAsync(sessionId, userId);
        await db.CreateService(gateway).RedeemSessionAsync(sessionId, userId);

        (await db.CreateService().GetBalanceAsync(userId))
            .Should().Be(WalletAccount.StartingBalance + 200);

        await using var ctx = db.CreateContext();
        ctx.Ledger.Count(l => l.WalletId == userId && l.Type == TransactionType.TopUp).Should().Be(1);
        ctx.Purchases.Count(p => p.ExternalPaymentId == sessionId).Should().Be(1);
    }

    [Fact]
    public async Task Redeem_then_webhook_credits_pebbles_exactly_once()
    {
        var userId = Guid.NewGuid();
        await db.CreateService().EnsureAccountAsync(userId);

        const string sessionId = "cs_redeem_then_webhook";
        const string packageId = "pebbles_200";
        var evt = new CheckoutCompletedEvent(sessionId, userId, packageId);
        var gateway = new FakeSessionGateway(evt);

        await db.CreateService(gateway).RedeemSessionAsync(sessionId, userId);
        // Webhook fires later — must be a no-op, not a double-credit.
        await db.CreateService(new FakeWebhookGateway(evt)).ProcessPaymentWebhookAsync("payload", "sig");

        (await db.CreateService().GetBalanceAsync(userId))
            .Should().Be(WalletAccount.StartingBalance + 200);

        await using var ctx = db.CreateContext();
        ctx.Ledger.Count(l => l.WalletId == userId && l.Type == TransactionType.TopUp).Should().Be(1);
    }

    [Fact]
    public async Task Webhook_then_redeem_credits_pebbles_exactly_once()
    {
        var userId = Guid.NewGuid();
        await db.CreateService().EnsureAccountAsync(userId);

        const string sessionId = "cs_webhook_then_redeem";
        const string packageId = "pebbles_200";
        var evt = new CheckoutCompletedEvent(sessionId, userId, packageId);

        await db.CreateService(new FakeWebhookGateway(evt)).ProcessPaymentWebhookAsync("payload", "sig");
        await db.CreateService(new FakeSessionGateway(evt)).RedeemSessionAsync(sessionId, userId);

        (await db.CreateService().GetBalanceAsync(userId))
            .Should().Be(WalletAccount.StartingBalance + 200);

        await using var ctx = db.CreateContext();
        ctx.Ledger.Count(l => l.WalletId == userId && l.Type == TransactionType.TopUp).Should().Be(1);
    }

    // ── Guards ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Redeem_is_noop_when_session_not_paid()
    {
        var userId = Guid.NewGuid();
        await db.CreateService().EnsureAccountAsync(userId);

        var gateway = new FakeSessionGateway(null); // gateway returns null = not paid
        await db.CreateService(gateway).RedeemSessionAsync("cs_unpaid", userId);

        (await db.CreateService().GetBalanceAsync(userId))
            .Should().Be(WalletAccount.StartingBalance);
    }

    [Fact]
    public async Task Redeem_is_noop_when_session_belongs_to_different_user()
    {
        var realOwner = Guid.NewGuid();
        var attacker = Guid.NewGuid();
        await db.CreateService().EnsureAccountAsync(realOwner);
        await db.CreateService().EnsureAccountAsync(attacker);

        const string sessionId = "cs_wrong_user";
        const string packageId = "pebbles_200";
        // Session metadata says realOwner, but attacker is calling redeem.
        var gateway = new FakeSessionGateway(new CheckoutCompletedEvent(sessionId, realOwner, packageId));

        await db.CreateService(gateway).RedeemSessionAsync(sessionId, attacker);

        // Neither account credited.
        (await db.CreateService().GetBalanceAsync(realOwner)).Should().Be(WalletAccount.StartingBalance);
        (await db.CreateService().GetBalanceAsync(attacker)).Should().Be(WalletAccount.StartingBalance);
    }

    // ── Fakes ────────────────────────────────────────────────────────────────

    /// <summary>Simulates FetchCompletedSessionAsync (the redeem path).</summary>
    private sealed class FakeSessionGateway(CheckoutCompletedEvent? result) : IPaymentGateway
    {
        public Task<CheckoutCompletedEvent?> FetchCompletedSessionAsync(string sessionId) =>
            Task.FromResult(result);
        public bool VerifyWebhookSignature(string payload, string signature) => throw new NotSupportedException();
        public CheckoutCompletedEvent? ParseCheckoutCompleted(string payload) => throw new NotSupportedException();
        public Task<string> CreateCheckoutSessionAsync(Guid userId, string packageId) => throw new NotSupportedException();
    }

    /// <summary>Simulates the webhook path (signature passes, event parses).</summary>
    private sealed class FakeWebhookGateway(CheckoutCompletedEvent evt) : IPaymentGateway
    {
        public bool VerifyWebhookSignature(string payload, string signature) => true;
        public CheckoutCompletedEvent? ParseCheckoutCompleted(string payload) => evt;
        public Task<CheckoutCompletedEvent?> FetchCompletedSessionAsync(string sessionId) => throw new NotSupportedException();
        public Task<string> CreateCheckoutSessionAsync(Guid userId, string packageId) => throw new NotSupportedException();
    }
}
