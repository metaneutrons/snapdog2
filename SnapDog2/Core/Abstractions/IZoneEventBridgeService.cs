namespace SnapDog2.Core.Abstractions;

using System.Threading.Tasks;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Models;

/// <summary>
/// Service that bridges external Snapcast zone/group events to internal zone status notifications.
/// This ensures that external changes (from other clients, web UI, etc.) are properly reflected
/// in the SnapDog2 status notification system for blueprint compliance.
/// </summary>
public interface IZoneEventBridgeService
{
    /// <summary>
    /// Bridges an external zone volume change to internal status notification.
    /// </summary>
    /// <param name="zoneIndex">The zone index (1-based).</param>
    /// <param name="volume">The new volume level (0-100).</param>
    /// <returns>Task representing the async operation.</returns>
    Task BridgeZoneVolumeChangeAsync(int zoneIndex, int volume);

    /// <summary>
    /// Bridges an external zone mute change to internal status notification.
    /// </summary>
    /// <param name="zoneIndex">The zone index (1-based).</param>
    /// <param name="isMuted">The new mute state.</param>
    /// <returns>Task representing the async operation.</returns>
    Task BridgeZoneMuteChangeAsync(int zoneIndex, bool isMuted);

    /// <summary>
    /// Bridges an external zone playback state change to internal status notification.
    /// </summary>
    /// <param name="zoneIndex">The zone index (1-based).</param>
    /// <param name="playbackState">The new playback state.</param>
    /// <returns>Task representing the async operation.</returns>
    Task BridgeZonePlaybackStateChangeAsync(int zoneIndex, PlaybackState playbackState);

    /// <summary>
    /// Bridges an external zone track change to internal status notification.
    /// </summary>
    /// <param name="zoneIndex">The zone index (1-based).</param>
    /// <param name="trackInfo">The new track information.</param>
    /// <param name="trackIndex">The new track index (1-based).</param>
    /// <returns>Task representing the async operation.</returns>
    Task BridgeZoneTrackChangeAsync(int zoneIndex, TrackInfo trackInfo, int trackIndex);

    /// <summary>
    /// Bridges an external zone playlist change to internal status notification.
    /// </summary>
    /// <param name="zoneIndex">The zone index (1-based).</param>
    /// <param name="playlistInfo">The new playlist information.</param>
    /// <param name="playlistIndex">The new playlist index (1-based).</param>
    /// <returns>Task representing the async operation.</returns>
    Task BridgeZonePlaylistChangeAsync(int zoneIndex, PlaylistInfo playlistInfo, int playlistIndex);

    /// <summary>
    /// Bridges an external zone state change to internal status notification.
    /// </summary>
    /// <param name="zoneIndex">The zone index (1-based).</param>
    /// <param name="zoneState">The new zone state.</param>
    /// <returns>Task representing the async operation.</returns>
    Task BridgeZoneStateChangeAsync(int zoneIndex, ZoneState zoneState);
}
