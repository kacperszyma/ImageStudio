using System.Diagnostics.Metrics;

namespace Wallet;

/// <summary>
/// Instruments for wallet/billing events: funds shortfalls, completed
/// purchases, and forged payment webhooks. One instance for the process
/// lifetime, shared by every scoped <see cref="WalletService"/>.
/// </summary>
public sealed class WalletMetrics
{
    public const string MeterName = "ImageStudio.Wallet";

    private readonly Counter<long> _insufficientFunds;
    private readonly Counter<long> _purchasesCompleted;
    private readonly Counter<long> _stripeWebhookVerificationFailed;

    public WalletMetrics()
    {
        var meter = new Meter(MeterName);
        _insufficientFunds = meter.CreateCounter<long>("wallet.insufficient_funds");
        _purchasesCompleted = meter.CreateCounter<long>("wallet.purchases.completed");
        _stripeWebhookVerificationFailed = meter.CreateCounter<long>("stripe.webhook.verification_failed");
    }

    public void InsufficientFunds() => _insufficientFunds.Add(1);

    public void PurchaseCompleted(string packageNameId) =>
        _purchasesCompleted.Add(1, new KeyValuePair<string, object?>("package", packageNameId));

    public void StripeWebhookVerificationFailed() => _stripeWebhookVerificationFailed.Add(1);
}
