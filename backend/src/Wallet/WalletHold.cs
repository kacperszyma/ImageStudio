namespace Wallet;

internal class WalletHold
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReleasedAt { get; set; }
    public long Amount { get; set; }
    public string Status { get; set; } = "";
    public string IdempotencyKey { get; set; } = "";
    public Guid PurchaseId { get; set; }
    public Guid WalletId { get; set; }

    public WalletAccount? Wallet { get; set; } = null;
}