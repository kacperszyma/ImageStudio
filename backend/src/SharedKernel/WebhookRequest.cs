namespace SharedKernel;

/// <summary>
/// An inbound third-party webhook, transport-neutral. The host fills this from
/// whatever web framework it runs on; each provider (Fal, Stripe, …) owns which
/// body bytes and header names actually mean something. Keeps both the web
/// framework and any provider's wire format out of the module contracts.
/// </summary>
/// <param name="Body">The raw request body, exactly as received. Signature
/// checks hash these bytes, so they must never be re-serialized upstream.</param>
/// <param name="Headers">Request headers, keyed case-insensitively.</param>
public sealed record WebhookRequest(
    byte[] Body,
    IReadOnlyDictionary<string, string> Headers);

/// <summary>
/// Thrown when a webhook fails authenticity checks (missing/forged signature,
/// stale timestamp). The host maps this to 401 and processes nothing.
/// </summary>
public sealed class WebhookVerificationException(string message) : Exception(message);
