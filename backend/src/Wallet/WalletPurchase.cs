namespace Wallet;

internal sealed class WalletPurchase
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string PackageNameId { get; set; } = null!;
    public decimal DollarAmount { get; set; }
    public long PebbleAmount { get; set; }
    public string ExternalPaymentId { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    public WalletAccount? Wallet { get; set; }
}
