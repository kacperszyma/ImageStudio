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
        services.AddScoped<IGenerationProvider, MockGenerationProvider>();
        services.AddScoped<IGenerationService, GenerationService>();
        return services;
    }
}