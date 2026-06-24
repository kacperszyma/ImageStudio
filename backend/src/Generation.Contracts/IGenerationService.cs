using SharedKernel;

namespace Generation.Contracts;

public interface IGenerationService
{
    List<ModelDto> GetModels();
    long GetCost(string modelSlug);
    Task<string> SubmitAsync(Guid userId, string modelSlug, string prompt);

    /// <summary>
    /// Verifies a provider's webhook is authentic, then decodes it into a
    /// provider-agnostic result. The host hands over the raw request; the
    /// provider's wire format and signature scheme never cross this boundary.
    /// Verification is fused with decoding so an unverified body can never
    /// produce a <see cref="GenerationCallback"/>.
    /// </summary>
    /// <exception cref="WebhookVerificationException">The webhook is not authentic.</exception>
    Task<GenerationCallback> ParseCallbackAsync(WebhookRequest request);

    /// <summary>Records the produced image against its generation (by request id).</summary>
    Task CompleteGenerationAsync(string requestId, string imageUrl);

    Task<IReadOnlyCollection<GenerationDetails>> GetGenerationHistory(Guid userId);
    Task<GenerationSummary?> GetDetailsByRequestIdAsync(string falRequestId);
    Task<IReadOnlyDictionary<string, GenerationSummary>> GetSummariesByRequestIdsAsync(IEnumerable<string> falRequestIds);
}

public record ModelDto(string Slug, long CreditCost);
public record GenerationCallback(string RequestId, bool Success, string? ImageUrl);
public record GenerationDetails(string modelSlug, string prompt, string? imageUrl, long creditCost);
public record GenerationSummary(string ModelSlug, string Prompt, string? ImageUrl, long CreditCost);
