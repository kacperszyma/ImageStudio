using GenerationManager.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace GenerationManager;

public static class GenerationManagerModule
{
    public static IServiceCollection AddGenerationManagerModule(this IServiceCollection services)
    {
        services.AddScoped<IGenerationManager, GenerationManagerService>();
        return services;
    }
}
