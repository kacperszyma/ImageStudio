namespace GenerationManager;

/// <summary>
/// One durable intent to perform the side effects of settling a job (charge or
/// refund funds, record the image, notify the user). Written in the SAME manager
/// transaction that flips the job's status, so a settlement decision and the work
/// it implies can never diverge. A dispatcher drains these, retrying until each
/// sticks — the side effects must therefore be idempotent.
/// </summary>
internal sealed class OutboxMessage
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }

    /// <summary>JSON; shape depends on <see cref="JobId"/>'s settlement (see SettlePayload).</summary>
    public string Payload { get; set; } = "";

    public DateTime CreatedAt { get; set; }

    /// <summary>Null until the side effects have been applied; set marks it done.</summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>Dispatch attempts so far; lets a worker spot a poison message.</summary>
    public int Attempts { get; set; }

    /// <summary>
    /// W3C traceparent of whatever caused this to be enqueued (a webhook request,
    /// or a reconciliation sweep). The dispatcher runs on a background timer with
    /// no request of its own, so this is stored rather than propagated live — it
    /// lets the dispatch span link back to the originating trace across the gap.
    /// </summary>
    public string? TraceParent { get; set; }
}

/// <summary>The settlement outcome the dispatcher must apply for a job.</summary>
internal sealed record SettlePayload(bool Success, string? ImageUrl);
