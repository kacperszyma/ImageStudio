namespace GenerationManager.Contracts;

public interface IGenerationManager
{
    Task<GenerationResultDto> GenerateAsync(Guid userId, string modelSlug, string prompt);
}

public record GenerationResultDto(Guid JobId, string ImageUrl);
