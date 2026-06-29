using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Users.Contracts;

namespace Users;

public static class UsersModule
{
    public static IServiceCollection AddUsersModule(
        this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<UsersDbContext>(opt =>
            opt.UseNpgsql(config.GetConnectionString("Postgres")));
        services.AddScoped<IUserService, UserService>();
        return services;
    }

    public static Task ApplyMigrationsAsync(IServiceProvider sp) =>
        sp.GetRequiredService<UsersDbContext>().Database.MigrateAsync();
}
