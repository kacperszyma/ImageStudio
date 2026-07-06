using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wallet.Contracts;
using Wallet.StripeGateway;

namespace Wallet;

public static class WalletModule
{
    public static IServiceCollection AddWalletModule(
        this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<WalletDbContext>(opt =>
            opt.UseNpgsql(config.GetConnectionString("Postgres")));
        services.AddSingleton<WalletMetrics>();
        services.AddScoped<IPaymentGateway, StripePaymentGateway>();
        services.AddScoped<IWalletService, WalletService>();
        return services;
    }

    public static Task ApplyMigrationsAsync(IServiceProvider sp) =>
        sp.GetRequiredService<WalletDbContext>().Database.MigrateAsync();
}