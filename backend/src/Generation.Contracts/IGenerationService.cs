namespace Generation.Contracts;

public interface IGenerationService
{
    List<ModelDto> GetModels();
    long GetCost(string modelSlug);
    Task<GenerationJobDto> RunAsync(Guid jobId, string modelSlug, string prompt);
}

public record ModelDto(string Slug, long CreditCost);
public record GenerationJobDto(Guid JobId, string ImageUrl);
