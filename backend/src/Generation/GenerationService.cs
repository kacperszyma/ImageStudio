using Generation.Contracts;

namespace Generation;

public class GenerationService : IGenerationService
{
    public List<ModelDto> GetModels()
    {
        return ImageModel.All.Select(m => new ModelDto(m.Slug)).ToList();
    }
}