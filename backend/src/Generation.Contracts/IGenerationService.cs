namespace Generation.Contracts;

public interface IGenerationService
{
    List<ModelDto> GetModels();
    long GetCost(string modelSlug);
    Task<string> SubmitAsync(Guid userId, string modelSlug, string prompt);

    /// <summary>
    /// Decodes a provider's raw webhook body into a provider-agnostic result.
    /// The host hands over the raw bytes; the provider's wire format never
    /// crosses this boundary.
    /// </summary>
    GenerationCallback ParseCallback(byte[] body);

    /// <summary>Records the produced image against its generation (by request id).</summary>
    Task CompleteGenerationAsync(string requestId, string imageUrl);

    Task<IReadOnlyCollection<GenerationDetails>> GetGenerationHistory(Guid userId);
}

public record ModelDto(string Slug, long CreditCost);
public record GenerationCallback(string RequestId, bool Success, string? ImageUrl);
public record GenerationDetails(string modelSlug, string prompt, string? imageUrl);
