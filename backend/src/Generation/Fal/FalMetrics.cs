using System.Diagnostics.Metrics;

namespace Generation.Fal;

/// <summary>
/// Instruments for calls to the Fal provider: outbound enqueue latency/errors
/// and inbound webhook authenticity. One instance for the process lifetime.
/// </summary>
public sealed class FalMetrics
{
    public const string MeterName = "ImageStudio.Generation";

    private readonly Histogram<double> _enqueueDuration;
    private readonly Counter<long> _enqueueErrors;
    private readonly Counter<long> _webhookVerificationFailed;

    public FalMetrics()
    {
        var meter = new Meter(MeterName);
        _enqueueDuration = meter.CreateHistogram<double>("fal.enqueue.duration", unit: "s");
        _enqueueErrors = meter.CreateCounter<long>("fal.enqueue.errors");
        _webhookVerificationFailed = meter.CreateCounter<long>("fal.webhook.verification_failed");
    }

    public void EnqueueSucceeded(TimeSpan duration) => _enqueueDuration.Record(duration.TotalSeconds);

    public void EnqueueFailed(TimeSpan duration)
    {
        _enqueueDuration.Record(duration.TotalSeconds, new KeyValuePair<string, object?>("error", true));
        _enqueueErrors.Add(1);
    }

    public void WebhookVerificationFailed() => _webhookVerificationFailed.Add(1);
}
