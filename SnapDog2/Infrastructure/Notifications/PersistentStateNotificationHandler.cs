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
namespace SnapDog2.Infrastructure.Notifications;

using System.Collections.Concurrent;
using Cortex.Mediator.Notifications;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Abstractions;
using SnapDog2.Server.Features.Clients.Notifications;
using SnapDog2.Server.Features.Zones.Notifications;

/// <summary>
/// Handles state change notifications and persists them immediately to Redis.
/// Provides event-based state persistence with debouncing for high-frequency updates.
/// </summary>
public partial class PersistentStateNotificationHandler :
    INotificationHandler<ZoneStateChangedNotification>,
    INotificationHandler<ClientStateChangedNotification>
{
    private readonly IPersistentStateStore _persistentStore;
    private readonly ILogger<PersistentStateNotificationHandler> _logger;

    // Debouncing for high-frequency updates (like position changes)
    private readonly ConcurrentDictionary<int, Timer> _zoneDebounceTimers = new();
    private readonly ConcurrentDictionary<int, Timer> _clientDebounceTimers = new();
    private readonly TimeSpan _debounceDelay = TimeSpan.FromSeconds(2);

    public PersistentStateNotificationHandler(
        IPersistentStateStore persistentStore,
        ILogger<PersistentStateNotificationHandler> logger)
    {
        _persistentStore = persistentStore;
        _logger = logger;
    }

    /// <summary>
    /// Handles zone state changes and persists them immediately.
    /// </summary>
    public async Task Handle(ZoneStateChangedNotification notification, CancellationToken cancellationToken)
    {
        try
        {
            LogZoneStatePersisting(notification.ZoneIndex);

            // For position updates, use debouncing to avoid excessive writes
            if (IsPositionOnlyUpdate(notification))
            {
                DebounceZoneSave(notification.ZoneIndex, notification.ZoneState);
                return;
            }

            // For important state changes (volume, mute, track, etc.), save immediately
            await _persistentStore.SaveZoneStateAsync(notification.ZoneIndex, notification.ZoneState);

            LogZoneStatePersisted(notification.ZoneIndex, notification.ZoneState.Name);
        }
        catch (Exception ex)
        {
            LogZoneStatePersistFailed(ex, notification.ZoneIndex);
        }
    }

    /// <summary>
    /// Handles client state changes and persists them immediately.
    /// </summary>
    public async Task Handle(ClientStateChangedNotification notification, CancellationToken cancellationToken)
    {
        try
        {
            LogClientStatePersisting(notification.ClientIndex);

            // Client state changes are typically less frequent, save immediately
            await _persistentStore.SaveClientStateAsync(notification.ClientIndex, notification.ClientState);

            LogClientStatePersisted(notification.ClientIndex, notification.ClientState.Name);
        }
        catch (Exception ex)
        {
            LogClientStatePersistFailed(ex, notification.ClientIndex);
        }
    }

    /// <summary>
    /// Debounces zone state saves for high-frequency updates like position changes.
    /// </summary>
    private void DebounceZoneSave(int zoneIndex, SnapDog2.Core.Models.ZoneState zoneState)
    {
        // Cancel existing timer
        if (_zoneDebounceTimers.TryGetValue(zoneIndex, out var existingTimer))
        {
            existingTimer.Dispose();
        }

        // Create new timer for delayed save
        _zoneDebounceTimers[zoneIndex] = new Timer(
            async _ =>
            {
                try
                {
                    await _persistentStore.SaveZoneStateAsync(zoneIndex, zoneState);
                    LogZoneStateDebounced(zoneIndex, zoneState.Name);
                }
                catch (Exception ex)
                {
                    LogZoneStatePersistFailed(ex, zoneIndex);
                }
                finally
                {
                    // Clean up timer
                    if (_zoneDebounceTimers.TryRemove(zoneIndex, out var timer))
                    {
                        timer.Dispose();
                    }
                }
            },
            null,
            _debounceDelay,
            Timeout.InfiniteTimeSpan
        );

        LogZoneStateDebouncing(zoneIndex);
    }

    /// <summary>
    /// Determines if this is a position-only update that should be debounced.
    /// </summary>
    private static bool IsPositionOnlyUpdate(ZoneStateChangedNotification notification)
    {
        // This is a heuristic - in a real implementation, you might want to 
        // compare with the previous state to see what actually changed
        // For now, we'll debounce all updates during playback
        return notification.ZoneState.PlaybackState == SnapDog2.Core.Enums.PlaybackState.Playing;
    }

    #region LoggerMessage Methods

    [LoggerMessage(
        EventId = 7000,
        Level = LogLevel.Debug,
        Message = "💾 Persisting zone {ZoneIndex} state..."
    )]
    private partial void LogZoneStatePersisting(int ZoneIndex);

    [LoggerMessage(
        EventId = 7001,
        Level = LogLevel.Debug,
        Message = "✅ Zone {ZoneIndex} ({ZoneName}) state persisted successfully"
    )]
    private partial void LogZoneStatePersisted(int ZoneIndex, string ZoneName);

    [LoggerMessage(
        EventId = 7002,
        Level = LogLevel.Error,
        Message = "❌ Failed to persist zone {ZoneIndex} state"
    )]
    private partial void LogZoneStatePersistFailed(Exception ex, int ZoneIndex);

    [LoggerMessage(
        EventId = 7003,
        Level = LogLevel.Debug,
        Message = "💾 Persisting client {ClientIndex} state..."
    )]
    private partial void LogClientStatePersisting(int ClientIndex);

    [LoggerMessage(
        EventId = 7004,
        Level = LogLevel.Debug,
        Message = "✅ Client {ClientIndex} ({ClientName}) state persisted successfully"
    )]
    private partial void LogClientStatePersisted(int ClientIndex, string ClientName);

    [LoggerMessage(
        EventId = 7005,
        Level = LogLevel.Error,
        Message = "❌ Failed to persist client {ClientIndex} state"
    )]
    private partial void LogClientStatePersistFailed(Exception ex, int ClientIndex);

    [LoggerMessage(
        EventId = 7006,
        Level = LogLevel.Trace,
        Message = "⏱️ Debouncing zone {ZoneIndex} state save..."
    )]
    private partial void LogZoneStateDebouncing(int ZoneIndex);

    [LoggerMessage(
        EventId = 7007,
        Level = LogLevel.Debug,
        Message = "⏱️ Zone {ZoneIndex} ({ZoneName}) state saved after debounce"
    )]
    private partial void LogZoneStateDebounced(int ZoneIndex, string ZoneName);

    #endregion
}
