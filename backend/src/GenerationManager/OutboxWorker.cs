using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GenerationManager;

/// <summary>
/// Periodically drains the settlement outbox. Mirrors <see cref="ReconciliationWorker"/>:
/// a singleton that opens a scope per tick so the scoped dispatcher (and its
/// DbContext) is fresh each pass.
///
/// Polls at <see cref="MinInterval"/> while there's real work, so settlement stays
/// fast right after a webhook lands; backs off toward <see cref="MaxInterval"/>
/// during idle stretches so an app with no traffic isn't hitting the DB every 5s
/// forever (that's most of this app's uptime, and it adds up on a metered DB).
/// </summary>
internal sealed class OutboxWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxWorker> logger) : BackgroundService
{
    private static readonly TimeSpan MinInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan MaxInterval = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = MinInterval;
        using var timer = new PeriodicTimer(interval);
        do
        {
            var foundWork = false;
            try
            {
                using var scope = scopeFactory.CreateScope();
                var dispatcher = scope.ServiceProvider.GetRequiredService<IOutboxDispatcher>();
                foundWork = await dispatcher.DispatchPendingAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Outbox dispatch pass failed.");
            }

            var next = foundWork ? MinInterval : Min(interval * 2, MaxInterval);
            if (next != interval)
            {
                interval = next;
                timer.Period = interval;
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private static TimeSpan Min(TimeSpan a, TimeSpan b) => a < b ? a : b;
}
