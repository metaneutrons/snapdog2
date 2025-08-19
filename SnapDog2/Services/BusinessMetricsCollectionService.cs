namespace SnapDog2.Services;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Abstractions;
using SnapDog2.Infrastructure.Application;
using SnapDog2.Infrastructure.Metrics;

/// <summary>
/// Background service that periodically collects and updates business metrics.
/// Provides real-time insights into zone activity, client connections, and audio playback.
/// </summary>
public partial class BusinessMetricsCollectionService : BackgroundService
{
    private readonly ILogger<BusinessMetricsCollectionService> _logger;
    private readonly EnterpriseMetricsService _metricsService;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _collectionInterval = TimeSpan.FromSeconds(15); // Collect every 15 seconds

    public BusinessMetricsCollectionService(
        ILogger<BusinessMetricsCollectionService> logger,
        EnterpriseMetricsService metricsService,
        IServiceProvider serviceProvider
    )
    {
        _logger = logger;
        _metricsService = metricsService;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "BusinessMetricsCollectionService started with {CollectionInterval} interval",
            _collectionInterval
        );

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CollectBusinessMetricsAsync();
                await Task.Delay(_collectionInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while collecting business metrics");

                // Record the error in metrics
                _metricsService.RecordException(ex, "BusinessMetricsCollection");

                // Wait a bit before retrying
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }

        _logger.LogInformation("BusinessMetricsCollectionService stopped");
    }

    /// <summary>
    /// Collects current business metrics from various services.
    /// </summary>
    private async Task CollectBusinessMetricsAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();

            var zoneManager = scope.ServiceProvider.GetService<IZoneManager>();
            var clientManager = scope.ServiceProvider.GetService<IClientManager>();

            var businessMetrics = new BusinessMetricsState
            {
                ZonesTotal = await GetTotalZonesAsync(zoneManager),
                ZonesActive = await GetActiveZonesAsync(zoneManager),
                ClientsConnected = await GetConnectedClientsAsync(clientManager),
                TracksPlaying = await GetPlayingTracksAsync(zoneManager),
            };

            _metricsService.UpdateBusinessMetrics(businessMetrics);

            this.LogBusinessMetricsCollected(
                businessMetrics.ZonesTotal,
                businessMetrics.ZonesActive,
                businessMetrics.ClientsConnected,
                businessMetrics.TracksPlaying
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to collect business metrics");
        }
    }

    /// <summary>
    /// Gets the total number of configured zones.
    /// </summary>
    private static Task<int> GetTotalZonesAsync(IZoneManager? zoneManager)
    {
        try
        {
            if (zoneManager == null)
                return Task.FromResult(0);

            // In a real implementation, you would call a method to get all zones
            // For now, return a placeholder value
            // Example: var zones = await zoneManager.GetAllZonesAsync();
            // return zones.Count;

            return Task.FromResult(0); // Placeholder - implement based on your IZoneManager interface
        }
        catch
        {
            return Task.FromResult(0);
        }
    }

    /// <summary>
    /// Gets the number of currently active zones (zones with activity).
    /// </summary>
    private static Task<int> GetActiveZonesAsync(IZoneManager? zoneManager)
    {
        try
        {
            if (zoneManager == null)
                return Task.FromResult(0);

            // In a real implementation, you would:
            // 1. Get all zones
            // 2. Check which ones are active (have clients, are playing, etc.)
            // 3. Return the count

            return Task.FromResult(0); // Placeholder - implement based on your zone state logic
        }
        catch
        {
            return Task.FromResult(0);
        }
    }

    /// <summary>
    /// Gets the number of connected Snapcast clients.
    /// </summary>
    private static Task<int> GetConnectedClientsAsync(IClientManager? clientManager)
    {
        try
        {
            if (clientManager == null)
                return Task.FromResult(0);

            // In a real implementation, you would:
            // 1. Get all clients
            // 2. Check which ones are connected
            // 3. Return the count

            return Task.FromResult(0); // Placeholder - implement based on your IClientManager interface
        }
        catch
        {
            return Task.FromResult(0);
        }
    }

    /// <summary>
    /// Gets the number of tracks currently playing across all zones.
    /// </summary>
    private static Task<int> GetPlayingTracksAsync(IZoneManager? zoneManager)
    {
        try
        {
            if (zoneManager == null)
                return Task.FromResult(0);

            // In a real implementation, you would:
            // 1. Get all zones
            // 2. Check which ones are currently playing
            // 3. Return the count

            return Task.FromResult(0); // Placeholder - implement based on your zone playback state logic
        }
        catch
        {
            return Task.FromResult(0);
        }
    }

    [LoggerMessage(
        4001,
        LogLevel.Debug,
        "Business metrics collected - Zones: {ZonesTotal} total, {ZonesActive} active; "
            + "Clients: {ClientsConnected} connected; Tracks: {TracksPlaying} playing"
    )]
    private partial void LogBusinessMetricsCollected(
        int zonesTotal,
        int zonesActive,
        int clientsConnected,
        int tracksPlaying
    );
}
