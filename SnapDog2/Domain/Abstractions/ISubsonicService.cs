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

using SnapDog2.Api.Models;
using SnapDog2.Shared.Models;

/// <summary>
/// Interface for Subsonic API integration service.
/// Provides access to playlists and streaming functionality from Subsonic-compatible servers.
/// </summary>
public interface ISubsonicService
{
    /// <summary>
    /// Gets all available playlists from the Subsonic server.
    /// Note: Radio stations are handled separately and not included in this list.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing list of playlist information.</returns>
    Task<Result<IReadOnlyList<PlaylistInfo>>> GetPlaylistsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific playlist with all its tracks.
    /// </summary>
    /// <param name="playlistIndex">The playlist identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing playlist with tracks.</returns>
    Task<Result<PlaylistWithTracks>> GetPlaylistAsync(
        string playlistIndex,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Gets the streaming URL for a specific track.
    /// </summary>
    /// <param name="trackId">The track identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the streaming URL.</returns>
    Task<Result<string>> GetStreamUrlAsync(string trackId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets cover art image data by cover ID.
    /// </summary>
    /// <param name="coverId">The cover art identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing cover art data.</returns>
    Task<Result<CoverArtData>> GetCoverArtAsync(string coverId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests the connection to the Subsonic server.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating connection success.</returns>
    Task<Result> TestConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Initializes the Subsonic service and tests connectivity.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating initialization success.</returns>
    Task<Result> InitializeAsync(CancellationToken cancellationToken = default);
}
