using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Abstractions;

namespace SnapDog2.Services;

/// <summary>
/// Continuous background service for automatic zone-based client grouping.
/// Monitors Snapcast server for client changes and maintains proper zone grouping automatically.
/// </summary>
public class ZoneGroupingBackgroundService : BackgroundService
{
    private readonly ILogger<ZoneGroupingBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public ZoneGroupingBackgroundService(
        ILogger<ZoneGroupingBackgroundService> logger,
        IServiceProvider serviceProvider
    )
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🎵 Starting continuous zone grouping service");

        // Initial startup reconciliation
        await PerformInitialReconciliation(stoppingToken);

        // Continuous monitoring loop
        await MonitorAndMaintainGrouping(stoppingToken);
    }

    private async Task PerformInitialReconciliation(CancellationToken cancellationToken)
    {
        _logger.LogInformation("🔧 Performing initial zone grouping reconciliation");

        try
        {
            // Wait for Snapcast service to be ready
            await WaitForSnapcastServiceAsync(cancellationToken);

            using var scope = _serviceProvider.CreateScope();
            var zoneGroupingService = scope.ServiceProvider.GetRequiredService<IZoneGroupingService>();

            // Perform full reconciliation
            var reconciliationResult = await zoneGroupingService.ReconcileAllZoneGroupingsAsync(cancellationToken);

            if (reconciliationResult.IsSuccess)
            {
                var result = reconciliationResult.Value!;
                _logger.LogInformation(
                    "✅ Initial reconciliation completed: {ZonesReconciled} zones, {ClientsMoved} clients moved in {Duration}ms",
                    result.ZonesReconciled,
                    result.ClientsMoved,
                    result.Duration.TotalMilliseconds
                );

                // Synchronize client names
                var nameSync = await zoneGroupingService.SynchronizeClientNamesAsync(cancellationToken);
                if (nameSync.IsSuccess)
                {
                    var nameSyncResult = nameSync.Value!;
                    _logger.LogInformation(
                        "✅ Client names synchronized: {Updated} updated, {AlreadyCorrect} already correct",
                        nameSyncResult.UpdatedClients,
                        nameSyncResult.AlreadyCorrect
                    );
                }
            }
            else
            {
                _logger.LogError("❌ Initial reconciliation failed: {Error}", reconciliationResult.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Error during initial reconciliation");
        }
    }

    private async Task MonitorAndMaintainGrouping(CancellationToken cancellationToken)
    {
        _logger.LogInformation("👁️ Starting continuous zone grouping monitoring");

        const int monitoringIntervalMs = 30000; // 30 seconds - reasonable for automatic maintenance

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(monitoringIntervalMs, cancellationToken);

                using var scope = _serviceProvider.CreateScope();
                var zoneGroupingService = scope.ServiceProvider.GetRequiredService<IZoneGroupingService>();

                // Quick validation check
                var validationResult = await zoneGroupingService.ValidateGroupingConsistencyAsync(cancellationToken);

                if (!validationResult.IsSuccess)
                {
                    _logger.LogInformation("🔄 Detected grouping inconsistency, performing automatic correction");

                    // Perform reconciliation to fix issues
                    var reconciliationResult = await zoneGroupingService.ReconcileAllZoneGroupingsAsync(
                        cancellationToken
                    );

                    if (reconciliationResult.IsSuccess)
                    {
                        var result = reconciliationResult.Value!;
                        if (result.ClientsMoved > 0)
                        {
                            _logger.LogInformation(
                                "✅ Automatic correction completed: {ClientsMoved} clients moved",
                                result.ClientsMoved
                            );
                        }
                    }
                    else
                    {
                        _logger.LogWarning(
                            "⚠️ Automatic correction failed: {Error}",
                            reconciliationResult.ErrorMessage
                        );
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when service is stopping
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error during continuous monitoring, will retry");
            }
        }

        _logger.LogInformation("🛑 Zone grouping monitoring stopped");
    }

    private async Task WaitForSnapcastServiceAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("⏳ Waiting for Snapcast service...");

        const int maxWaitTimeMs = 30000; // 30 seconds - reduced timeout
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
}
