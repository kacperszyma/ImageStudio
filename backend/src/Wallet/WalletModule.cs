using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Wallet;

public static class WalletModule
{
    public static IServiceCollection AddWalletModule(
        this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<WalletDbContext>(opt =>
            opt.UseNpgsql(config.GetConnectionString("Postgres")));
        return services;
    }
}