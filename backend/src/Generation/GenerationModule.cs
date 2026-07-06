using Generation.Contracts;
using Generation.Fal;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Generation;

public static class GenerationModule
{
    public static IServiceCollection AddGenerationModule(
        this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<GenerationDbContext>(opt =>
            opt.UseNpgsql(config.GetConnectionString("Postgres")));
        services.AddHttpClient<FalClient>();
        services.AddHttpClient(); // IHttpClientFactory for the verifier's JWKS fetch
        services.AddSingleton<FalWebhookVerifier>(); // caches Fal's public keys
        services.AddSingleton<FalMetrics>();
        services.AddScoped<IGenerationProvider,FalGenerationProvider>();
       // services.AddScoped<IGenerationProvider, MockGenerationProvider>();
        services.AddScoped<GenerationService>();
        services.AddScoped<IGenerationService>(sp => sp.GetRequiredService<GenerationService>());
        services.AddScoped<IGenerationQueryService>(sp => sp.GetRequiredService<GenerationService>());
        services.AddScoped<IGenerationWebhook>(sp => sp.GetRequiredService<GenerationService>());
        return services;
    }

    public static Task ApplyMigrationsAsync(IServiceProvider sp) =>
        sp.GetRequiredService<GenerationDbContext>().Database.MigrateAsync();
}