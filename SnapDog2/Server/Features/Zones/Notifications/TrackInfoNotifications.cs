namespace SnapDog2.Server.Features.Zones.Notifications;

using Cortex.Mediator.Notifications;
using SnapDog2.Core.Attributes;
using SnapDog2.Core.Models;

/// <summary>
/// Notification published when a zone's detailed track information changes.
/// </summary>
[StatusId("TRACK_INFO")]
public record ZoneTrackInfoChangedNotification : INotification
{
    /// <summary>
    /// Gets the zone index (1-based).
    /// </summary>
    public required int ZoneIndex { get; init; }

    /// <summary>
    /// Gets the detailed track information.
    /// </summary>
    public required TrackInfo TrackInfo { get; init; }
}

/// <summary>
/// Notification published when a zone's track length information changes.
/// </summary>
[StatusId("TRACK_INFO_LENGTH")]
public record ZoneTrackLengthChangedNotification : INotification
{
    /// <summary>
    /// Gets the zone index (1-based).
    /// </summary>
    public required int ZoneIndex { get; init; }

    /// <summary>
    /// Gets the track length in milliseconds.
    /// </summary>
    public required long TrackLengthMs { get; init; }
}

/// <summary>
/// Notification published when a zone's track position changes.
/// </summary>
[StatusId("TRACK_INFO_POSITION")]
public record ZoneTrackPositionChangedNotification : INotification
{
    /// <summary>
    /// Gets the zone index (1-based).
    /// </summary>
    public required int ZoneIndex { get; init; }

    /// <summary>
    /// Gets the current track position in milliseconds.
    /// </summary>
    public required long TrackPositionMs { get; init; }
}

/// <summary>
/// Notification published when a zone's track title changes.
/// </summary>
[StatusId("TRACK_INFO_TITLE")]
public record ZoneTrackTitleChangedNotification : INotification
{
    /// <summary>
    /// Gets the zone index (1-based).
    /// </summary>
    public required int ZoneIndex { get; init; }

    /// <summary>
    /// Gets the track title.
    /// </summary>
    public required string TrackTitle { get; init; }
}

/// <summary>
/// Notification published when a zone's track artist changes.
/// </summary>
[StatusId("TRACK_INFO_ARTIST")]
public record ZoneTrackArtistChangedNotification : INotification
{
    /// <summary>
    /// Gets the zone index (1-based).
    /// </summary>
    public required int ZoneIndex { get; init; }

    /// <summary>
    /// Gets the track artist.
    /// </summary>
    public required string TrackArtist { get; init; }
}

/// <summary>
/// Notification published when a zone's track album changes.
/// </summary>
[StatusId("TRACK_INFO_ALBUM")]
public record ZoneTrackAlbumChangedNotification : INotification
{
    /// <summary>
    /// Gets the zone index (1-based).
    /// </summary>
    public required int ZoneIndex { get; init; }

    /// <summary>
    /// Gets the track album.
    /// </summary>
    public required string TrackAlbum { get; init; }
}

/// <summary>
/// Notification published when a zone's playlist information changes.
/// </summary>
[StatusId("PLAYLIST_INFO")]
public record ZonePlaylistInfoChangedNotification : INotification
{
    /// <summary>
    /// Gets the zone index (1-based).
    /// </summary>
    public required int ZoneIndex { get; init; }

    /// <summary>
    /// Gets the detailed playlist information.
    /// </summary>
    public required PlaylistInfo PlaylistInfo { get; init; }
}
