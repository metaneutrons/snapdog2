namespace SnapDog2.Infrastructure.Services;

using System;
using System.Threading.Tasks;
using Cortex.Mediator;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Models;
using SnapDog2.Server.Features.Zones.Notifications;

/// <summary>
/// Service that bridges external Snapcast zone/group events to internal zone status notifications.
/// This ensures that external changes (from other clients, web UI, etc.) are properly reflected
/// in the SnapDog2 status notification system for blueprint compliance.
/// </summary>
public partial class ZoneEventBridgeService(
    ILogger<ZoneEventBridgeService> logger,
    IMediator mediator,
    IZoneManager zoneManager
) : IZoneEventBridgeService
{
    private readonly ILogger<ZoneEventBridgeService> _logger = logger;
    private readonly IMediator _mediator = mediator;
    private readonly IZoneManager _zoneManager = zoneManager;

    [LoggerMessage(8001, LogLevel.Debug, "Bridging zone {ZoneIndex} volume change: {Volume}")]
    private partial void LogVolumeChange(int zoneIndex, int volume);

    [LoggerMessage(8002, LogLevel.Debug, "Bridging zone {ZoneIndex} mute change: {IsMuted}")]
    private partial void LogMuteChange(int zoneIndex, bool isMuted);

    [LoggerMessage(8003, LogLevel.Debug, "Bridging zone {ZoneIndex} playback state change: {PlaybackState}")]
    private partial void LogPlaybackStateChange(int zoneIndex, PlaybackState playbackState);

    [LoggerMessage(8004, LogLevel.Debug, "Bridging zone {ZoneIndex} track change: {TrackTitle}")]
    private partial void LogTrackChange(int zoneIndex, string trackTitle);

    [LoggerMessage(8005, LogLevel.Debug, "Bridging zone {ZoneIndex} playlist change: {PlaylistName}")]
    private partial void LogPlaylistChange(int zoneIndex, string playlistName);

    [LoggerMessage(8006, LogLevel.Warning, "Failed to bridge zone {ZoneIndex} event: {Error}")]
    private partial void LogBridgeError(int zoneIndex, string error);

    /// <summary>
    /// Bridges an external zone volume change to internal status notification.
    /// </summary>
    /// <param name="zoneIndex">The zone index (1-based).</param>
    /// <param name="volume">The new volume level (0-100).</param>
    /// <returns>Task representing the async operation.</returns>
    public async Task BridgeZoneVolumeChangeAsync(int zoneIndex, int volume)
    {
        try
        {
            this.LogVolumeChange(zoneIndex, volume);

            // Get the zone service to publish the status notification
            var zoneResult = await this._zoneManager.GetZoneAsync(zoneIndex);
            if (zoneResult.IsFailure)
            {
                this.LogBridgeError(zoneIndex, $"Zone not found: {zoneResult.ErrorMessage}");
                return;
            }

            var zone = zoneResult.Value!;
            await zone.PublishVolumeStatusAsync(volume);
        }
        catch (Exception ex)
        {
            this.LogBridgeError(zoneIndex, ex.Message);
        }
    }

    /// <summary>
    /// Bridges an external zone mute change to internal status notification.
    /// </summary>
    /// <param name="zoneIndex">The zone index (1-based).</param>
    /// <param name="isMuted">The new mute state.</param>
    /// <returns>Task representing the async operation.</returns>
    public async Task BridgeZoneMuteChangeAsync(int zoneIndex, bool isMuted)
    {
        try
        {
            this.LogMuteChange(zoneIndex, isMuted);

            // Get the zone service to publish the status notification
            var zoneResult = await this._zoneManager.GetZoneAsync(zoneIndex);
            if (zoneResult.IsFailure)
            {
                this.LogBridgeError(zoneIndex, $"Zone not found: {zoneResult.ErrorMessage}");
                return;
            }

            var zone = zoneResult.Value!;
            await zone.PublishMuteStatusAsync(isMuted);
        }
        catch (Exception ex)
        {
            this.LogBridgeError(zoneIndex, ex.Message);
        }
    }

    /// <summary>
    /// Bridges an external zone playback state change to internal status notification.
    /// </summary>
    /// <param name="zoneIndex">The zone index (1-based).</param>
    /// <param name="playbackState">The new playback state.</param>
    /// <returns>Task representing the async operation.</returns>
    public async Task BridgeZonePlaybackStateChangeAsync(int zoneIndex, PlaybackState playbackState)
    {
        try
        {
            this.LogPlaybackStateChange(zoneIndex, playbackState);

            // Get the zone service to publish the status notification
            var zoneResult = await this._zoneManager.GetZoneAsync(zoneIndex);
            if (zoneResult.IsFailure)
            {
                this.LogBridgeError(zoneIndex, $"Zone not found: {zoneResult.ErrorMessage}");
                return;
            }

            var zone = zoneResult.Value!;
            await zone.PublishPlaybackStateStatusAsync(playbackState);
        }
        catch (Exception ex)
        {
            this.LogBridgeError(zoneIndex, ex.Message);
        }
    }

    /// <summary>
    /// Bridges an external zone track change to internal status notification.
    /// </summary>
    /// <param name="zoneIndex">The zone index (1-based).</param>
    /// <param name="trackInfo">The new track information.</param>
    /// <param name="trackIndex">The new track index (1-based).</param>
    /// <returns>Task representing the async operation.</returns>
    public async Task BridgeZoneTrackChangeAsync(int zoneIndex, TrackInfo trackInfo, int trackIndex)
    {
        try
        {
            this.LogTrackChange(zoneIndex, trackInfo.Title ?? "Unknown");

            // Get the zone service to publish the status notification
            var zoneResult = await this._zoneManager.GetZoneAsync(zoneIndex);
            if (zoneResult.IsFailure)
            {
                this.LogBridgeError(zoneIndex, $"Zone not found: {zoneResult.ErrorMessage}");
                return;
            }

            var zone = zoneResult.Value!;
            await zone.PublishTrackStatusAsync(trackInfo, trackIndex);
        }
        catch (Exception ex)
        {
            this.LogBridgeError(zoneIndex, ex.Message);
        }
    }

    /// <summary>
    /// Bridges an external zone playlist change to internal status notification.
    /// </summary>
    /// <param name="zoneIndex">The zone index (1-based).</param>
    /// <param name="playlistInfo">The new playlist information.</param>
    /// <param name="playlistIndex">The new playlist index (1-based).</param>
    /// <returns>Task representing the async operation.</returns>
    public async Task BridgeZonePlaylistChangeAsync(int zoneIndex, PlaylistInfo playlistInfo, int playlistIndex)
    {
        try
        {
            this.LogPlaylistChange(zoneIndex, playlistInfo.Name ?? "Unknown");

            // Get the zone service to publish the status notification
            var zoneResult = await this._zoneManager.GetZoneAsync(zoneIndex);
            if (zoneResult.IsFailure)
            {
                this.LogBridgeError(zoneIndex, $"Zone not found: {zoneResult.ErrorMessage}");
                return;
            }

            var zone = zoneResult.Value!;
            await zone.PublishPlaylistStatusAsync(playlistInfo, playlistIndex);
        }
        catch (Exception ex)
        {
            this.LogBridgeError(zoneIndex, ex.Message);
        }
    }

    /// <summary>
    /// Bridges an external zone state change to internal status notification.
    /// </summary>
    /// <param name="zoneIndex">The zone index (1-based).</param>
    /// <param name="zoneState">The new zone state.</param>
    /// <returns>Task representing the async operation.</returns>
    public async Task BridgeZoneStateChangeAsync(int zoneIndex, ZoneState zoneState)
    {
        try
        {
            // Get the zone service to publish the status notification
            var zoneResult = await this._zoneManager.GetZoneAsync(zoneIndex);
            if (zoneResult.IsFailure)
            {
                this.LogBridgeError(zoneIndex, $"Zone not found: {zoneResult.ErrorMessage}");
                return;
            }

            var zone = zoneResult.Value!;
            await zone.PublishZoneStateStatusAsync(zoneState);
        }
        catch (Exception ex)
        {
            this.LogBridgeError(zoneIndex, ex.Message);
        }
    }
}
