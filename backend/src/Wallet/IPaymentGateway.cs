namespace Wallet;

internal interface IPaymentGateway
{
    Task<string> CreateCheckoutSessionAsync(Guid userId, string packageId);
    bool VerifyWebhookSignature(string payload, string signature);
}
