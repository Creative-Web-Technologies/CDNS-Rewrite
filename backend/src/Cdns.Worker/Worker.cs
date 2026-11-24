using Cdns.Application.Interfaces;
using Cdns.Domain.Entities.SystemInfrastructureAuditing;
using Cdns.Infrastructure.Persistence;
using Cdns.Infrastructure.Sync;
using Microsoft.EntityFrameworkCore;

namespace Cdns.Worker;

public class Worker(IServiceScopeFactory scopeFactory, ILogger<Worker> logger) : BackgroundService
{
    // Identifies this sync job's own row in task_schedule_app_details, so its watermark
    // (LastRunDateTime) is tracked independently of any other scheduled job that might
    // use the same table later. Fixed, arbitrary GUID generated once — never change this
    // value once it's live, or the watermark history for this job effectively resets.
    private static readonly Guid AsycudaSyncAppId = new("3f2a9c1e-6b7d-4e3a-9f0a-5d8c4b1e2a77");
    private const string AppName = "AsycudaCusDecSync";

    private static readonly TimeSpan SyncInterval = TimeSpan.FromMinutes(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await RunSyncCycleAsync(stoppingToken);

            try
            {
                await Task.Delay(SyncInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Shutting down — exit the loop cleanly.
            }
        }
    }

    private async Task RunSyncCycleAsync(CancellationToken stoppingToken)
    {
        // Worker itself is a long-lived singleton, but CdnsDbContext etc. are scoped
        // services, so each cycle gets its own scope.
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CdnsDbContext>();
        var asycudaSyncService = scope.ServiceProvider.GetRequiredService<IAsycudaSyncService>();
        var processor = scope.ServiceProvider.GetRequiredService<CusDecSyncProcessor>();
        var itemProcessor = scope.ServiceProvider.GetRequiredService<CusDecItemSyncProcessor>();

        var appDetail = await db.TaskScheduleAppDetails
            .FirstOrDefaultAsync(a => a.AppId == AsycudaSyncAppId, stoppingToken);

        if (appDetail is null)
        {
            appDetail = new TaskScheduleAppDetail
            {
                AppId = AsycudaSyncAppId,
                AppName = AppName,
                LastRunDateTime = null,
                IsSuccess = false,
            };
            db.TaskScheduleAppDetails.Add(appDetail);
        }

        // Capture the run's start time up front — this becomes the new watermark on
        // success, same idea as legacy's SetDate(). If there's never been a successful
        // run yet, fall back to 26 hours ago (matches legacy's own fallback window).
        var runStartTime = DateTimeOffset.UtcNow;
        var since = appDetail.LastRunDateTime?.UtcDateTime ?? runStartTime.UtcDateTime.AddHours(-26);

        try
        {
            logger.LogInformation("CusDec sync starting. Window: {Since} to {RunStartTime}", since, runStartTime);

            var rows = await asycudaSyncService.GetChangedCusDecsAsync(since, stoppingToken);
            await processor.ProcessAsync(rows, stoppingToken);

            var itemRows = await asycudaSyncService.GetChangedCusDecItemsAsync(since, stoppingToken);
            await itemProcessor.ProcessAsync(itemRows, stoppingToken);

            // Only advance the watermark when the whole cycle succeeds — this is the
            // legacy bug we're deliberately fixing: a failed run must not lose its window.
            appDetail.LastRunDateTime = runStartTime;
            appDetail.IsSuccess = true;
            appDetail.Errors = null;

            logger.LogInformation("CusDec sync finished. {RowCount} rows processed.", rows.Count);
            logger.LogInformation("CusDecItem sync finished. {RowCount} rows processed.", itemRows.Count);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Shutting down mid-cycle — not a sync failure, don't record it as one.
            return;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "CusDec sync failed. Watermark not advanced; next run retries the same window.");

            appDetail.IsSuccess = false;
            appDetail.Errors = ex.ToString();
            // LastRunDateTime intentionally left untouched on failure.
        }

        await db.SaveChangesAsync(stoppingToken);
    }
}