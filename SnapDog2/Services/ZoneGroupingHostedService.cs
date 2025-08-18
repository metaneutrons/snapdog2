using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Abstractions;

namespace SnapDog2.Services;

/// <summary>
/// Hosted service that ensures proper zone-based client grouping on application startup.
/// Replaces the legacy ClientZoneInitializationService with enterprise-grade zone grouping functionality.
/// </summary>
public class ZoneGroupingHostedService : IHostedService
{
    private readonly ILogger<ZoneGroupingHostedService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public ZoneGroupingHostedService(ILogger<ZoneGroupingHostedService> logger, IServiceProvider serviceProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("🎵 Starting zone-based client grouping initialization");

        try
        {
            // Wait for Snapcast service to be ready
            await WaitForSnapcastServiceAsync(cancellationToken);

            // Perform full zone grouping reconciliation
            using var scope = _serviceProvider.CreateScope();
            var zoneGroupingService = scope.ServiceProvider.GetRequiredService<IZoneGroupingService>();

            _logger.LogInformation("🔧 Performing initial zone grouping reconciliation");
            var reconciliationResult = await zoneGroupingService.ReconcileAllZoneGroupingsAsync(cancellationToken);

            if (reconciliationResult.IsSuccess)
            {
                var result = reconciliationResult.Value!;
                _logger.LogInformation(
                    "✅ Zone grouping initialization completed successfully: "
                        + "{ZonesReconciled} zones reconciled, {ClientsMoved} clients moved in {Duration}ms",
                    result.ZonesReconciled,
                    result.ClientsMoved,
                    result.Duration.TotalMilliseconds
                );

                if (result.Actions.Any())
                {
                    _logger.LogInformation("📋 Actions taken: {Actions}", string.Join(", ", result.Actions));
                }

                if (result.Errors.Any())
                {
                    _logger.LogWarning("⚠️ Errors during reconciliation: {Errors}", string.Join(", ", result.Errors));
                }
            }
            else
            {
                _logger.LogError("❌ Zone grouping reconciliation failed: {Error}", reconciliationResult.ErrorMessage);
            }

            // Validate final grouping state
            _logger.LogInformation("🔍 Validating final zone grouping consistency");
            var validationResult = await zoneGroupingService.ValidateGroupingConsistencyAsync(cancellationToken);

            if (validationResult.IsSuccess)
            {
                _logger.LogInformation("✅ Zone grouping consistency validation passed");
            }
            else
            {
                _logger.LogWarning(
                    "⚠️ Zone grouping consistency issues detected: {Issues}",
                    validationResult.ErrorMessage
                );
            }

            _logger.LogInformation("🎉 Zone grouping hosted service startup completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Critical error during zone grouping initialization");
            // Don't throw - this is not critical for application startup, but log as error
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("🛑 Zone grouping hosted service stopping");
        return Task.CompletedTask;
    }

    private async Task WaitForSnapcastServiceAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("⏳ Waiting for Snapcast service to be ready...");

        const int maxWaitTimeMs = 30000; // 30 seconds
        const int checkIntervalMs = 1000; // 1 second
        var elapsed = 0;

        while (elapsed < maxWaitTimeMs && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var snapcastService = scope.ServiceProvider.GetRequiredService<ISnapcastService>();

                // Try to get server status to check if service is ready
                var statusResult = await snapcastService.GetServerStatusAsync(cancellationToken);
                if (statusResult.IsSuccess)
                {
                    _logger.LogInformation("🔄 Snapcast service ready, proceeding with zone grouping");
                    return;
                }
            }
            catch
            {
                // Service not ready yet, continue waiting
            }

            await Task.Delay(checkIntervalMs, cancellationToken);
            elapsed += checkIntervalMs;
        }

        if (elapsed >= maxWaitTimeMs)
        {
            _logger.LogWarning(
                "⏱️ Timeout waiting for Snapcast service to be ready after {TimeoutMs}ms",
                maxWaitTimeMs
            );
        }
    }
}
