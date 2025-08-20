using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Configuration;
using SnapDog2.Infrastructure.Metrics;

namespace SnapDog2.Services;

/// <summary>
/// Simple background service that runs periodic zone grouping checks.
/// </summary>
public class ZoneGroupingBackgroundService : BackgroundService
{
    private readonly ILogger<ZoneGroupingBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly ZoneGroupingMetrics _metrics;
    private readonly SnapcastConfig _config;
    private readonly TimeSpan _reconciliationInterval;

    public ZoneGroupingBackgroundService(
        ILogger<ZoneGroupingBackgroundService> logger,
        IServiceProvider serviceProvider,
        ZoneGroupingMetrics metrics,
        IOptions<SnapcastConfig> config
    )
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        _reconciliationInterval = TimeSpan.FromMilliseconds(_config.ZoneGroupingCheckIntervalMs);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "🎵 Zone grouping background service starting with {Interval}ms interval...",
            _config.ZoneGroupingCheckIntervalMs
        );

        // Wait for services to be ready
        _logger.LogInformation("⏳ Waiting 5 seconds for services to be ready...");
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        _logger.LogInformation("✅ Starting periodic zone grouping checks...");

        // Start periodic checks
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogDebug("🔄 Starting periodic zone grouping check...");
                await PerformPeriodicCheckAsync(stoppingToken);
                _logger.LogDebug(
                    "✅ Periodic zone grouping check completed, waiting {Interval}ms...",
                    _config.ZoneGroupingCheckIntervalMs
                );
                await Task.Delay(_reconciliationInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("🛑 Zone grouping monitoring cancelled");
                break;
            }
            catch (Exception ex)
            {
                _metrics.RecordError("exception", "periodic_check");
                _logger.LogError(ex, "💥 Exception in periodic check loop");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task PerformPeriodicCheckAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var zoneGroupingService = scope.ServiceProvider.GetRequiredService<IZoneGroupingService>();

            _logger.LogDebug("🔍 Checking zone grouping configuration...");

            // Simple periodic check - just ensure zones are properly configured
            var result = await zoneGroupingService.EnsureZoneGroupingAsync(cancellationToken);

            if (result.IsSuccess)
            {
                _metrics.RecordReconciliation(stopwatch.Elapsed.TotalSeconds, true);
                _logger.LogDebug("✅ Zone grouping check completed successfully");
            }
            else
            {
                _metrics.RecordReconciliation(stopwatch.Elapsed.TotalSeconds, false, errorType: "ensure_failed");
                _logger.LogWarning("⚠️ Periodic zone grouping check failed: {Error}", result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _metrics.RecordError("exception", "periodic_check");
            _logger.LogError(ex, "💥 Exception during periodic zone grouping check");
        }
    }
}
