using Wallet.Contracts;

namespace Wallet;

internal class WalletLedger
{
    public Guid Id { get; set; }

    public DateTime CreatedAt { get; set; }

    DateTime UpdatedAt { get; set; }
    public long Amount { get; set; }

    public TransactionType Type { get; set; }

    public long BalanceAfter { get; set; }

    public string IdempotencyKey { get; set; } = "";

    public Guid WalletId { get; set; }

    public WalletAccount? Wallet { get; set; } = null;
}