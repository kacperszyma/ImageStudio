using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Wallet;

namespace Wallet.IntegrationTests;

/// <summary>
/// The Stripe top-up path must never take a customer's money without granting the
/// pebbles — even if an earlier webhook delivery failed partway (purchase recorded,
/// credit not yet applied) and Stripe redelivers.
/// </summary>
[Collection("wallet-db")]
public class StripeWebhookTests(WalletDbFixture db)
{
    [Fact]
    public async Task Webhook_grants_pebbles_even_if_a_prior_delivery_recorded_the_purchase_but_never_credited()
    {
        var userId = Guid.NewGuid();
        await db.CreateService().EnsureAccountAsync(userId);

        const string sessionId = "cs_test_partial";
        const string packageId = "pebbles_200"; // Basic: $1 → 200 pebbles

        // Simulate a crashed earlier delivery: the purchase row committed, but the
        // process died before the pebble credit committed. No ledger entry exists.
        await using (var ctx = db.CreateContext())
        {
            ctx.Purchases.Add(new WalletPurchase
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                PackageNameId = packageId,
                DollarAmount = 1m,
                PebbleAmount = 200,
                ExternalPaymentId = sessionId,
                CreatedAt = DateTime.UtcNow,
            });
            await ctx.SaveChangesAsync();
        }

        // Stripe redelivers the same checkout.session.completed event.
        var gateway = new FakePaymentGateway(new CheckoutCompletedEvent(sessionId, userId, packageId));
        await db.CreateService(gateway).ProcessPaymentWebhookAsync("payload", "sig");

        // The customer must end up with the pebbles they paid for...
        (await db.CreateService().GetBalanceAsync(userId))
            .Should().Be(WalletAccount.StartingBalance + 200);

        // ...credited exactly once, keyed on the Stripe session id.
        await using var check = db.CreateContext();
        check.Ledger.Count(l => l.WalletId == userId && l.Type == Contracts.TransactionType.TopUp)
            .Should().Be(1);
        check.Purchases.Count(p => p.ExternalPaymentId == sessionId).Should().Be(1);
    }

    [Fact]
    public async Task Webhook_is_idempotent_against_redelivery_of_a_fully_processed_event()
    {
        var userId = Guid.NewGuid();
        await db.CreateService().EnsureAccountAsync(userId);

        const string sessionId = "cs_test_dup";
        const string packageId = "pebbles_200";
        var gateway = new FakePaymentGateway(new CheckoutCompletedEvent(sessionId, userId, packageId));

        await db.CreateService(gateway).ProcessPaymentWebhookAsync("payload", "sig");
        await db.CreateService(gateway).ProcessPaymentWebhookAsync("payload", "sig"); // redelivery

        (await db.CreateService().GetBalanceAsync(userId))
            .Should().Be(WalletAccount.StartingBalance + 200); // credited once

        await using var check = db.CreateContext();
        check.Purchases.Count(p => p.ExternalPaymentId == sessionId).Should().Be(1);
        check.Ledger.Count(l => l.WalletId == userId && l.Type == Contracts.TransactionType.TopUp).Should().Be(1);
    }

    private sealed class FakePaymentGateway(CheckoutCompletedEvent evt) : IPaymentGateway
    {
        public bool VerifyWebhookSignature(string payload, string signature) => true;
        public CheckoutCompletedEvent? ParseCheckoutCompleted(string payload) => evt;
        public Task<string> CreateCheckoutSessionAsync(Guid userId, string packageId) => throw new NotSupportedException();
        public Task<CheckoutCompletedEvent?> FetchCompletedSessionAsync(string sessionId) => throw new NotSupportedException();
    }
}
