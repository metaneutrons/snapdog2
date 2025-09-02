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
namespace SnapDog2.Domain.Services;

using Cortex.Mediator;
using SnapDog2.Api.Models;
using SnapDog2.Domain.Abstractions;
using SnapDog2.Server.Playlists.Queries;
using SnapDog2.Shared.Models;

/// <summary>
/// Production implementation of IPlaylistManager that delegates to playlist query handlers.
/// Properly handles radio stations (index 1) and Subsonic playlists (index 2+) with 1-based indexing.
/// </summary>
public partial class PlaylistManager : IPlaylistManager
{
    private readonly ILogger<PlaylistManager> _logger;
    private readonly IMediator _mediator;

    [LoggerMessage(
        EventId = 6800,
        Level = LogLevel.Debug,
        Message = "Getting all playlists via query handlers"
    )]
    private partial void LogGettingAllPlaylists();

    [LoggerMessage(
        EventId = 6802,
        Level = LogLevel.Debug,
        Message = "Getting tracks for playlist index: {PlaylistIndex}"
    )]
    private partial void LogGettingTracksByPlaylistIndex(int playlistIndex);

    [LoggerMessage(
        EventId = 6804,
        Level = LogLevel.Warning,
        Message = "Playlist index {PlaylistIndex} not found"
    )]
    private partial void LogPlaylistIndexNotFound(int playlistIndex);

    [LoggerMessage(
        EventId = 6805,
        Level = LogLevel.Error,
        Message = "Failed to get playlists: {Error}"
    )]
    private partial void LogPlaylistError(string error);

    public PlaylistManager(ILogger<PlaylistManager> logger, IMediator mediator)
    {
        this._logger = logger;
        this._mediator = mediator;
    }

    public async Task<Result<List<PlaylistInfo>>> GetAllPlaylistsAsync()
    {
        this.LogGettingAllPlaylists();

        try
        {
            var query = new GetAllPlaylistsQuery();
            var result = await this._mediator.SendQueryAsync<GetAllPlaylistsQuery, Result<List<PlaylistInfo>>>(query).ConfigureAwait(false);

            if (result.IsFailure)
            {
                this.LogPlaylistError(result.ErrorMessage ?? "Unknown error");
                return Result<List<PlaylistInfo>>.Failure(result.ErrorMessage ?? "Failed to get playlists");
            }

            return result;
        }
        catch (Exception ex)
        {
            this.LogPlaylistError(ex.Message);
            return Result<List<PlaylistInfo>>.Failure($"Exception getting playlists: {ex.Message}");
        }
    }

    public async Task<Result<List<TrackInfo>>> GetPlaylistTracksAsync(int playlistIndex)
    {
        this.LogGettingTracksByPlaylistIndex(playlistIndex);

        try
        {
            var query = new GetPlaylistQuery { PlaylistIndex = playlistIndex };
            var result = await this._mediator.SendQueryAsync<GetPlaylistQuery, Result<PlaylistWithTracks>>(query).ConfigureAwait(false);

            if (result.IsFailure)
            {
                this.LogPlaylistIndexNotFound(playlistIndex);
                return Result<List<TrackInfo>>.Failure(result.ErrorMessage ?? $"Playlist at index {playlistIndex} not found");
            }

            var tracks = result.Value?.Tracks ?? new List<TrackInfo>();
            return Result<List<TrackInfo>>.Success(tracks);
        }
        catch (Exception ex)
        {
            this.LogPlaylistError(ex.Message);
            return Result<List<TrackInfo>>.Failure($"Exception getting playlist by index: {ex.Message}");
        }
    }

    public async Task<Result<PlaylistInfo>> GetPlaylistAsync(int playlistIndex)
    {
        try
        {
            // Get all playlists and find the matching index
            var allPlaylistsResult = await this.GetAllPlaylistsAsync().ConfigureAwait(false);
            if (allPlaylistsResult.IsFailure)
            {
                return Result<PlaylistInfo>.Failure(allPlaylistsResult.ErrorMessage ?? "Failed to get playlists");
            }

            var playlist = allPlaylistsResult.Value?.FirstOrDefault(p => p.Index == playlistIndex);
            if (playlist == null)
            {
                this.LogPlaylistIndexNotFound(playlistIndex);
                return Result<PlaylistInfo>.Failure($"Playlist at index {playlistIndex} not found");
            }

            return Result<PlaylistInfo>.Success(playlist);
        }
        catch (Exception ex)
        {
            this.LogPlaylistError(ex.Message);
            return Result<PlaylistInfo>.Failure($"Exception getting playlist by index: {ex.Message}");
        }
    }
}
