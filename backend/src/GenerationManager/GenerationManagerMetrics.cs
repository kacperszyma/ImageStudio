using System.Diagnostics.Metrics;

namespace GenerationManager;

/// <summary>
/// Instruments for the generation saga: job lifecycle and outbox health. One
/// instance for the process lifetime, shared by every scoped service that
/// raises these events.
/// </summary>
public sealed class GenerationManagerMetrics
{
    public const string MeterName = "ImageStudio.GenerationManager";

    private readonly Counter<long> _jobsCreated;
    private readonly Counter<long> _jobsSubmitFailed;
    private readonly Counter<long> _jobsSettled;
    private readonly Histogram<double> _jobDuration;
    private readonly Gauge<int> _outboxBacklog;
    private readonly Counter<long> _outboxDispatchFailed;
    private readonly Histogram<double> _outboxMessageLag;
    private readonly Counter<long> _imageSaveRetried;
    private readonly Counter<long> _imageSaveFailed;

    public GenerationManagerMetrics()
    {
        var meter = new Meter(MeterName);
        _jobsCreated = meter.CreateCounter<long>("generation.jobs.created");
        _jobsSubmitFailed = meter.CreateCounter<long>("generation.jobs.submit_failed");
        _jobsSettled = meter.CreateCounter<long>("generation.jobs.settled");
        _jobDuration = meter.CreateHistogram<double>("generation.job.duration", unit: "s");
        _outboxBacklog = meter.CreateGauge<int>("outbox.backlog");
        _outboxDispatchFailed = meter.CreateCounter<long>("outbox.dispatch_failed");
        _outboxMessageLag = meter.CreateHistogram<double>("outbox.message.lag", unit: "s");
        _imageSaveRetried = meter.CreateCounter<long>("generation.image_save.retried");
        _imageSaveFailed = meter.CreateCounter<long>("generation.image_save.failed");
    }

    public void JobCreated(string modelSlug) =>
        _jobsCreated.Add(1, new KeyValuePair<string, object?>("model_slug", modelSlug));

    public void JobSubmitFailed() => _jobsSubmitFailed.Add(1);

    // outcome: "completed" | "failed" | "expired"
    public void JobSettled(string outcome, TimeSpan duration)
    {
        var tag = new KeyValuePair<string, object?>("outcome", outcome);
        _jobsSettled.Add(1, tag);
        _jobDuration.Record(duration.TotalSeconds, tag);
    }

    public void OutboxBacklog(int pendingCount) => _outboxBacklog.Record(pendingCount);

    public void OutboxDispatchFailed() => _outboxDispatchFailed.Add(1);

    public void OutboxMessageDispatched(TimeSpan lag) => _outboxMessageLag.Record(lag.TotalSeconds);

    public void ImageSaveRetried() => _imageSaveRetried.Add(1);

    public void ImageSaveFailed() => _imageSaveFailed.Add(1);
}
