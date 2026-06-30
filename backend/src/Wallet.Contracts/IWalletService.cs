namespace Wallet.Contracts;

public interface IWalletService
{
    Task FreezeFundsAsync(Guid userId, long amount, Guid generationJobId, string idempotencyKey);
    Task UnfreezeAsync(Guid generationJobId);
    Task ChargeFrozenAsync(Guid generationJobId);
    Task EnsureAccountAsync(Guid userId);
    Task<long> GetBalanceAsync(Guid userId);
    Task TopUpAsync(Guid userId, long amount, string idempotencyKey, Guid? purchaseId = null);
    List<PackageOfferDto> GetPackages();
    Task<IReadOnlyList<TransactionDto>> GetSpendingHistoryAsync(Guid userId);
    Task<IReadOnlyList<PurchaseDto>> GetPurchasesAsync(Guid userId);
    Task<GenerationWalletDetails?> GetGenerationWalletDetailsAsync(Guid jobId);
    Task<TransactionDetailDto?> GetTransactionAsync(Guid transactionId, Guid userId);
    bool VerifyPaymentWebhook(string payload, string signature);
    Task<string> CreateCheckoutAsync(Guid userId, string packageId);
    Task ProcessPaymentWebhookAsync(string payload, string signature);
    Task RedeemSessionAsync(string sessionId, Guid userId);
}

public record PackageOfferDto(String NameId, decimal DollarPrice, decimal PebbleAmount, decimal DiscountAmount);

public record TransactionDto(Guid Id, Guid UserId, long Amount, TransactionType Type, DateTime CreatedAt);
public record TransactionDetailDto(Guid Id, Guid UserId, long Amount, TransactionType Type, DateTime CreatedAt, Guid? GenerationJobId);
public record PurchaseDto(Guid Id, string PackageNameId, decimal DollarAmount, long PebbleAmount, DateTime CreatedAt);
public record GenerationWalletDetails(long BalanceBefore, long BalanceAfter);

public enum TransactionType { TopUp, Freeze, Charge, Unfreeze }

public class InsufficientFundsException(Guid userId, long required, long available)
    : Exception($"User {userId} has {available} credits, needs {required}.");
