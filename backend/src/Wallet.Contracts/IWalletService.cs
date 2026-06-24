namespace Wallet.Contracts;

public interface IWalletService
{
    Task FreezeFundsAsync(Guid userId, long amount, Guid generationJobId, string idempotencyKey);
    Task UnfreezeAsync(Guid generationJobId);
    Task ChargeFrozenAsync(Guid generationJobId);
    Task EnsureAccountAsync(Guid userId);
    Task<long> GetBalanceAsync(Guid userId);
    Task TopUpAsync(Guid userId, long amount, string idempotencyKey);
    Task<IReadOnlyList<TransactionDto>> GetTransactionsAsync(Guid userId);
    Task<GenerationWalletDetails?> GetGenerationWalletDetailsAsync(Guid jobId);
    Task<TransactionDetailDto?> GetTransactionAsync(Guid transactionId);
}

public record TransactionDto(Guid Id, Guid UserId, long Amount, TransactionType Type, DateTime CreatedAt);
public record TransactionDetailDto(Guid Id, Guid UserId, long Amount, TransactionType Type, DateTime CreatedAt, Guid? GenerationJobId);
public record GenerationWalletDetails(long BalanceBefore, long BalanceAfter);

public enum TransactionType { TopUp, Freeze, Charge, Unfreeze }

public class InsufficientFundsException(Guid userId, long required, long available)
    : Exception($"User {userId} has {available} credits, needs {required}.");
