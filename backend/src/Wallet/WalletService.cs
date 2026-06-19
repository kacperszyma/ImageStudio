using Microsoft.EntityFrameworkCore;
using Wallet.Contracts;

namespace Wallet;

internal sealed class WalletService(WalletDbContext db) : IWalletService
{
    public async Task<long> GetBalanceAsync(Guid userId)
    {
        var account = await db.Accounts.FirstOrDefaultAsync(a => a.UserId == userId);
        return account?.Balance ?? 0;
    }

    public async Task EnsureAccountAsync(Guid userId)
    {
        if (!await db.Accounts.AnyAsync(a => a.UserId == userId))
        {
            db.Accounts.Add(new WalletAccount { UserId = userId, Balance = WalletAccount.StartingBalance });
            await db.SaveChangesAsync();
        }
    }
    public Task FreezeFundsAsync(Guid userId, long amount, Guid generationJobId) => throw new NotImplementedException();
    public Task UnfreezeAsync(Guid generationJobId) => throw new NotImplementedException();
    public Task ChargeFrozenAsync(Guid generationJobId) => throw new NotImplementedException();
    public Task TopUpAsync(Guid userId, long amount) => throw new NotImplementedException();
    public Task<IReadOnlyList<TransactionDto>> GetTransactionsAsync(Guid userId) => throw new NotImplementedException();
}
