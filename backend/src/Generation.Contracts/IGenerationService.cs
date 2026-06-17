namespace Generation.Contracts;

public interface IGenerationService
{
    List<ModelDto> GetModels();
}

public record ModelDto(string name);
