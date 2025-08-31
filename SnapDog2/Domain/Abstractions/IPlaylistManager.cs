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
namespace SnapDog2.Domain.Abstractions;

using SnapDog2.Shared.Models;

/// <summary>
/// Provides management operations for playlists and tracks.
/// </summary>
public interface IPlaylistManager
{
    /// <summary>
    /// Gets all available playlists.
    /// </summary>
    /// <returns>A result containing the list of all playlists.</returns>
    Task<Result<List<PlaylistInfo>>> GetAllPlaylistsAsync();

    /// <summary>
    /// Gets tracks for a specific playlist by ID.
    /// </summary>
    /// <param name="playlistIndex">The playlist ID.</param>
    /// <returns>A result containing the list of tracks in the playlist.</returns>
    Task<Result<List<TrackInfo>>> GetPlaylistTracksByIdAsync(string playlistIndex);

    /// <summary>
    /// Gets tracks for a specific playlist by index.
    /// </summary>
    /// <param name="playlistIndex">The playlist index (1-based).</param>
    /// <returns>A result containing the list of tracks in the playlist.</returns>
    Task<Result<List<TrackInfo>>> GetPlaylistTracksByIndexAsync(int playlistIndex);

    /// <summary>
    /// Gets a specific playlist by ID.
    /// </summary>
    /// <param name="playlistIndex">The playlist ID.</param>
    /// <returns>A result containing the playlist information.</returns>
    Task<Result<PlaylistInfo>> GetPlaylistByIdAsync(string playlistIndex);

    /// <summary>
    /// Gets a specific playlist by index.
    /// </summary>
    /// <param name="playlistIndex">The playlist index (1-based).</param>
    /// <returns>A result containing the playlist information.</returns>
    Task<Result<PlaylistInfo>> GetPlaylistByIndexAsync(int playlistIndex);
}
