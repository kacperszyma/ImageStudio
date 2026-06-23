using GenerationManager.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GenerationManager;

public static class GenerationManagerModule
{
    public static IServiceCollection AddGenerationManagerModule(
        this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<GenerationManagerDbContext>(opt =>
            opt.UseNpgsql(config.GetConnectionString("Postgres")));
        services.AddScoped<IGenerationManager, GenerationManagerService>();
        services.AddScoped<IStaleJobReconciler, StaleJobReconciler>();
        services.AddHostedService<ReconciliationWorker>();
        return services;
    }
}
