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
/// Interface for playlist management operations.
/// Works with 1-based playlist indices: 1=Radio, 2+=Subsonic playlists.
/// </summary>
public interface IPlaylistManager
{
    /// <summary>
    /// Gets all available playlists with their 1-based indices.
    /// </summary>
    /// <returns>Result containing list of playlist information.</returns>
    Task<Result<List<PlaylistInfo>>> GetAllPlaylistsAsync();

    /// <summary>
    /// Gets tracks from a playlist by its 1-based index.
    /// </summary>
    /// <param name="playlistIndex">1-based playlist index (1=Radio, 2+=Subsonic).</param>
    /// <returns>Result containing list of tracks.</returns>
    Task<Result<List<TrackInfo>>> GetPlaylistTracksAsync(int playlistIndex);

    /// <summary>
    /// Gets playlist information by its 1-based index.
    /// </summary>
    /// <param name="playlistIndex">1-based playlist index (1=Radio, 2+=Subsonic).</param>
    /// <returns>Result containing playlist information.</returns>
    Task<Result<PlaylistInfo>> GetPlaylistAsync(int playlistIndex);
}
