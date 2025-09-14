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
namespace SnapDog2.Infrastructure.Services;

using SnapDog2.Domain.Abstractions;
using SnapDog2.Shared.Configuration;

/// <summary>
/// Service that persists all zone and client states during graceful application shutdown.
/// Ensures state consistency between shutdown and startup.
/// </summary>
public sealed partial class ShutdownStatePersistenceService(
    IPersistentStateStore persistentStore,
    IZoneStateStore zoneStateStore,
    IClientStateStore clientStateStore,
    SnapDogConfiguration config,
    ILogger<ShutdownStatePersistenceService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait for shutdown signal
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        LogShutdownStatePersistenceStarted();

        try
        {
            // Persist all zone states
            for (var i = 0; i < config.Zones.Count; i++)
            {
                var zoneIndex = i + 1; // 1-based indexing
                var zoneState = zoneStateStore.GetZoneState(zoneIndex);
                if (zoneState != null)
                {
                    await persistentStore.SaveZoneStateAsync(zoneIndex, zoneState);
                    LogZoneStatePersisted(zoneIndex, zoneState.PlaybackState.ToString());
                }
            }

            // Persist all client states  
            for (var i = 0; i < config.Clients.Count; i++)
            {
                var clientIndex = i + 1; // 1-based indexing
                var clientState = clientStateStore.GetClientState(clientIndex);
                if (clientState != null)
                {
                    await persistentStore.SaveClientStateAsync(clientIndex, clientState);
                    LogClientStatePersisted(clientIndex, clientState.Name);
                }
            }

            LogShutdownStatePersistenceCompleted();
        }
        catch (Exception ex)
        {
            LogShutdownStatePersistenceError(ex);
        }

        await base.StopAsync(cancellationToken);
    }

    #region Logging

    [LoggerMessage(EventId = 15001, Level = LogLevel.Information, Message = "Starting shutdown state persistence")]
    private partial void LogShutdownStatePersistenceStarted();

    [LoggerMessage(EventId = 15002, Level = LogLevel.Debug, Message = "Persisted zone {ZoneIndex} state: {PlaybackState}")]
    private partial void LogZoneStatePersisted(int ZoneIndex, string PlaybackState);

    [LoggerMessage(EventId = 15003, Level = LogLevel.Debug, Message = "Persisted client {ClientIndex} state: {ClientName}")]
    private partial void LogClientStatePersisted(int ClientIndex, string ClientName);

    [LoggerMessage(EventId = 15004, Level = LogLevel.Information, Message = "Shutdown state persistence completed successfully")]
    private partial void LogShutdownStatePersistenceCompleted();

    [LoggerMessage(EventId = 15005, Level = LogLevel.Error, Message = "Error during shutdown state persistence")]
    private partial void LogShutdownStatePersistenceError(Exception ex);

    #endregion
}
