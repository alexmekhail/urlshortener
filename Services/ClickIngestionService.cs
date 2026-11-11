using UrlShortener.Models;

namespace UrlShortener.Services;

/// <summary>
/// Drains <see cref="IClickEventQueue"/> and batch-inserts clicks into Postgres,
/// so recording a click never adds latency to the redirect response.
/// </summary>
public class ClickIngestionService(
    IClickEventQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<ClickIngestionService> logger) : BackgroundService
{
    private const int MaxBatchSize = 100;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var batch = new List<ClickEvent>(MaxBatchSize);

        try
        {
            while (await queue.Reader.WaitToReadAsync(stoppingToken))
            {
                while (batch.Count < MaxBatchSize && queue.Reader.TryRead(out var clickEvent))
                    batch.Add(clickEvent);

                if (batch.Count > 0)
                {
                    await FlushAsync(batch, stoppingToken);
                    batch.Clear();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Shutting down — fall through to flush whatever remains below.
        }

        // Best-effort drain of anything left in the channel on shutdown.
        while (queue.Reader.TryRead(out var remaining))
            batch.Add(remaining);

        if (batch.Count > 0)
            await FlushAsync(batch, CancellationToken.None);
    }

    private async Task FlushAsync(List<ClickEvent> batch, CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<UrlShortenerContext>();

            db.Clicks.AddRange(batch.Select(e => new Click
            {
                UrlId = e.UrlId,
                ClickedAt = e.ClickedAt,
                ReferrerHost = e.ReferrerHost,
                Country = e.Country,
                DeviceType = e.DeviceType,
                Browser = e.Browser,
            }));

            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            // Click analytics are best-effort: never let an ingestion failure crash the app.
            logger.LogWarning(ex, "Failed to persist a batch of {Count} click events.", batch.Count);
        }
    }
}
