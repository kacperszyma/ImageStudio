using GenerationManager;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Wallet;
using Wallet.Contracts;

namespace GenerationManager.IntegrationTests;

/// <summary>
/// Spins up a clean 'imagestudio_saga_test' database holding BOTH the wallet and
/// generation-manager schemas, so the cross-module billing saga can be exercised
/// end to end against real Postgres (real transactions, real row locks).
///
/// Host/port come from PGHOST/PGPORT (default localhost:5432) so the same tests
/// run locally and inside the Dagger pipeline, where Postgres is a bound service
/// reachable at host 'db'.
/// </summary>
public sealed class SagaDbFixture : IAsyncLifetime
{
    private const string TestDatabase = "imagestudio_saga_test";

    private static string Host => Environment.GetEnvironmentVariable("PGHOST") ?? "localhost";
    private static string Port => Environment.GetEnvironmentVariable("PGPORT") ?? "5432";

    private static string AdminConnStr =>
        $"Host={Host};Port={Port};Database=postgres;Username=postgres;Password=postgres";

    public static string ConnectionString =>
        $"Host={Host};Port={Port};Database={TestDatabase};Username=postgres;Password=postgres";

    public async Task InitializeAsync()
    {
        await using var conn = new NpgsqlConnection(AdminConnStr);
        await conn.OpenAsync();
        await Exec(conn, $"""
            SELECT pg_terminate_backend(pid) FROM pg_stat_activity
            WHERE datname = '{TestDatabase}' AND pid <> pg_backend_pid();
            """);
        await Exec(conn, $"DROP DATABASE IF EXISTS {TestDatabase}");
        await Exec(conn, $"CREATE DATABASE {TestDatabase}");

        // Both contexts share one database; each owns its own Postgres schema.
        await using (var wallet = CreateWalletContext())
            await wallet.Database.MigrateAsync();
        await using (var manager = CreateManagerContext())
            await manager.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await using var conn = new NpgsqlConnection(AdminConnStr);
        await conn.OpenAsync();
        await Exec(conn, $"""
            SELECT pg_terminate_backend(pid) FROM pg_stat_activity
            WHERE datname = '{TestDatabase}' AND pid <> pg_backend_pid();
            """);
        await Exec(conn, $"DROP DATABASE IF EXISTS {TestDatabase}");
    }

    private static async Task Exec(NpgsqlConnection conn, string sql)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync();
    }

    internal WalletDbContext CreateWalletContext() =>
        new(new DbContextOptionsBuilder<WalletDbContext>().UseNpgsql(ConnectionString).Options);

    internal GenerationManagerDbContext CreateManagerContext() =>
        new(new DbContextOptionsBuilder<GenerationManagerDbContext>().UseNpgsql(ConnectionString).Options);

    // A fresh service (and DbContext) per call, mirroring the scoped lifetime each
    // HTTP request / webhook delivery gets in production.
    internal IWalletService CreateWalletService() =>
        new WalletService(CreateWalletContext(), new NullPaymentGateway());

    private sealed class NullPaymentGateway : IPaymentGateway
    {
        public Task<string> CreateCheckoutSessionAsync(Guid userId, string packageId) => throw new NotSupportedException();
        public bool VerifyWebhookSignature(string payload, string signature) => throw new NotSupportedException();
        public CheckoutCompletedEvent? ParseCheckoutCompleted(string payload) => throw new NotSupportedException();
    }
}

[CollectionDefinition("saga-db")]
public sealed class SagaDbCollection : ICollectionFixture<SagaDbFixture> { }
