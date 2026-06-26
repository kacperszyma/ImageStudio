using Microsoft.EntityFrameworkCore;
using Npgsql;
using Wallet;

namespace Wallet.IntegrationTests;

/// <summary>
/// Creates a fresh 'imagestudio_test' database on the local Postgres for each test run.
/// Tests are isolated by unique userId values — no truncation needed between tests.
/// </summary>
public sealed class WalletDbFixture : IAsyncLifetime
{
    private const string AdminConnStr =
        "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres";

    private const string TestDatabase = "imagestudio_test";

    public string ConnectionString { get; } =
        "Host=localhost;Port=5432;Database=imagestudio_test;Username=postgres;Password=postgres";

    public async Task InitializeAsync()
    {
        await using var conn = new NpgsqlConnection(AdminConnStr);
        await conn.OpenAsync();

        // Drop and recreate to start clean every test run
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = $"""
                SELECT pg_terminate_backend(pid)
                FROM pg_stat_activity
                WHERE datname = '{TestDatabase}' AND pid <> pg_backend_pid();
                """;
            await cmd.ExecuteNonQueryAsync();
        }
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = $"DROP DATABASE IF EXISTS {TestDatabase}";
            await cmd.ExecuteNonQueryAsync();
        }
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = $"CREATE DATABASE {TestDatabase}";
            await cmd.ExecuteNonQueryAsync();
        }

        await using var ctx = CreateContext();
        await ctx.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await using var conn = new NpgsqlConnection(AdminConnStr);
        await conn.OpenAsync();

        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = $"""
                SELECT pg_terminate_backend(pid)
                FROM pg_stat_activity
                WHERE datname = '{TestDatabase}' AND pid <> pg_backend_pid();
                """;
            await cmd.ExecuteNonQueryAsync();
        }
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = $"DROP DATABASE IF EXISTS {TestDatabase}";
            await cmd.ExecuteNonQueryAsync();
        }
    }

    internal WalletDbContext CreateContext()
    {
        var opts = new DbContextOptionsBuilder<WalletDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        return new WalletDbContext(opts);
    }

    internal WalletService CreateService() => new(CreateContext(), new NullPaymentGateway());

    private sealed class NullPaymentGateway : IPaymentGateway
    {
        public Task<string> CreateCheckoutSessionAsync(Guid userId, string packageId) => throw new NotSupportedException();
        public bool VerifyWebhookSignature(string payload, string signature) => throw new NotSupportedException();
        public CheckoutCompletedEvent? ParseCheckoutCompleted(string payload) => throw new NotSupportedException();
    }
}

[CollectionDefinition("wallet-db")]
public class WalletDbCollection : ICollectionFixture<WalletDbFixture> { }
