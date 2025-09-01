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
using SnapDog2.Domain.Abstractions;
using SnapDog2.Server.Clients.Notifications;
using SnapDog2.Shared.Models;

/// <summary>
/// Handles state change notifications and persists them immediately to Redis.
/// Provides event-based state persistence with debouncing for high-frequency updates.
/// </summary>
public partial class PersistentStateNotificationHandler(
    IPersistentStateStore persistentStore,
    ILogger<PersistentStateNotificationHandler> logger)
    :
        INotificationHandler<ClientStateChangedNotification>
{
    // Debouncing for high-frequency updates (like position changes)
    private readonly ConcurrentDictionary<int, Timer> _zoneDebounceTimers = new();
    private readonly TimeSpan _debounceDelay = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Handles client state changes and persists them immediately.
    /// </summary>
    public async Task Handle(ClientStateChangedNotification notification, CancellationToken cancellationToken)
    {
        try
        {
            this.LogClientStatePersisting(notification.ClientIndex);

            // Client state changes are typically less frequent, save immediately
            await persistentStore.SaveClientStateAsync(notification.ClientIndex, notification.ClientState);

            this.LogClientStatePersisted(notification.ClientIndex, notification.ClientState.Name);
        }
        catch (Exception ex)
        {
            this.LogClientStatePersistFailed(ex, notification.ClientIndex);
        }
    }

    /// <summary>
    /// Debounces zone state saves for high-frequency updates like position changes.
    /// </summary>
    private void DebounceZoneSave(int zoneIndex, ZoneState zoneState)
    {
        // TODO: Why is this method never used?

        // Cancel existing timer
        if (this._zoneDebounceTimers.TryGetValue(zoneIndex, out var existingTimer))
        {
            existingTimer.Dispose();
        }

        // Create new timer for delayed save
        this._zoneDebounceTimers[zoneIndex] = new Timer(
            async void (_) =>
            {
                try
                {
                    await persistentStore.SaveZoneStateAsync(zoneIndex, zoneState);
                    this.LogZoneStateDebounced(zoneIndex, zoneState.Name);
                }
                catch (Exception ex)
                {
                    this.LogZoneStatePersistFailed(ex, zoneIndex);
                }
                finally
                {
                    // Clean up timer
                    if (this._zoneDebounceTimers.TryRemove(zoneIndex, out var timer))
                    {
                        timer.Dispose();
                    }
                }
            },
            null, this._debounceDelay,
            Timeout.InfiniteTimeSpan
        );

        this.LogZoneStateDebouncing(zoneIndex);
    }

    /// <summary>
    /// Determines if this is a position-only update that should be debounced.
    /// </summary>
    #region LoggerMessage Methods

    [LoggerMessage(
        EventId = 10200,
        Level = LogLevel.Debug,
        Message = "💾 Persisting zone {ZoneIndex} state..."
    )]
    private partial void LogZoneStatePersisting(int ZoneIndex);

    [LoggerMessage(
        EventId = 10201,
        Level = LogLevel.Debug,
        Message = "✅ Zone {ZoneIndex} ({ZoneName}) state persisted successfully"
    )]
    private partial void LogZoneStatePersisted(int ZoneIndex, string ZoneName);

    [LoggerMessage(
        EventId = 10202,
        Level = LogLevel.Error,
        Message = "❌ Failed to persist zone {ZoneIndex} state"
    )]
    private partial void LogZoneStatePersistFailed(Exception ex, int ZoneIndex);

    [LoggerMessage(
        EventId = 10203,
        Level = LogLevel.Debug,
        Message = "💾 Persisting client {ClientIndex} state..."
    )]
    private partial void LogClientStatePersisting(int ClientIndex);

    [LoggerMessage(
        EventId = 10204,
        Level = LogLevel.Debug,
        Message = "✅ Client {ClientIndex} ({ClientName}) state persisted successfully"
    )]
    private partial void LogClientStatePersisted(int ClientIndex, string ClientName);

    [LoggerMessage(
        EventId = 10205,
        Level = LogLevel.Error,
        Message = "❌ Failed to persist client {ClientIndex} state"
    )]
    private partial void LogClientStatePersistFailed(Exception ex, int ClientIndex);

    [LoggerMessage(
        EventId = 10206,
        Level = LogLevel.Trace,
        Message = "⏱️ Debouncing zone {ZoneIndex} state save..."
    )]
    private partial void LogZoneStateDebouncing(int ZoneIndex);

    [LoggerMessage(
        EventId = 10207,
        Level = LogLevel.Debug,
        Message = "⏱️ Zone {ZoneIndex} ({ZoneName}) state saved after debounce"
    )]
    private partial void LogZoneStateDebounced(int ZoneIndex, string ZoneName);

    #endregion
}
