namespace GenerationManager.Contracts;

public interface IGenerationManager
{
    /// <summary>
    /// Freezes funds, enqueues the generation with the provider and returns
    /// immediately with the job id. The result arrives later via the webhook
    /// path (<see cref="CompleteJobAsync"/>), not from this call.
    /// </summary>
    Task<Guid> GenerateAsync(Guid userId, string modelSlug, string prompt);

    /// <summary>
    /// Settles an in-flight job once the provider reports back: charges or
    /// refunds the frozen funds, records the outcome and notifies the user.
    /// Idempotent — safe to call again for the provider's webhook retries.
    /// </summary>
    Task CompleteJobAsync(string requestId, string? imageUrl, bool success);

    /// <summary>Returns the job's details only if it belongs to <paramref name="userId"/>; otherwise null.</summary>
    Task<GenerationDetailDto?> GetDetailsAsync(Guid jobId, Guid userId);
    Task<IReadOnlyList<GenerationHistoryItem>> GetHistoryAsync(Guid userId);
}

public record GenerationHistoryItem(
    Guid JobId,
    string? ModelSlug,
    string? Prompt,
    string? ImageUrl,
    long? CreditCost,
    string Status,
    DateTime CreatedAt);

public record GenerationDetailDto(
    Guid JobId,
    string? ModelSlug,
    string? Prompt,
    string? ImageUrl,
    long? CreditCost,
    string Status,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    TimeSpan? Duration,
    long? BalanceBefore,
    long? BalanceAfter);
