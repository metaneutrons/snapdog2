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
namespace SnapDog2.Server.Zones.Queries;

using System.Collections.Generic;
using Cortex.Mediator.Queries;
using SnapDog2.Shared.Enums;
using SnapDog2.Shared.Models;

/// <summary>
/// Query to retrieve the state of all zones.
/// </summary>
public record GetAllZonesQuery : IQuery<Result<List<ZoneState>>>;

/// <summary>
/// Query to get the current state of a specific zone.
/// </summary>
public record GetZoneStateQuery : IQuery<Result<ZoneState>>
{
    /// <summary>
    /// Gets the index of the zone to query (1-based).
    /// </summary>
    public required int ZoneIndex { get; init; }
}

/// <summary>
/// Query to get the states of all zones.
/// </summary>
public record GetAllZoneStatesQuery : IQuery<Result<IEnumerable<ZoneState>>> { }

/// <summary>
/// Query to get the current playback state of a zone.
/// </summary>
public record GetZonePlaybackStateQuery : IQuery<Result<PlaybackState>>
{
    /// <summary>
    /// Gets the index of the zone to query (1-based).
    /// </summary>
    public required int ZoneIndex { get; init; }
}

/// <summary>
/// Query to get the current volume of a zone.
/// </summary>
public record GetZoneVolumeQuery : IQuery<Result<int>>
{
    /// <summary>
    /// Gets the index of the zone to query (1-based).
    /// </summary>
    public required int ZoneIndex { get; init; }
}

/// <summary>
/// Query to get the current track information for a zone.
/// </summary>
public record GetZoneTrackInfoQuery : IQuery<Result<TrackInfo>>
{
    /// <summary>
    /// Gets the index of the zone to query (1-based).
    /// </summary>
    public required int ZoneIndex { get; init; }
}

/// <summary>
/// Query to get the current playlist information for a zone.
/// </summary>
public record GetZonePlaylistInfoQuery : IQuery<Result<PlaylistInfo>>
{
    /// <summary>
    /// Gets the index of the zone to query (1-based).
    /// </summary>
    public required int ZoneIndex { get; init; }
}

/// <summary>
/// Query to get all available playlists.
/// </summary>
public record GetAllPlaylistsQuery : IQuery<Result<List<PlaylistInfo>>>;

/// <summary>
/// Query to get all tracks in a specific playlist.
/// </summary>
public record GetPlaylistTracksQuery : IQuery<Result<List<TrackInfo>>>
{
    /// <summary>
    /// Gets the index of the playlist to query (1-based).
    /// </summary>
    public required int PlaylistIndex { get; init; }
}
