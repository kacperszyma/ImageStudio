using SharedKernel;

namespace Generation.Contracts;

public interface IGenerationService
{
    List<ModelDto> GetModels();
    long GetCost(string modelSlug);
    Task<string> SubmitAsync(Guid userId, string modelSlug, string prompt);

    /// <returns>The URL to show the user for this generation — not necessarily
    /// <paramref name="imageUrl"/> itself, since the image may be re-hosted in our
    /// own storage first.</returns>
    Task<string> CompleteGenerationAsync(string requestId, string imageUrl);
}

public interface IGenerationQueryService
{
    Task<GenerationSummary?> GetDetailsByRequestIdAsync(string falRequestId);
    Task<IReadOnlyDictionary<string, GenerationSummary>> GetSummariesByRequestIdsAsync(IEnumerable<string> falRequestIds);
}

/// <summary>Verifies and decodes a provider webhook. Used only by the API layer.</summary>
public interface IGenerationWebhook
{
    /// <exception cref="WebhookVerificationException">The webhook is not authentic.</exception>
    Task<GenerationCallback> ParseCallbackAsync(WebhookRequest request);
}

public record ModelDto(string Slug, long CreditCost);
public record GenerationCallback(string RequestId, bool Success, string? ImageUrl);
public record GenerationSummary(string ModelSlug, string Prompt, string? ImageUrl, long CreditCost);
