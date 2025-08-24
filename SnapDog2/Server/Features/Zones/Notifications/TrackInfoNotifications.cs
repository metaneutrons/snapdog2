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
namespace SnapDog2.Server.Features.Zones.Notifications;

using System;
using Cortex.Mediator.Notifications;
using SnapDog2.Core.Attributes;
using SnapDog2.Core.Models;

// ============================================================================
// STATIC TRACK METADATA NOTIFICATIONS
// Information about the media file itself (does not change during playback)
// ============================================================================

/// <summary>
/// Notification published when a zone's complete track metadata changes.
/// Contains all static information about the media file.
/// </summary>
[StatusId("TRACK_METADATA")]
public record ZoneTrackMetadataChangedNotification : INotification
{
    /// <summary>
    /// Gets the zone index (1-based).
    /// </summary>
    public required int ZoneIndex { get; init; }

    /// <summary>
    /// Gets the complete track metadata.
    /// </summary>
    public required TrackInfo TrackInfo { get; init; }
}

/// <summary>
/// Notification published when a zone's track duration information changes.
/// This is static metadata from the media file.
/// </summary>
[StatusId("TRACK_METADATA_DURATION")]
public record ZoneTrackDurationChangedNotification : INotification
{
    /// <summary>
    /// Gets the zone index (1-based).
    /// </summary>
    public required int ZoneIndex { get; init; }

    /// <summary>
    /// Gets the track duration in milliseconds (static metadata).
    /// </summary>
    public required long DurationMs { get; init; }
}

/// <summary>
/// Notification published when a zone's track title changes.
/// This is static metadata from the media file.
/// </summary>
[StatusId("TRACK_METADATA_TITLE")]
public record ZoneTrackTitleChangedNotification : INotification
{
    /// <summary>
    /// Gets the zone index (1-based).
    /// </summary>
    public required int ZoneIndex { get; init; }

    /// <summary>
    /// Gets the track title (static metadata).
    /// </summary>
    public required string Title { get; init; }
}

/// <summary>
/// Notification published when a zone's track artist changes.
/// This is static metadata from the media file.
/// </summary>
[StatusId("TRACK_METADATA_ARTIST")]
public record ZoneTrackArtistChangedNotification : INotification
{
    /// <summary>
    /// Gets the zone index (1-based).
    /// </summary>
    public required int ZoneIndex { get; init; }

    /// <summary>
    /// Gets the track artist (static metadata).
    /// </summary>
    public required string Artist { get; init; }
}

/// <summary>
/// Notification published when a zone's track album changes.
/// This is static metadata from the media file.
/// </summary>
[StatusId("TRACK_METADATA_ALBUM")]
public record ZoneTrackAlbumChangedNotification : INotification
{
    /// <summary>
    /// Gets the zone index (1-based).
    /// </summary>
    public required int ZoneIndex { get; init; }

    /// <summary>
    /// Gets the track album (static metadata).
    /// </summary>
    public required string Album { get; init; }
}

/// <summary>
/// Notification published when a zone's track cover art URL changes.
/// This is static metadata from the media file.
/// </summary>
[StatusId("TRACK_METADATA_COVER")]
public record ZoneTrackCoverChangedNotification : INotification
{
    /// <summary>
    /// Gets the zone index (1-based).
    /// </summary>
    public required int ZoneIndex { get; init; }

    /// <summary>
    /// Gets the track cover art URL (static metadata).
    /// </summary>
    public required string CoverUrl { get; init; }
}

// ============================================================================
// DYNAMIC PLAYBACK STATE NOTIFICATIONS
// Real-time information about playback (changes during playback)
// ============================================================================

/// <summary>
/// Notification published when a zone's track playback position changes.
/// This is dynamic playback state that changes during playback.
/// </summary>
[StatusId("TRACK_POSITION_STATUS")]
public record ZoneTrackPositionChangedNotification : INotification
{
    /// <summary>
    /// Gets the zone index (1-based).
    /// </summary>
    public required int ZoneIndex { get; init; }

    /// <summary>
    /// Gets the current playback position in milliseconds (dynamic state).
    /// </summary>
    public required long PositionMs { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the position changed.
    /// </summary>
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Notification published when a zone's track playback progress changes.
/// This is dynamic playback state that changes during playback.
/// </summary>
[StatusId("TRACK_PROGRESS_STATUS")]
public record ZoneTrackProgressChangedNotification : INotification
{
    /// <summary>
    /// Gets the zone index (1-based).
    /// </summary>
    public required int ZoneIndex { get; init; }

    /// <summary>
    /// Gets the current playback progress as percentage 0.0-1.0 (dynamic state).
    /// </summary>
    public required float Progress { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the progress changed.
    /// </summary>
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Notification published when a zone's track playing status changes.
/// This is dynamic playback state that indicates if playback is active.
/// </summary>
[StatusId("TRACK_PLAYING_STATUS")]
public record ZoneTrackPlayingStatusChangedNotification : INotification
{
    /// <summary>
    /// Gets the zone index (1-based).
    /// </summary>
    public required int ZoneIndex { get; init; }

    /// <summary>
    /// Gets whether the track is currently playing (dynamic state).
    /// </summary>
    public required bool IsPlaying { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the playing status changed.
    /// </summary>
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
}

// ============================================================================
// PLAYLIST METADATA NOTIFICATIONS
// Information about playlists (separate from track metadata)
// ============================================================================

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
