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

using Microsoft.AspNetCore.SignalR;
using SnapDog2.Api.Hubs;
using SnapDog2.Api.Models;
using SnapDog2.Domain.Abstractions;
using SnapDog2.Shared.Configuration;
using SnapDog2.Shared.Models;

/// <summary>
/// Production implementation of IPlaylistManager that delegates to playlist query handlers.
/// Properly handles radio stations (index 1) and Subsonic playlists (index 2+) with 1-based indexing.
/// </summary>
public partial class PlaylistManager : IPlaylistManager
{
    private readonly ILogger<PlaylistManager> _logger;
    private readonly ISubsonicService _subsonicService;
    private readonly IHubContext<SnapDogHub> _hubContext;
    private readonly SnapDogConfiguration _configuration;

    [LoggerMessage(EventId = 110300, Level = LogLevel.Debug, Message = "Getting all playlists via query handlers"
)]
    private partial void LogGettingAllPlaylists();

    [LoggerMessage(EventId = 110301, Level = LogLevel.Debug, Message = "Getting tracks for playlist index: {PlaylistIndex}"
)]
    private partial void LogGettingTracksByPlaylistIndex(int playlistIndex);

    [LoggerMessage(EventId = 110302, Level = LogLevel.Warning, Message = "Playlist index {PlaylistIndex} not found"
)]
    private partial void LogPlaylistIndexNotFound(int playlistIndex);

    [LoggerMessage(EventId = 110303, Level = LogLevel.Error, Message = "Failed → get playlists: {Error}"
)]
    private partial void LogPlaylistError(string error);

    public PlaylistManager(
        ILogger<PlaylistManager> logger,
        ISubsonicService subsonicService,
        IHubContext<SnapDogHub> hubContext,
        SnapDogConfiguration configuration)
    {
        this._logger = logger;
        this._subsonicService = subsonicService;
        this._hubContext = hubContext;
        this._configuration = configuration;
    }

    public async Task<Result<List<PlaylistInfo>>> GetAllPlaylistsAsync()
    {
        this.LogGettingAllPlaylists();

        try
        {
            var playlists = new List<PlaylistInfo>();

            // Add radio stations as playlist index 1
            if (this._configuration.RadioStations.Count > 0)
            {
                var radioPlaylist = new PlaylistInfo
                {
                    Index = 1,
                    Name = "Radio Stations",
                    TrackCount = this._configuration.RadioStations.Count,
                    Source = "radio"
                };
                playlists.Add(radioPlaylist);
            }

            // Add Subsonic playlists starting from index 2
            var subsonicResult = await this._subsonicService.GetPlaylistsAsync();
            if (subsonicResult.IsSuccess && subsonicResult.Value != null)
            {
                var subsonicPlaylists = subsonicResult.Value
                    .Select((playlist, index) => new PlaylistInfo
                    {
                        Index = index + 2, // Start from index 2
                        Name = playlist.Name,
                        TrackCount = playlist.TrackCount,
                        Source = "subsonic"
                    })
                    .ToList();

                playlists.AddRange(subsonicPlaylists);
            }

            return Result<List<PlaylistInfo>>.Success(playlists);
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
            // Handle radio stations (index 1)
            if (playlistIndex == 1)
            {
                var radioTracks = this._configuration.RadioStations
                    .Select((station, index) => new TrackInfo
                    {
                        Index = index + 1,
                        Title = station.Name,
                        Artist = "Radio Station",
                        Album = "Radio Stations",
                        Url = station.Url,
                        CoverArtUrl = station.CoverUrl,
                        Source = "radio"
                    })
                    .ToList();

                return Result<List<TrackInfo>>.Success(radioTracks);
            }

            // Handle Subsonic playlists (index 2+)
            if (playlistIndex >= 2)
            {
                // Get all Subsonic playlists to find the correct one
                var subsonicResult = await this._subsonicService.GetPlaylistsAsync();
                if (subsonicResult.IsFailure || subsonicResult.Value == null)
                {
                    this.LogPlaylistIndexNotFound(playlistIndex);
                    return Result<List<TrackInfo>>.Failure($"Failed to get Subsonic playlists: {subsonicResult.ErrorMessage}");
                }

                var subsonicIndex = playlistIndex - 2; // Convert to 0-based index for Subsonic
                var subsonicPlaylists = subsonicResult.Value.ToList();

                if (subsonicIndex >= subsonicPlaylists.Count)
                {
                    this.LogPlaylistIndexNotFound(playlistIndex);
                    return Result<List<TrackInfo>>.Failure($"Playlist at index {playlistIndex} not found");
                }

                var targetPlaylist = subsonicPlaylists[subsonicIndex];
                var playlistResult = await this._subsonicService.GetPlaylistAsync(targetPlaylist.SubsonicPlaylistId);

                if (playlistResult.IsFailure || playlistResult.Value == null)
                {
                    this.LogPlaylistIndexNotFound(playlistIndex);
                    return Result<List<TrackInfo>>.Failure($"Failed to get playlist tracks: {playlistResult.ErrorMessage}");
                }

                return Result<List<TrackInfo>>.Success(playlistResult.Value.Tracks);
            }

            this.LogPlaylistIndexNotFound(playlistIndex);
            return Result<List<TrackInfo>>.Failure($"Invalid playlist index: {playlistIndex}");
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
            // Handle radio stations (index 1)
            if (playlistIndex == 1)
            {
                if (this._configuration.RadioStations.Count > 0)
                {
                    var radioPlaylist = new PlaylistInfo
                    {
                        Index = 1,
                        Name = "Radio Stations",
                        TrackCount = this._configuration.RadioStations.Count,
                        Source = "radio"
                    };
                    return Result<PlaylistInfo>.Success(radioPlaylist);
                }
                else
                {
                    this.LogPlaylistIndexNotFound(playlistIndex);
                    return Result<PlaylistInfo>.Failure("No radio stations configured");
                }
            }

            // Handle Subsonic playlists (index 2+)
            if (playlistIndex >= 2)
            {
                var subsonicResult = await this._subsonicService.GetPlaylistsAsync();
                if (subsonicResult.IsFailure || subsonicResult.Value == null)
                {
                    this.LogPlaylistIndexNotFound(playlistIndex);
                    return Result<PlaylistInfo>.Failure($"Failed to get Subsonic playlists: {subsonicResult.ErrorMessage}");
                }

                var subsonicIndex = playlistIndex - 2; // Convert to 0-based index for Subsonic
                var subsonicPlaylists = subsonicResult.Value.ToList();

                if (subsonicIndex >= subsonicPlaylists.Count)
                {
                    this.LogPlaylistIndexNotFound(playlistIndex);
                    return Result<PlaylistInfo>.Failure($"Playlist at index {playlistIndex} not found");
                }

                var targetPlaylist = subsonicPlaylists[subsonicIndex];
                var playlistInfo = new PlaylistInfo
                {
                    Index = playlistIndex,
                    Name = targetPlaylist.Name,
                    TrackCount = targetPlaylist.TrackCount,
                    Source = "subsonic"
                };

                return Result<PlaylistInfo>.Success(playlistInfo);
            }

            this.LogPlaylistIndexNotFound(playlistIndex);
            return Result<PlaylistInfo>.Failure($"Invalid playlist index: {playlistIndex}");
        }
        catch (Exception ex)
        {
            this.LogPlaylistError(ex.Message);
            return Result<PlaylistInfo>.Failure($"Exception getting playlist by index: {ex.Message}");
        }
    }
}
