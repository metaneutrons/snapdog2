//
// SnapDog
// The Snapcast-based Smart Home Audio System with MQTT & KNX integration
// Copyright (C) 2025 Fabian Schmieder
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <https://www.gnu.org/licenses/>.
//
namespace SnapDog2.Application.Services;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SnapDog2.Domain.Abstractions;
using SnapDog2.Domain.Services;
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
        this._logger = logger;
        this._metricsService = metricsService;
        this._serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogServiceStarted(this._logger, this._collectionInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await this.CollectBusinessMetricsAsync();
                await Task.Delay(this._collectionInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                LogErrorCollectingMetrics(this._logger, ex);

                // Record the error in metrics
                this._metricsService.RecordException(ex, "BusinessMetricsCollection");

                // Wait a bit before retrying
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }

        LogServiceStopped(this._logger);
    }

    /// <summary>
    /// Collects current business metrics from various services.
    /// </summary>
    private async Task CollectBusinessMetricsAsync()
    {
        try
        {
            using var scope = this._serviceProvider.CreateScope();

            var zoneManager = scope.ServiceProvider.GetService<IZoneManager>();
            var clientManager = scope.ServiceProvider.GetService<IClientManager>();

            var businessMetrics = new BusinessMetricsState
            {
                ZonesTotal = await GetTotalZonesAsync(zoneManager),
                ZonesActive = await GetActiveZonesAsync(zoneManager),
                ClientsConnected = await GetConnectedClientsAsync(clientManager),
                TracksPlaying = await GetPlayingTracksAsync(zoneManager),
            };

            this._metricsService.UpdateBusinessMetrics(businessMetrics);

            this.LogBusinessMetricsCollected(
                businessMetrics.ZonesTotal,
                businessMetrics.ZonesActive,
                businessMetrics.ClientsConnected,
                businessMetrics.TracksPlaying
            );
        }
        catch (Exception ex)
        {
            LogFailedToCollectMetrics(this._logger, ex);
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
            {
                return Task.FromResult(0);
            }

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
            {
                return Task.FromResult(0);
            }

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
            {
                return Task.FromResult(0);
            }

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
            {
                return Task.FromResult(0);
            }

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
        EventId = 7400,
        Level = Microsoft.Extensions.Logging.LogLevel.Debug,
        Message = "Business metrics collected - Zones: {ZonesTotal} total, {ZonesActive} active; "
            + "Clients: {ClientsConnected} connected; Tracks: {TracksPlaying} playing"
    )]
    private partial void LogBusinessMetricsCollected(
        int zonesTotal,
        int zonesActive,
        int clientsConnected,
        int tracksPlaying
    );

    [LoggerMessage(
        EventId = 7401,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "BusinessMetricsCollectionService started with {CollectionInterval} interval"
    )]
    private static partial void LogServiceStarted(ILogger logger, TimeSpan collectionInterval);

    [LoggerMessage(
        EventId = 7402,
        Level = Microsoft.Extensions.Logging.LogLevel.Error,
        Message = "Error occurred while collecting business metrics"
    )]
    private static partial void LogErrorCollectingMetrics(ILogger logger, Exception ex);

    [LoggerMessage(
        EventId = 7403,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "BusinessMetricsCollectionService stopped"
    )]
    private static partial void LogServiceStopped(ILogger logger);

    [LoggerMessage(
        EventId = 7404,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Failed to collect business metrics"
    )]
    private static partial void LogFailedToCollectMetrics(ILogger logger, Exception ex);
}
