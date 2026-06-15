using Microsoft.EntityFrameworkCore;

namespace Wallet;

internal sealed class WalletDbContext(DbContextOptions<WalletDbContext> options)
    : DbContext(options)
{
    public DbSet<WalletAccount> Accounts => Set<WalletAccount>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.HasDefaultSchema("wallet");
        b.Entity<WalletAccount>(e =>
        {
            e.ToTable("accounts");
            e.HasKey(x => x.UserId);
        });
    }
}