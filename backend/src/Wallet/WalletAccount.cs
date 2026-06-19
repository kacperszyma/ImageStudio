namespace Wallet;

internal sealed class WalletAccount
{
    public static long StartingBalance = 200;
    public Guid UserId { get; set; }
    public long Balance { get; set; }
}