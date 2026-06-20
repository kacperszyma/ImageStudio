namespace Generation.Contracts;

public interface IGenerationService
{
    List<ModelDto> GetModels();
    long GetCost(string modelSlug);
    Task<GenerationJobDto> RunAsync(Guid jobId, Guid userId, string modelSlug, string prompt);

    Task<IReadOnlyCollection<GenerationDetails>> GetGenerationHistory(Guid userId);
}

public record ModelDto(string Slug, long CreditCost);
public record GenerationJobDto(Guid JobId, string ImageUrl);

public record GenerationDetails(string modelSlug, string prompt, string imageUrl);
