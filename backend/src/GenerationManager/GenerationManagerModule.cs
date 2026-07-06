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
        services.AddSingleton<GenerationManagerMetrics>();
        services.AddScoped<IGenerationManager, GenerationManagerService>();
        services.AddScoped<IStaleJobReconciler, StaleJobReconciler>();
        services.AddScoped<IOutboxDispatcher, OutboxDispatcher>();
        services.AddHostedService<ReconciliationWorker>();
        services.AddHostedService<OutboxWorker>();
        return services;
    }

    public static Task ApplyMigrationsAsync(IServiceProvider sp) =>
        sp.GetRequiredService<GenerationManagerDbContext>().Database.MigrateAsync();
}
