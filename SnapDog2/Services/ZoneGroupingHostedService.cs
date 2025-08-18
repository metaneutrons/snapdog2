using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Models;
using SnapDog2.Infrastructure.Metrics;

namespace SnapDog2.Services;

/// <summary>
/// Background service that continuously monitors and maintains zone-based client grouping.
/// Performs automatic reconciliation every 30 seconds and exposes metrics via OpenTelemetry.
/// </summary>
public class ZoneGroupingBackgroundService : BackgroundService
{
    private readonly ILogger<ZoneGroupingBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly ZoneGroupingMetrics _metrics;
    private readonly TimeSpan _reconciliationInterval = TimeSpan.FromSeconds(30);

    public ZoneGroupingBackgroundService(
        ILogger<ZoneGroupingBackgroundService> logger,
        IServiceProvider serviceProvider,
        ZoneGroupingMetrics metrics
    )
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🎵 Zone grouping background service starting...");

        // Perform initial reconciliation
        await PerformInitialReconciliationAsync(stoppingToken);

        // Start continuous monitoring loop
        await StartContinuousMonitoringAsync(stoppingToken);
    }

    private async Task PerformInitialReconciliationAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("🔄 Performing initial zone grouping reconciliation...");

        var stopwatch = Stopwatch.StartNew();
        try
        {
            // Wait for Snapcast service to be ready
            await WaitForSnapcastServiceAsync(cancellationToken);

            using var scope = _serviceProvider.CreateScope();
            var zoneGroupingService = scope.ServiceProvider.GetRequiredService<IZoneGroupingService>();

            var reconciliationResult = await zoneGroupingService.ReconcileAllZoneGroupingsAsync(cancellationToken);
            var statusResult = await zoneGroupingService.GetZoneGroupingStatusAsync(cancellationToken);

            if (reconciliationResult.IsSuccess && statusResult.IsSuccess)
            {
                var clientUpdates = reconciliationResult.Value?.ClientsMoved ?? 0;
                _metrics.RecordReconciliation(stopwatch.Elapsed.TotalSeconds, true, clientUpdates);
                _metrics.UpdateZoneStatus(statusResult.Value!);

                _logger.LogInformation(
                    "✅ Initial reconciliation completed successfully in {Duration:F2}s. Updated {ClientCount} clients.",
                    stopwatch.Elapsed.TotalSeconds,
                    clientUpdates
                );
            }
            else
            {
                var errorMessage = reconciliationResult.ErrorMessage ?? statusResult.ErrorMessage ?? "Unknown error";
                _metrics.RecordReconciliation(
                    stopwatch.Elapsed.TotalSeconds,
                    false,
                    errorType: "reconciliation_failed"
                );
                _metrics.RecordError("reconciliation_failed", "initial_reconciliation");

                _logger.LogError("❌ Initial reconciliation failed: {Error}", errorMessage);
            }
        }
        catch (Exception ex)
        {
            _metrics.RecordReconciliation(stopwatch.Elapsed.TotalSeconds, false, errorType: "exception");
            _metrics.RecordError("exception", "initial_reconciliation");

            _logger.LogError(ex, "💥 Exception during initial reconciliation");
            throw;
        }
    }

    private async Task StartContinuousMonitoringAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "🔄 Starting continuous zone grouping monitoring (interval: {Interval}s)",
            _reconciliationInterval.TotalSeconds
        );

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_reconciliationInterval, cancellationToken);
                await PerformPeriodicReconciliationAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("🛑 Zone grouping monitoring cancelled");
                break;
            }
            catch (Exception ex)
            {
                _metrics.RecordError("exception", "continuous_monitoring");
                _logger.LogError(ex, "💥 Exception in continuous monitoring loop");

                // Continue monitoring despite errors
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }
    }

    private async Task PerformPeriodicReconciliationAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var zoneGroupingService = scope.ServiceProvider.GetRequiredService<IZoneGroupingService>();

            // Check current status first
            var statusResult = await zoneGroupingService.GetZoneGroupingStatusAsync(cancellationToken);
            if (!statusResult.IsSuccess)
            {
                _metrics.RecordError("status_check_failed", "periodic_reconciliation");
                _logger.LogWarning("⚠️ Failed to get zone grouping status: {Error}", statusResult.ErrorMessage);
                return;
            }

            var status = statusResult.Value!;
            _metrics.UpdateZoneStatus(status);

            // Only perform reconciliation if there are issues
            if (status.OverallHealth == ZoneGroupingHealth.Healthy)
            {
                _logger.LogDebug("✅ Zone grouping is healthy, no reconciliation needed");
                _metrics.RecordReconciliation(stopwatch.Elapsed.TotalSeconds, true);
                return;
            }

            _logger.LogInformation(
                "🔧 Zone grouping health is {Health}, performing reconciliation...",
                status.OverallHealth
            );

            var reconciliationResult = await zoneGroupingService.ReconcileAllZoneGroupingsAsync(cancellationToken);

            if (reconciliationResult.IsSuccess)
            {
                var clientUpdates = reconciliationResult.Value?.ClientsMoved ?? 0;
                _metrics.RecordReconciliation(stopwatch.Elapsed.TotalSeconds, true, clientUpdates);

                if (clientUpdates > 0)
                {
                    _logger.LogInformation(
                        "✅ Automatic reconciliation completed in {Duration:F2}s. Updated {ClientCount} clients.",
                        stopwatch.Elapsed.TotalSeconds,
                        clientUpdates
                    );
                }
                else
                {
                    _logger.LogDebug("✅ Reconciliation completed, no updates needed");
                }
            }
            else
            {
                _metrics.RecordReconciliation(
                    stopwatch.Elapsed.TotalSeconds,
                    false,
                    errorType: "reconciliation_failed"
                );
                _metrics.RecordError("reconciliation_failed", "periodic_reconciliation");

                _logger.LogWarning("⚠️ Periodic reconciliation failed: {Error}", reconciliationResult.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _metrics.RecordReconciliation(stopwatch.Elapsed.TotalSeconds, false, errorType: "exception");
            _metrics.RecordError("exception", "periodic_reconciliation");

            _logger.LogError(ex, "💥 Exception during periodic reconciliation");
        }
    }

    private async Task WaitForSnapcastServiceAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("⏳ Waiting for Snapcast service...");

        const int maxWaitTimeMs = 30000; // 30 seconds
        const int checkIntervalMs = 1000; // 1 second
        var elapsed = 0;

        while (elapsed < maxWaitTimeMs && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var snapcastService = scope.ServiceProvider.GetRequiredService<ISnapcastService>();

                var statusResult = await snapcastService.GetServerStatusAsync(cancellationToken);
                if (statusResult.IsSuccess)
                {
                    _logger.LogInformation("🔄 Snapcast service ready");
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Snapcast service not ready, retrying...");
            }

            await Task.Delay(checkIntervalMs, cancellationToken);
            elapsed += checkIntervalMs;
        }

        _logger.LogWarning("⏱️ Snapcast service not ready after {TimeoutMs}ms, proceeding anyway", maxWaitTimeMs);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("🛑 Zone grouping background service stopping...");
        await base.StopAsync(cancellationToken);
        _logger.LogInformation("✅ Zone grouping background service stopped");
    }
}
