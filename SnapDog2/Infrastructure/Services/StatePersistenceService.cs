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
/// Service that persists all zone and client states periodically and during graceful shutdown.
/// Only runs when Redis is enabled in configuration.
/// </summary>
public sealed partial class StatePersistenceService(
    IPersistentStateStore persistentStore,
    IZoneStateStore zoneStateStore,
    IClientStateStore clientStateStore,
    SnapDogConfiguration config,
    ILogger<StatePersistenceService> logger)
    : BackgroundService
{
    private readonly TimeSpan _persistenceInterval = TimeSpan.FromSeconds(60);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!config.Redis.Enabled)
        {
            LogRedisDisabled();
            return;
        }

        LogStatePersistenceStarted();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PersistAllStatesAsync();
                await Task.Delay(_persistenceInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                LogStatePersistenceStopping();
                break;
            }
            catch (Exception ex)
            {
                LogPeriodicPersistenceError(ex);
                await Task.Delay(_persistenceInterval, stoppingToken);
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (config.Redis.Enabled)
        {
            LogShutdownPersistenceStarted();
            await PersistAllStatesAsync();
            LogShutdownPersistenceCompleted();
        }

        await base.StopAsync(cancellationToken);
    }

    private async Task PersistAllStatesAsync()
    {
        // Persist all zone states
        for (var i = 0; i < config.Zones.Count; i++)
        {
            var zoneIndex = i + 1; // 1-based indexing
            var zoneState = zoneStateStore.GetZoneState(zoneIndex);
            if (zoneState != null)
            {
                await persistentStore.SaveZoneStateAsync(zoneIndex, zoneState);
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
            }
        }

        LogStatesPersisted(config.Zones.Count, config.Clients.Count);
    }

    #region Logging

    [LoggerMessage(EventId = 14137, Level = LogLevel.Information, Message = "Redis disabled - state persistence service will not run")]
    private partial void LogRedisDisabled();

    [LoggerMessage(EventId = 14138, Level = LogLevel.Information, Message = "State persistence service started (60s intervals)")]
    private partial void LogStatePersistenceStarted();

    [LoggerMessage(EventId = 14139, Level = LogLevel.Information, Message = "State persistence service stopping")]
    private partial void LogStatePersistenceStopping();

    [LoggerMessage(EventId = 14140, Level = LogLevel.Debug, Message = "Persisted {ZoneCount} zones and {ClientCount} clients")]
    private partial void LogStatesPersisted(int ZoneCount, int ClientCount);

    [LoggerMessage(EventId = 14141, Level = LogLevel.Information, Message = "Starting shutdown state persistence")]
    private partial void LogShutdownPersistenceStarted();

    [LoggerMessage(EventId = 14142, Level = LogLevel.Information, Message = "Shutdown state persistence completed")]
    private partial void LogShutdownPersistenceCompleted();

    [LoggerMessage(EventId = 14143, Level = LogLevel.Error, Message = "Error during periodic state persistence")]
    private partial void LogPeriodicPersistenceError(Exception ex);

    #endregion
}
