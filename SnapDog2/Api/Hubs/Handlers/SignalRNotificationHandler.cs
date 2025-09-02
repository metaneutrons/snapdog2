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
namespace SnapDog2.Api.Hubs.Handlers;

using Cortex.Mediator.Notifications;
using Microsoft.AspNetCore.SignalR;
using SnapDog2.Api.Hubs.Notifications;

/// <summary>
/// Handles domain notifications and emits them to SignalR clients.
/// Bridges the gap between internal domain events and real-time UI updates.
/// </summary>
public partial class SignalRNotificationHandler(
    IHubContext<SnapDogHub> hubContext,
    ILogger<SignalRNotificationHandler> logger
) :
    INotificationHandler<ZoneProgressChangedNotification>,
    INotificationHandler<ZoneTrackMetadataChangedNotification>,
    INotificationHandler<ZoneVolumeChangedNotification>,
    INotificationHandler<ZonePlaybackChangedNotification>,
    INotificationHandler<ZoneMuteChangedNotification>,
    INotificationHandler<ZoneRepeatModeChangedNotification>,
    INotificationHandler<ZoneShuffleChangedNotification>,
    INotificationHandler<ZonePlaylistChangedNotification>,
    INotificationHandler<ClientConnectedNotification>,
    INotificationHandler<ClientZoneChangedNotification>,
    INotificationHandler<ClientVolumeChangedNotification>,
    INotificationHandler<ClientMuteChangedNotification>,
    INotificationHandler<ClientLatencyChangedNotification>,
    INotificationHandler<ErrorOccurredNotification>,
    INotificationHandler<SystemStatusChangedNotification>
{
    private readonly IHubContext<SnapDogHub> _hubContext = hubContext;
    private readonly ILogger<SignalRNotificationHandler> _logger = logger;

    // ═══════════════════════════════════════════════════════════════════════════════
    // ZONE NOTIFICATIONS
    // ═══════════════════════════════════════════════════════════════════════════════

    public async Task Handle(ZoneProgressChangedNotification notification, CancellationToken cancellationToken)
    {
        await _hubContext.Clients.Group($"zone_{notification.ZoneIndex}")
            .SendAsync("ZoneProgressChanged", notification.ZoneIndex, notification.Position, notification.Progress, cancellationToken);

        LogZoneProgressEmitted(notification.ZoneIndex, notification.Position, notification.Progress);
    }

    public async Task Handle(ZoneTrackMetadataChangedNotification notification, CancellationToken cancellationToken)
    {
        await _hubContext.Clients.Group($"zone_{notification.ZoneIndex}")
            .SendAsync("ZoneTrackMetadataChanged", notification.ZoneIndex, notification.Track, cancellationToken);

        LogZoneTrackMetadataEmitted(notification.ZoneIndex, notification.Track.Title, notification.Track.Artist);
    }

    public async Task Handle(ZoneVolumeChangedNotification notification, CancellationToken cancellationToken)
    {
        await _hubContext.Clients.Group($"zone_{notification.ZoneIndex}")
            .SendAsync("ZoneVolumeChanged", notification.ZoneIndex, notification.Volume, cancellationToken);

        LogZoneVolumeEmitted(notification.ZoneIndex, notification.Volume);
    }

    public async Task Handle(ZonePlaybackChangedNotification notification, CancellationToken cancellationToken)
    {
        await _hubContext.Clients.Group($"zone_{notification.ZoneIndex}")
            .SendAsync("ZonePlaybackChanged", notification.ZoneIndex, notification.PlaybackState, cancellationToken);

        LogZonePlaybackEmitted(notification.ZoneIndex, notification.PlaybackState);
    }

    public async Task Handle(ZoneMuteChangedNotification notification, CancellationToken cancellationToken)
    {
        await _hubContext.Clients.Group($"zone_{notification.ZoneIndex}")
            .SendAsync("ZoneMuteChanged", notification.ZoneIndex, notification.Muted, cancellationToken);

        LogZoneMuteEmitted(notification.ZoneIndex, notification.Muted);
    }

    public async Task Handle(ZoneRepeatModeChangedNotification notification, CancellationToken cancellationToken)
    {
        await _hubContext.Clients.Group($"zone_{notification.ZoneIndex}")
            .SendAsync("ZoneRepeatModeChanged", notification.ZoneIndex, notification.TrackRepeat, notification.PlaylistRepeat, cancellationToken);

        LogZoneRepeatModeEmitted(notification.ZoneIndex, notification.TrackRepeat, notification.PlaylistRepeat);
    }

    public async Task Handle(ZoneShuffleChangedNotification notification, CancellationToken cancellationToken)
    {
        await _hubContext.Clients.Group($"zone_{notification.ZoneIndex}")
            .SendAsync("ZoneShuffleChanged", notification.ZoneIndex, notification.Shuffle, cancellationToken);

        LogZoneShuffleEmitted(notification.ZoneIndex, notification.Shuffle);
    }

    public async Task Handle(ZonePlaylistChangedNotification notification, CancellationToken cancellationToken)
    {
        await _hubContext.Clients.Group($"zone_{notification.ZoneIndex}")
            .SendAsync("ZonePlaylistChanged", notification.ZoneIndex, notification.Playlist?.Index ?? 0, notification.Playlist?.Name ?? "", cancellationToken);

        LogZonePlaylistEmitted(notification.ZoneIndex, notification.Playlist?.Index ?? 0, notification.Playlist?.Name ?? "");
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // CLIENT NOTIFICATIONS
    // ═══════════════════════════════════════════════════════════════════════════════

    public async Task Handle(ClientConnectedNotification notification, CancellationToken cancellationToken)
    {
        await _hubContext.Clients.Group($"client_{notification.ClientIndex}")
            .SendAsync("ClientConnected", notification.ClientIndex, notification.Connected, cancellationToken);

        LogClientConnectedEmitted(notification.ClientIndex, notification.Connected);
    }

    public async Task Handle(ClientZoneChangedNotification notification, CancellationToken cancellationToken)
    {
        await _hubContext.Clients.Group($"client_{notification.ClientIndex}")
            .SendAsync("ClientZoneChanged", notification.ClientIndex, notification.ZoneIndex, cancellationToken);

        LogClientZoneEmitted(notification.ClientIndex, notification.ZoneIndex);
    }

    public async Task Handle(ClientVolumeChangedNotification notification, CancellationToken cancellationToken)
    {
        await _hubContext.Clients.Group($"client_{notification.ClientIndex}")
            .SendAsync("ClientVolumeChanged", notification.ClientIndex, notification.Volume, cancellationToken);

        LogClientVolumeEmitted(notification.ClientIndex, notification.Volume);
    }

    public async Task Handle(ClientMuteChangedNotification notification, CancellationToken cancellationToken)
    {
        await _hubContext.Clients.Group($"client_{notification.ClientIndex}")
            .SendAsync("ClientMuteChanged", notification.ClientIndex, notification.Muted, cancellationToken);

        LogClientMuteEmitted(notification.ClientIndex, notification.Muted);
    }

    public async Task Handle(ClientLatencyChangedNotification notification, CancellationToken cancellationToken)
    {
        await _hubContext.Clients.Group($"client_{notification.ClientIndex}")
            .SendAsync("ClientLatencyChanged", notification.ClientIndex, notification.LatencyMs, cancellationToken);

        LogClientLatencyEmitted(notification.ClientIndex, notification.LatencyMs);
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // SYSTEM NOTIFICATIONS
    // ═══════════════════════════════════════════════════════════════════════════════

    public async Task Handle(ErrorOccurredNotification notification, CancellationToken cancellationToken)
    {
        await _hubContext.Clients.Group("system")
            .SendAsync("ErrorOccurred", notification.ErrorCode, notification.Message, notification.Context, cancellationToken);

        LogErrorEmitted(notification.ErrorCode, notification.Message);
    }

    public async Task Handle(SystemStatusChangedNotification notification, CancellationToken cancellationToken)
    {
        await _hubContext.Clients.Group("system")
            .SendAsync("SystemStatusChanged", notification.Status, cancellationToken);

        LogSystemStatusEmitted(notification.Status.IsOnline);
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // LOGGING METHODS
    // ═══════════════════════════════════════════════════════════════════════════════

    [LoggerMessage(EventId = 6000, Level = LogLevel.Debug, Message = "Emitted zone {ZoneIndex} progress: {Position}ms ({Progress:P1})")]
    private partial void LogZoneProgressEmitted(int zoneIndex, long position, float progress);

    [LoggerMessage(EventId = 6001, Level = LogLevel.Debug, Message = "Emitted zone {ZoneIndex} track metadata: '{Title}' by {Artist}")]
    private partial void LogZoneTrackMetadataEmitted(int zoneIndex, string title, string artist);

    [LoggerMessage(EventId = 6002, Level = LogLevel.Debug, Message = "Emitted zone {ZoneIndex} volume: {Volume}")]
    private partial void LogZoneVolumeEmitted(int zoneIndex, int volume);

    [LoggerMessage(EventId = 6003, Level = LogLevel.Debug, Message = "Emitted zone {ZoneIndex} playback: {PlaybackState}")]
    private partial void LogZonePlaybackEmitted(int zoneIndex, string playbackState);

    [LoggerMessage(EventId = 6004, Level = LogLevel.Debug, Message = "Emitted zone {ZoneIndex} mute: {Muted}")]
    private partial void LogZoneMuteEmitted(int zoneIndex, bool muted);

    [LoggerMessage(EventId = 6005, Level = LogLevel.Debug, Message = "Emitted zone {ZoneIndex} repeat: track={TrackRepeat}, playlist={PlaylistRepeat}")]
    private partial void LogZoneRepeatModeEmitted(int zoneIndex, bool trackRepeat, bool playlistRepeat);

    [LoggerMessage(EventId = 6006, Level = LogLevel.Debug, Message = "Emitted zone {ZoneIndex} shuffle: {Shuffle}")]
    private partial void LogZoneShuffleEmitted(int zoneIndex, bool shuffle);

    [LoggerMessage(EventId = 6007, Level = LogLevel.Debug, Message = "Emitted zone {ZoneIndex} playlist: {PlaylistId} '{PlaylistName}'")]
    private partial void LogZonePlaylistEmitted(int zoneIndex, int playlistId, string playlistName);

    [LoggerMessage(EventId = 6010, Level = LogLevel.Debug, Message = "Emitted client {ClientIndex} connected: {Connected}")]
    private partial void LogClientConnectedEmitted(int clientIndex, bool connected);

    [LoggerMessage(EventId = 6011, Level = LogLevel.Debug, Message = "Emitted client {ClientIndex} zone: {ZoneIndex}")]
    private partial void LogClientZoneEmitted(int clientIndex, int? zoneIndex);

    [LoggerMessage(EventId = 6012, Level = LogLevel.Debug, Message = "Emitted client {ClientIndex} volume: {Volume}")]
    private partial void LogClientVolumeEmitted(int clientIndex, int volume);

    [LoggerMessage(EventId = 6013, Level = LogLevel.Debug, Message = "Emitted client {ClientIndex} mute: {Muted}")]
    private partial void LogClientMuteEmitted(int clientIndex, bool muted);

    [LoggerMessage(EventId = 6014, Level = LogLevel.Debug, Message = "Emitted client {ClientIndex} latency: {LatencyMs}ms")]
    private partial void LogClientLatencyEmitted(int clientIndex, int latencyMs);

    [LoggerMessage(EventId = 6020, Level = LogLevel.Information, Message = "Emitted error: {ErrorCode} - {Message}")]
    private partial void LogErrorEmitted(string errorCode, string message);

    [LoggerMessage(EventId = 6021, Level = LogLevel.Debug, Message = "Emitted system status: online={IsOnline}")]
    private partial void LogSystemStatusEmitted(bool isOnline);
}
