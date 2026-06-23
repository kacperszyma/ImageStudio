namespace GenerationManager;

/// <summary>
/// Durable state of one billing saga: funds are frozen when the job is created
/// Pending, then charged or refunded when the provider's webhook settles it.
/// The image artifact and history live in the Generation module, not here.
/// </summary>
internal sealed class GenerationJob
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    /// <summary>Provider request id; null until submitted. Webhook correlates on this.</summary>
    public string? FalRequestId { get; set; }

    public GenerationJobStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

internal enum GenerationJobStatus
{
    Pending,
    Completed,
    Failed,
    /// <summary>Timed out waiting for the provider's webhook; funds were released.</summary>
    Expired
}
