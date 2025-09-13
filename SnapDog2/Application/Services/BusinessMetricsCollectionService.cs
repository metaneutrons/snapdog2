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

using SnapDog2.Domain.Abstractions;
using SnapDog2.Domain.Services;
using SnapDog2.Infrastructure.Metrics;
using SnapDog2.Shared.Enums;

/// <summary>
/// Background service that periodically collects and updates business metrics.
/// Provides real-time insights into zone activity, client connections, and audio playback.
/// </summary>
public partial class BusinessMetricsCollectionService(
    ILogger<BusinessMetricsCollectionService> logger,
    EnterpriseMetricsService metricsService,
    IServiceProvider serviceProvider)
    : BackgroundService
{
    private readonly TimeSpan _collectionInterval = TimeSpan.FromSeconds(15); // Collect every 15 seconds

    async protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogServiceStarted(this._collectionInterval);

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
                LogErrorCollectingMetrics(ex);

                // Record the error in metrics
                metricsService.RecordException(ex, "BusinessMetricsCollection");

                // Wait a bit before retrying
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }

        LogServiceStopped();
    }

    /// <summary>
    /// Collects current business metrics from various services.
    /// </summary>
    private async Task CollectBusinessMetricsAsync()
    {
        try
        {
            using var scope = serviceProvider.CreateScope();

            var zoneManager = scope.ServiceProvider.GetService<IZoneManager>();
            var clientManager = scope.ServiceProvider.GetService<IClientManager>();

            var businessMetrics = new BusinessMetricsState
            {
                ZonesTotal = await GetTotalZonesAsync(zoneManager),
                ZonesActive = await GetActiveZonesAsync(zoneManager),
                ClientsConnected = await GetConnectedClientsAsync(clientManager),
                TracksPlaying = await GetPlayingTracksAsync(zoneManager),
            };

            metricsService.UpdateBusinessMetrics(businessMetrics);

            this.LogBusinessMetricsCollected(
                businessMetrics.ZonesTotal,
                businessMetrics.ZonesActive,
                businessMetrics.ClientsConnected,
                businessMetrics.TracksPlaying
            );
        }
        catch (Exception ex)
        {
            LogFailedToCollectMetrics(ex);
        }
    }

    /// <summary>
    /// Gets the total number of configured zones.
    /// </summary>
    private static async Task<int> GetTotalZonesAsync(IZoneManager? zoneManager)
    {
        try
        {
            if (zoneManager == null)
            {
                return 0;
            }

            var result = await zoneManager.GetAllZoneStatesAsync();
            return result.IsSuccess ? result.Value!.Count : 0;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Gets the number of currently active zones (zones with activity).
    /// </summary>
    private static async Task<int> GetActiveZonesAsync(IZoneManager? zoneManager)
    {
        try
        {
            if (zoneManager == null)
            {
                return 0;
            }

            var result = await zoneManager.GetAllZoneStatesAsync();
            if (!result.IsSuccess || result.Value == null)
            {
                return 0;
            }

            // Count zones that are not stopped (playing or paused)
            return result.Value.Count(zone => zone.PlaybackState != PlaybackState.Stopped);
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Gets the number of connected Snapcast clients.
    /// </summary>
    private static async Task<int> GetConnectedClientsAsync(IClientManager? clientManager)
    {
        try
        {
            if (clientManager == null)
            {
                return 0;
            }

            var result = await clientManager.GetAllClientsAsync();
            if (!result.IsSuccess || result.Value == null)
            {
                return 0;
            }

            // Count clients that are connected
            return result.Value.Count(client => client.Connected);
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Gets the number of tracks currently playing across all zones.
    /// </summary>
    private static async Task<int> GetPlayingTracksAsync(IZoneManager? zoneManager)
    {
        try
        {
            if (zoneManager == null)
            {
                return 0;
            }

            var result = await zoneManager.GetAllZoneStatesAsync();
            if (!result.IsSuccess || result.Value == null)
            {
                return 0;
            }

            // Count zones that are currently playing
            return result.Value.Count(zone => zone.PlaybackState == PlaybackState.Playing);
        }
        catch
        {
            return 0;
        }
    }

    [LoggerMessage(EventId = 11000, Level = LogLevel.Information, Message = "ServiceStarted: {CollectionInterval}")]
    private partial void LogServiceStarted(TimeSpan collectionInterval);

    [LoggerMessage(EventId = 11001, Level = LogLevel.Warning, Message = "ErrorCollectingMetrics")]
    private partial void LogErrorCollectingMetrics(Exception ex);

    [LoggerMessage(EventId = 11002, Level = LogLevel.Information, Message = "ServiceStopped")]
    private partial void LogServiceStopped();

    [LoggerMessage(EventId = 11003, Level = LogLevel.Warning, Message = "FailedToCollectMetrics")]
    private partial void LogFailedToCollectMetrics(Exception ex);

    [LoggerMessage(EventId = 11004, Level = LogLevel.Debug, Message = "Business metrics collected - Zones: {ZonesTotal} total, {ZonesActive} active;"
            + "Clients: {ClientsConnected} connected; Tracks: {TracksPlaying} playing"
)]
    private partial void LogBusinessMetricsCollected(
        int zonesTotal,
        int zonesActive,
        int clientsConnected,
        int tracksPlaying
    );

}
