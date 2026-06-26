using Microsoft.EntityFrameworkCore;

namespace Wallet;

internal sealed class WalletDbContext(DbContextOptions<WalletDbContext> options)
    : DbContext(options)
{
    public DbSet<WalletAccount> Accounts => Set<WalletAccount>();
    public DbSet<WalletHold> Holds => Set<WalletHold>();
    public DbSet<WalletLedger> Ledger => Set<WalletLedger>();
    public DbSet<WalletPurchase> Purchases => Set<WalletPurchase>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.HasDefaultSchema("wallet");
        b.Entity<WalletAccount>(e =>
        {
            e.ToTable("accounts", t => t.HasCheckConstraint("ck_balance_non_negative", "\"Balance\" >= 0"));
            e.HasKey(x => x.UserId);
        });
        b.Entity<WalletHold>(e =>
        {
            e.ToTable("wallet_holds", t => t.HasCheckConstraint("ck_holds_amount_non_negative", "\"Amount\" >= 0"));
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Wallet).WithMany().HasForeignKey(x => x.WalletId).HasPrincipalKey(x => x.UserId);
            e.HasIndex(x => new { x.WalletId, x.Status }).HasDatabaseName("idx_holds_wallet");
            e.HasIndex(x => x.PurchaseId).HasDatabaseName("idx_holds_purchase");
            e.HasIndex(x => x.IdempotencyKey).IsUnique().HasDatabaseName("uq_holds_idempotency_key");
        });
        b.Entity<WalletLedger>(e =>
        {
            e.ToTable("ledger_entries", t => t.HasCheckConstraint("ck_ledger_amount_non_negative", "\"Amount\" >= 0"));
            e.HasKey(x => x.Id);
            e.Property(x => x.Type).HasConversion<string>();
            e.HasOne(x => x.Wallet).WithMany().HasForeignKey(x => x.WalletId).HasPrincipalKey(x => x.UserId);
            e.HasOne(x => x.Purchase).WithMany().HasForeignKey(x => x.PurchaseId).IsRequired(false);
            e.HasIndex(x => new { x.WalletId, x.CreatedAt }).HasDatabaseName("idx_ledger_wallet");
            e.HasIndex(x => x.IdempotencyKey).IsUnique().HasDatabaseName("uq_ledger_idempotency_key");
        });
        b.Entity<WalletPurchase>(e =>
        {
            e.ToTable("purchases");
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Wallet).WithMany().HasForeignKey(x => x.UserId).HasPrincipalKey(x => x.UserId);
            e.HasIndex(x => x.ExternalPaymentId).IsUnique().HasDatabaseName("uq_purchases_external_payment_id");
            e.HasIndex(x => x.UserId).HasDatabaseName("idx_purchases_user");
        });
    }
}