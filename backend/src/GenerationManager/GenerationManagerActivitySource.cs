using System.Diagnostics;

namespace GenerationManager;

public static class GenerationManagerActivitySource
{
    public const string Name = "ImageStudio.GenerationManager";
    public static readonly ActivitySource Instance = new(Name);
}
