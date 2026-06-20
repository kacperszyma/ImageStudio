namespace Wallet;

internal class WalletAccount
{
    public static long StartingBalance = 200;

    public Guid UserId { get; set; }

    DateTime CreatedAt { get; set; }

    DateTime UpdatedAt { get; set; }
    public long Balance { get; set; }
    public long Frozen { get; set; }

    ICollection<WalletLedger>? LegerEntries { get; set; } = null;
    ICollection<WalletLedger>? FrozenFunds { get; set; } = null;
}