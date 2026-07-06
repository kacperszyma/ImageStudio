using System.Diagnostics;

namespace Wallet;

public static class WalletActivitySource
{
    public const string Name = "ImageStudio.Wallet";
    public static readonly ActivitySource Instance = new(Name);
}
