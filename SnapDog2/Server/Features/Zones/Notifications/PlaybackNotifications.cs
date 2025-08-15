namespace SnapDog2.Server.Features.Zones.Notifications;

using Cortex.Mediator.Notifications;
using SnapDog2.Core.Models;

/// <summary>
/// Notification published when track playback starts in a zone.
/// </summary>
/// <param name="ZoneIndex">The zone where playback started</param>
/// <param name="Track">The track that started playing</param>
public record TrackPlaybackStartedNotification(int ZoneIndex, TrackInfo Track) : INotification;

/// <summary>
/// Notification published when track playback stops in a zone.
/// </summary>
/// <param name="ZoneIndex">The zone where playback stopped</param>
public record TrackPlaybackStoppedNotification(int ZoneIndex) : INotification;

/// <summary>
/// Notification published when track playback is paused in a zone.
/// </summary>
/// <param name="ZoneIndex">The zone where playback was paused</param>
public record TrackPlaybackPausedNotification(int ZoneIndex) : INotification;

/// <summary>
/// Notification published when a track ends naturally (not stopped by user).
/// </summary>
/// <param name="ZoneIndex">The zone where the track ended</param>
/// <param name="Track">The track that ended</param>
public record TrackEndedNotification(int ZoneIndex, TrackInfo Track) : INotification;

/// <summary>
/// Notification published when a playback error occurs.
/// </summary>
/// <param name="ZoneIndex">The zone where the error occurred</param>
/// <param name="Track">The track that was playing when the error occurred</param>
/// <param name="Error">The error that occurred</param>
public record PlaybackErrorNotification(int ZoneIndex, TrackInfo? Track, string Error) : INotification;

/// <summary>
/// Notification published when audio format changes in a zone.
/// </summary>
/// <param name="ZoneIndex">The zone where the format changed</param>
/// <param name="OldFormat">The previous audio format</param>
/// <param name="NewFormat">The new audio format</param>
public record AudioFormatChangedNotification(int ZoneIndex, AudioFormat? OldFormat, AudioFormat NewFormat)
    : INotification;

/// <summary>
/// Notification published when streaming buffer underrun occurs.
/// </summary>
/// <param name="ZoneIndex">The zone where the underrun occurred</param>
/// <param name="Track">The track that was playing</param>
public record StreamingBufferUnderrunNotification(int ZoneIndex, TrackInfo Track) : INotification;

/// <summary>
/// Notification published when streaming connection is lost.
/// </summary>
/// <param name="ZoneIndex">The zone where the connection was lost</param>
/// <param name="Track">The track that was playing</param>
/// <param name="Reason">The reason for connection loss</param>
public record StreamingConnectionLostNotification(int ZoneIndex, TrackInfo Track, string Reason) : INotification;
