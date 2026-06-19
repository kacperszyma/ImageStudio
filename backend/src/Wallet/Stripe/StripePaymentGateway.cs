namespace Wallet.Stripe;

internal sealed class StripePaymentGateway : IPaymentGateway
{
    public Task<string> CreateCheckoutSessionAsync(Guid userId, string packageId) => throw new NotImplementedException();
    public bool VerifyWebhookSignature(string payload, string signature) => throw new NotImplementedException();
}
