using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Wallet;

namespace Wallet.IntegrationTests;

/// <summary>
/// Creates a fresh 'imagestudio_test' database on the local Postgres for each test run.
/// Tests are isolated by unique userId values — no truncation needed between tests.
/// </summary>
public sealed class WalletDbFixture : IAsyncLifetime
{
    // Host/port come from PGHOST/PGPORT (default localhost:5432) so the same tests
    // run locally and inside the Dagger pipeline, where Postgres is a bound service
    // reachable at host 'db'.
    private static string Host => Environment.GetEnvironmentVariable("PGHOST") ?? "localhost";
    private static string Port => Environment.GetEnvironmentVariable("PGPORT") ?? "5432";

    private static string AdminConnStr =>
        $"Host={Host};Port={Port};Database=postgres;Username=postgres;Password=postgres";

    private const string TestDatabase = "imagestudio_test";

    public string ConnectionString =>
        $"Host={Host};Port={Port};Database={TestDatabase};Username=postgres;Password=postgres";

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

    // Shared for the fixture's lifetime (one per test collection) so tests that
    // care about emitted measurements can attach a MeterListener to it.
    internal WalletMetrics Metrics { get; } = new();

    internal WalletService CreateService() =>
        new(CreateContext(), new NullPaymentGateway(), Metrics, NullLogger<WalletService>.Instance);

    internal WalletService CreateService(IPaymentGateway gateway) =>
        new(CreateContext(), gateway, Metrics, NullLogger<WalletService>.Instance);

    private sealed class NullPaymentGateway : IPaymentGateway
    {
        public Task<string> CreateCheckoutSessionAsync(Guid userId, string packageId) => throw new NotSupportedException();
        public bool VerifyWebhookSignature(string payload, string signature) => throw new NotSupportedException();
        public CheckoutCompletedEvent? ParseCheckoutCompleted(string payload) => throw new NotSupportedException();
        public Task<CheckoutCompletedEvent?> FetchCompletedSessionAsync(string sessionId) => throw new NotSupportedException();
    }
}

[CollectionDefinition("wallet-db")]
public class WalletDbCollection : ICollectionFixture<WalletDbFixture> { }
