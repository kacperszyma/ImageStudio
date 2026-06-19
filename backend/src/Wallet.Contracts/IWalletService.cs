namespace Wallet.Contracts;

public interface IWalletService
{
    Task FreezeFundsAsync(Guid userId, long amount, Guid generationJobId);
    Task UnfreezeAsync(Guid generationJobId);
    Task ChargeFrozenAsync(Guid generationJobId);
    Task EnsureAccountAsync(Guid userId);
    Task<long> GetBalanceAsync(Guid userId);
    Task TopUpAsync(Guid userId, long amount);
    Task<IReadOnlyList<TransactionDto>> GetTransactionsAsync(Guid userId);
}

public record TransactionDto(Guid Id, Guid UserId, long Amount, TransactionType Type, DateTime CreatedAt);

public enum TransactionType { TopUp, Freeze, Charge, Unfreeze }

public class InsufficientFundsException(Guid userId, long required, long available)
    : Exception($"User {userId} has {available} credits, needs {required}.");
