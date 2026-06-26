using Microsoft.Extensions.Configuration;
using Stripe;
using Stripe.Checkout;

namespace Wallet.StripeGateway;

internal sealed class StripePaymentGateway(IConfiguration config) : IPaymentGateway
{
    private readonly string _webhookSecret = config["STRIPE_WEBHOOK_SECRET"]
        ?? throw new InvalidOperationException("STRIPE_WEBHOOK_SECRET is not configured.");

    private readonly StripeClient _client = new(config["STRIPE_SECRET_KEY"]
        ?? throw new InvalidOperationException("STRIPE_SECRET_KEY is not configured."));

    private readonly string _returnUrl = config["STRIPE_RETURN_URL"]
        ?? throw new InvalidOperationException("STRIPE_RETURN_URL is not configured.");

    public async Task<string> CreateCheckoutSessionAsync(Guid userId, string packageId)
    {
        var package = PebblePackage.FromName(packageId);

        var options = new SessionCreateOptions
        {
            UiMode = "embedded_page",
            Mode = "payment",
            LineItems =
            [
                new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "usd",
                        UnitAmountDecimal = package.DollarPrice * 100,
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = $"{package.PebbleAmount} Pebbles",
                        },
                    },
                    Quantity = 1,
                }
            ],
            Metadata = new Dictionary<string, string>
            {
                ["userId"] = userId.ToString(),
                ["packageId"] = packageId,
            },
            ReturnUrl = _returnUrl,
        };

        var service = new SessionService(_client);
        var session = await service.CreateAsync(options);
        return session.ClientSecret;
    }

    public bool VerifyWebhookSignature(string payload, string signature)
    {
        try
        {
            EventUtility.ConstructEvent(payload, signature, _webhookSecret);
            return true;
        }
        catch (StripeException)
        {
            return false;
        }
    }

    public CheckoutCompletedEvent? ParseCheckoutCompleted(string payload)
    {
        var stripeEvent = EventUtility.ParseEvent(payload);
        if (stripeEvent.Type != EventTypes.CheckoutSessionCompleted)
            return null;

        var session = stripeEvent.Data.Object as global::Stripe.Checkout.Session;
        if (session?.Metadata is null) return null;

        if (!session.Metadata.TryGetValue("userId", out var userIdStr) ||
            !Guid.TryParse(userIdStr, out var userId))
            return null;

        if (!session.Metadata.TryGetValue("packageId", out var packageId))
            return null;

        return new CheckoutCompletedEvent(session.Id, userId, packageId);
    }
}
