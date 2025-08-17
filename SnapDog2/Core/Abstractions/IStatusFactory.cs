namespace SnapDog2.Core.Abstractions;

using SnapDog2.Core.Enums;
using SnapDog2.Core.Models;
using SnapDog2.Server.Features.Clients.Notifications;
using SnapDog2.Server.Features.Global.Notifications;
using SnapDog2.Server.Features.Zones.Notifications;

/// <summary>
/// Factory interface for creating status notifications with type safety and blueprint compliance.
/// Provides centralized, consistent status notification creation across all system components.
/// </summary>
public interface IStatusFactory
{
    #region Global Status Notifications

    /// <summary>
    /// Creates a system status notification.
    /// </summary>
    /// <param name="isOnline">Whether the system is online.</param>
    /// <returns>System status notification.</returns>
    SystemStatusChangedNotification CreateSystemStatusNotification(bool isOnline);

    /// <summary>
    /// Creates a version info notification.
    /// </summary>
    /// <param name="versionDetails">Version information.</param>
    /// <returns>Version info notification.</returns>
    VersionInfoChangedNotification CreateVersionInfoNotification(VersionDetails versionDetails);

    /// <summary>
    /// Creates a server stats notification.
    /// </summary>
    /// <param name="serverStats">Server statistics.</param>
    /// <returns>Server stats notification.</returns>
    ServerStatsChangedNotification CreateServerStatsNotification(ServerStats serverStats);

    /// <summary>
    /// Creates a system error notification.
    /// </summary>
    /// <param name="errorDetails">Error details.</param>
    /// <returns>System error notification.</returns>
    SystemErrorNotification CreateSystemErrorNotification(ErrorDetails errorDetails);

    /// <summary>
    /// Creates a zones info notification.
    /// </summary>
    /// <param name="availableZones">Array of available zone indices.</param>
    /// <returns>Zones info notification.</returns>
    ZonesInfoChangedNotification CreateZonesInfoNotification(int[] availableZones);

    #endregion

    #region Zone Status Notifications (State Changes)

    /// <summary>
    /// Creates a zone playback state changed notification.
    /// </summary>
    /// <param name="zoneIndex">Zone index (1-based).</param>
    /// <param name="playbackState">New playback state.</param>
    /// <returns>Zone playback state notification.</returns>
    ZonePlaybackStateChangedNotification CreateZonePlaybackStateChangedNotification(
        int zoneIndex,
        PlaybackState playbackState
    );

    /// <summary>
    /// Creates a zone volume changed notification.
    /// </summary>
    /// <param name="zoneIndex">Zone index (1-based).</param>
    /// <param name="volume">New volume level (0-100).</param>
    /// <returns>Zone volume changed notification.</returns>
    ZoneVolumeChangedNotification CreateZoneVolumeChangedNotification(int zoneIndex, int volume);

    /// <summary>
    /// Creates a zone mute changed notification.
    /// </summary>
    /// <param name="zoneIndex">Zone index (1-based).</param>
    /// <param name="isMuted">New mute state.</param>
    /// <returns>Zone mute changed notification.</returns>
    ZoneMuteChangedNotification CreateZoneMuteChangedNotification(int zoneIndex, bool isMuted);

    /// <summary>
    /// Creates a zone track changed notification.
    /// </summary>
    /// <param name="zoneIndex">Zone index (1-based).</param>
    /// <param name="trackInfo">New track information.</param>
    /// <param name="trackIndex">New track index (1-based).</param>
    /// <returns>Zone track changed notification.</returns>
    ZoneTrackChangedNotification CreateZoneTrackChangedNotification(int zoneIndex, TrackInfo trackInfo, int trackIndex);

    /// <summary>
    /// Creates a zone playlist changed notification.
    /// </summary>
    /// <param name="zoneIndex">Zone index (1-based).</param>
    /// <param name="playlistInfo">New playlist information.</param>
    /// <param name="playlistIndex">New playlist index (1-based).</param>
    /// <returns>Zone playlist changed notification.</returns>
    ZonePlaylistChangedNotification CreateZonePlaylistChangedNotification(
        int zoneIndex,
        PlaylistInfo playlistInfo,
        int playlistIndex
    );

    /// <summary>
    /// Creates a zone complete state changed notification.
    /// </summary>
    /// <param name="zoneIndex">Zone index (1-based).</param>
    /// <param name="zoneState">Complete zone state.</param>
    /// <returns>Zone state changed notification.</returns>
    ZoneStateChangedNotification CreateZoneStateChangedNotification(int zoneIndex, ZoneState zoneState);

    #endregion

    #region Zone Status Notifications (Blueprint Compliance - Status Publishing)

    /// <summary>
    /// Creates a zone playback state status notification for blueprint compliance.
    /// </summary>
    /// <param name="zoneIndex">Zone index (1-based).</param>
    /// <param name="playbackState">Playback state to publish.</param>
    /// <returns>Zone playback state status notification.</returns>
    ZonePlaybackStateStatusNotification CreateZonePlaybackStateStatusNotification(
        int zoneIndex,
        PlaybackState playbackState
    );

    /// <summary>
    /// Creates a zone volume status notification for blueprint compliance.
    /// </summary>
    /// <param name="zoneIndex">Zone index (1-based).</param>
    /// <param name="volume">Volume level to publish (0-100).</param>
    /// <returns>Zone volume status notification.</returns>
    ZoneVolumeStatusNotification CreateZoneVolumeStatusNotification(int zoneIndex, int volume);

    /// <summary>
    /// Creates a zone mute status notification for blueprint compliance.
    /// </summary>
    /// <param name="zoneIndex">Zone index (1-based).</param>
    /// <param name="isMuted">Mute state to publish.</param>
    /// <returns>Zone mute status notification.</returns>
    ZoneMuteStatusNotification CreateZoneMuteStatusNotification(int zoneIndex, bool isMuted);

    /// <summary>
    /// Creates a zone track status notification for blueprint compliance.
    /// </summary>
    /// <param name="zoneIndex">Zone index (1-based).</param>
    /// <param name="trackInfo">Track information to publish.</param>
    /// <param name="trackIndex">Track index to publish (1-based).</param>
    /// <returns>Zone track status notification.</returns>
    ZoneTrackStatusNotification CreateZoneTrackStatusNotification(int zoneIndex, TrackInfo trackInfo, int trackIndex);

    /// <summary>
    /// Creates a zone playlist status notification for blueprint compliance.
    /// </summary>
    /// <param name="zoneIndex">Zone index (1-based).</param>
    /// <param name="playlistInfo">Playlist information to publish.</param>
    /// <param name="playlistIndex">Playlist index to publish (1-based).</param>
    /// <returns>Zone playlist status notification.</returns>
    ZonePlaylistStatusNotification CreateZonePlaylistStatusNotification(
        int zoneIndex,
        PlaylistInfo playlistInfo,
        int playlistIndex
    );

    /// <summary>
    /// Creates a zone state status notification for blueprint compliance.
    /// </summary>
    /// <param name="zoneIndex">Zone index (1-based).</param>
    /// <param name="zoneState">Zone state to publish.</param>
    /// <returns>Zone state status notification.</returns>
    ZoneStateStatusNotification CreateZoneStateStatusNotification(int zoneIndex, ZoneState zoneState);

    #endregion

    #region Client Status Notifications (State Changes)

    /// <summary>
    /// Creates a client volume changed notification.
    /// </summary>
    /// <param name="clientIndex">Client index (1-based).</param>
    /// <param name="volume">New volume level (0-100).</param>
    /// <returns>Client volume changed notification.</returns>
    ClientVolumeChangedNotification CreateClientVolumeChangedNotification(int clientIndex, int volume);

    /// <summary>
    /// Creates a client mute changed notification.
    /// </summary>
    /// <param name="clientIndex">Client index (1-based).</param>
    /// <param name="isMuted">New mute state.</param>
    /// <returns>Client mute changed notification.</returns>
    ClientMuteChangedNotification CreateClientMuteChangedNotification(int clientIndex, bool isMuted);

    /// <summary>
    /// Creates a client latency changed notification.
    /// </summary>
    /// <param name="clientIndex">Client index (1-based).</param>
    /// <param name="latencyMs">New latency in milliseconds.</param>
    /// <returns>Client latency changed notification.</returns>
    ClientLatencyChangedNotification CreateClientLatencyChangedNotification(int clientIndex, int latencyMs);

    /// <summary>
    /// Creates a client connection changed notification.
    /// </summary>
    /// <param name="clientIndex">Client index (1-based).</param>
    /// <param name="isConnected">New connection state.</param>
    /// <returns>Client connection changed notification.</returns>
    ClientConnectionChangedNotification CreateClientConnectionChangedNotification(int clientIndex, bool isConnected);

    /// <summary>
    /// Creates a client zone assignment changed notification.
    /// </summary>
    /// <param name="clientIndex">Client index (1-based).</param>
    /// <param name="zoneIndex">New zone index (1-based, null if unassigned).</param>
    /// <returns>Client zone assignment changed notification.</returns>
    ClientZoneAssignmentChangedNotification CreateClientZoneAssignmentChangedNotification(
        int clientIndex,
        int? zoneIndex
    );

    /// <summary>
    /// Creates a client name changed notification.
    /// </summary>
    /// <param name="clientIndex">Client index (1-based).</param>
    /// <param name="name">New client name.</param>
    /// <returns>Client name changed notification.</returns>
    ClientNameChangedNotification CreateClientNameChangedNotification(int clientIndex, string name);

    /// <summary>
    /// Creates a client complete state changed notification.
    /// </summary>
    /// <param name="clientIndex">Client index (1-based).</param>
    /// <param name="clientState">Complete client state.</param>
    /// <returns>Client state changed notification.</returns>
    ClientStateChangedNotification CreateClientStateChangedNotification(int clientIndex, ClientState clientState);

    #endregion

    #region Client Status Notifications (Blueprint Compliance - Status Publishing)

    /// <summary>
    /// Creates a client volume status notification for blueprint compliance.
    /// </summary>
    /// <param name="clientIndex">Client index (1-based).</param>
    /// <param name="volume">Volume level to publish (0-100).</param>
    /// <returns>Client volume status notification.</returns>
    ClientVolumeStatusNotification CreateClientVolumeStatusNotification(int clientIndex, int volume);

    /// <summary>
    /// Creates a client mute status notification for blueprint compliance.
    /// </summary>
    /// <param name="clientIndex">Client index (1-based).</param>
    /// <param name="isMuted">Mute state to publish.</param>
    /// <returns>Client mute status notification.</returns>
    ClientMuteStatusNotification CreateClientMuteStatusNotification(int clientIndex, bool isMuted);

    /// <summary>
    /// Creates a client latency status notification for blueprint compliance.
    /// </summary>
    /// <param name="clientIndex">Client index (1-based).</param>
    /// <param name="latencyMs">Latency to publish in milliseconds.</param>
    /// <returns>Client latency status notification.</returns>
    ClientLatencyStatusNotification CreateClientLatencyStatusNotification(int clientIndex, int latencyMs);

    /// <summary>
    /// Creates a client zone status notification for blueprint compliance.
    /// </summary>
    /// <param name="clientIndex">Client index (1-based).</param>
    /// <param name="zoneIndex">Zone index to publish (1-based, null if unassigned).</param>
    /// <returns>Client zone status notification.</returns>
    ClientZoneStatusNotification CreateClientZoneStatusNotification(int clientIndex, int? zoneIndex);

    /// <summary>
    /// Creates a client connection status notification for blueprint compliance.
    /// </summary>
    /// <param name="clientIndex">Client index (1-based).</param>
    /// <param name="isConnected">Connection state to publish.</param>
    /// <returns>Client connection status notification.</returns>
    ClientConnectionStatusNotification CreateClientConnectionStatusNotification(int clientIndex, bool isConnected);

    /// <summary>
    /// Creates a client state status notification for blueprint compliance.
    /// </summary>
    /// <param name="clientIndex">Client index (1-based).</param>
    /// <param name="clientState">Client state to publish.</param>
    /// <returns>Client state status notification.</returns>
    ClientStateNotification CreateClientStateStatusNotification(int clientIndex, ClientState clientState);

    #endregion

    #region Command Response Status Notifications

    /// <summary>
    /// Creates a command status notification.
    /// </summary>
    /// <param name="commandId">Command identifier.</param>
    /// <param name="status">Command status.</param>
    /// <param name="message">Optional status message.</param>
    /// <returns>Command status notification.</returns>
    CommandStatusNotification CreateCommandStatusNotification(string commandId, string status, string? message = null);

    /// <summary>
    /// Creates a command error notification.
    /// </summary>
    /// <param name="commandId">Command identifier.</param>
    /// <param name="errorCode">Error code.</param>
    /// <param name="errorMessage">Error message.</param>
    /// <returns>Command error notification.</returns>
    CommandErrorNotification CreateCommandErrorNotification(string commandId, string errorCode, string errorMessage);

    #endregion
}
