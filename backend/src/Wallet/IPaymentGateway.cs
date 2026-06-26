namespace Wallet;

internal interface IPaymentGateway
{
    Task<string> CreateCheckoutSessionAsync(Guid userId, string packageId);
    bool VerifyWebhookSignature(string payload, string signature);
    CheckoutCompletedEvent? ParseCheckoutCompleted(string payload);
}

internal record CheckoutCompletedEvent(string SessionId, Guid UserId, string PackageId);
