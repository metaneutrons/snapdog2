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
namespace SnapDog2.Server.Playlists.Queries;

using Cortex.Mediator.Queries;
using SnapDog2.Api.Models;
using SnapDog2.Shared.Models;

/// <summary>
/// Query to retrieve all available playlists (including radio stations as first playlist).
/// </summary>
public record GetAllPlaylistsQuery : IQuery<Result<List<PlaylistInfo>>>;

/// <summary>
/// Query to retrieve a specific playlist with all its tracks.
/// </summary>
public record GetPlaylistQuery : IQuery<Result<PlaylistWithTracks>>
{
    /// <summary>
    /// Gets the playlist index (1-based). Index 1 is always radio stations, 2+ are Subsonic playlists.
    /// </summary>
    public required int PlaylistIndex { get; init; }
}

/// <summary>
/// Query to retrieve a specific playlist by string identifier (for API compatibility).
/// </summary>
public record GetPlaylistByIdQuery : IQuery<Result<PlaylistWithTracks>>
{
    /// <summary>
    /// Gets the playlist identifier (string format for API compatibility).
    /// </summary>
    public required string PlaylistId { get; init; }
}

/// <summary>
/// Query to get the streaming URL for a specific track.
/// </summary>
public record GetStreamUrlQuery : IQuery<Result<string>>
{
    /// <summary>
    /// Gets the track identifier.
    /// </summary>
    public required string TrackId { get; init; }
}

/// <summary>
/// Query to get details for a specific track.
/// </summary>
public record GetTrackQuery : IQuery<Result<TrackInfo>>
{
    /// <summary>
    /// Gets the track identifier.
    /// </summary>
    public required string TrackId { get; init; }
}

/// <summary>
/// Query to test Subsonic server connection.
/// </summary>
public record TestSubsonicConnectionQuery : IQuery<Result>;
