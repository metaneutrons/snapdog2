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
public partial class ZoneGroupingBackgroundService : BackgroundService
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
        LogZoneGroupingServiceStarting(_config.ZoneGroupingCheckIntervalMs);

        // Wait for services to be ready
        LogWaitingForServicesToBeReady();
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        LogStartingPeriodicZoneGroupingChecks();

        // Start periodic checks
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                LogStartingPeriodicZoneGroupingCheck();
                await PerformPeriodicCheckAsync(stoppingToken);
                LogPeriodicZoneGroupingCheckCompleted(_config.ZoneGroupingCheckIntervalMs);
                await Task.Delay(_reconciliationInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                LogZoneGroupingMonitoringCancelled();
                break;
            }
            catch (Exception ex)
            {
                _metrics.RecordError("exception", "periodic_check");
                LogExceptionInPeriodicCheckLoop(ex);
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

            LogCheckingZoneGroupingConfiguration();

            // Simple periodic check - just ensure zones are properly configured
            var result = await zoneGroupingService.EnsureZoneGroupingAsync(cancellationToken);

            if (result.IsSuccess)
            {
                _metrics.RecordReconciliation(stopwatch.Elapsed.TotalSeconds, true);
                LogZoneGroupingCheckCompletedSuccessfully();
            }
            else
            {
                _metrics.RecordReconciliation(stopwatch.Elapsed.TotalSeconds, false, errorType: "ensure_failed");
                LogPeriodicZoneGroupingCheckFailed(result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _metrics.RecordError("exception", "periodic_check");
            LogExceptionDuringPeriodicZoneGroupingCheck(ex);
        }
    }

    // LoggerMessage methods for high-performance logging
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "🎵 Zone grouping background service starting with {Interval}ms interval..."
    )]
    private partial void LogZoneGroupingServiceStarting(int Interval);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Information,
        Message = "⏳ Waiting 5 seconds for services to be ready..."
    )]
    private partial void LogWaitingForServicesToBeReady();

    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "✅ Starting periodic zone grouping checks...")]
    private partial void LogStartingPeriodicZoneGroupingChecks();

    [LoggerMessage(EventId = 4, Level = LogLevel.Debug, Message = "🔄 Starting periodic zone grouping check...")]
    private partial void LogStartingPeriodicZoneGroupingCheck();

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Debug,
        Message = "✅ Periodic zone grouping check completed, waiting {Interval}ms..."
    )]
    private partial void LogPeriodicZoneGroupingCheckCompleted(int Interval);

    [LoggerMessage(EventId = 6, Level = LogLevel.Information, Message = "🛑 Zone grouping monitoring cancelled")]
    private partial void LogZoneGroupingMonitoringCancelled();

    [LoggerMessage(EventId = 7, Level = LogLevel.Error, Message = "💥 Exception in periodic check loop")]
    private partial void LogExceptionInPeriodicCheckLoop(Exception ex);

    [LoggerMessage(EventId = 8, Level = LogLevel.Debug, Message = "🔍 Checking zone grouping configuration...")]
    private partial void LogCheckingZoneGroupingConfiguration();

    [LoggerMessage(EventId = 9, Level = LogLevel.Debug, Message = "✅ Zone grouping check completed successfully")]
    private partial void LogZoneGroupingCheckCompletedSuccessfully();

    [LoggerMessage(EventId = 10, Level = LogLevel.Warning, Message = "⚠️ Periodic zone grouping check failed: {Error}")]
    private partial void LogPeriodicZoneGroupingCheckFailed(string? Error);

    [LoggerMessage(EventId = 11, Level = LogLevel.Error, Message = "💥 Exception during periodic zone grouping check")]
    private partial void LogExceptionDuringPeriodicZoneGroupingCheck(Exception ex);
}
