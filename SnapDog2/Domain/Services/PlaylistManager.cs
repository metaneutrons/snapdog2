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

using SnapDog2.Domain.Abstractions;
using SnapDog2.Shared.Models;

/// <summary>
/// Placeholder implementation of IPlaylistManager.
/// This will be replaced with actual music service integration later.
/// </summary>
public partial class PlaylistManager : IPlaylistManager
{
    private readonly ILogger<PlaylistManager> _logger;
    private readonly Dictionary<string, PlaylistInfo> _playlists;
    private readonly Dictionary<string, List<TrackInfo>> _playlistTracks;

    [LoggerMessage(
        EventId = 6800,
        Level = LogLevel.Debug,
        Message = "Getting all playlists"
    )]
    private partial void LogGettingAllPlaylists();

    [LoggerMessage(
        EventId = 6801,
        Level = LogLevel.Debug,
        Message = "Getting tracks for playlist ID: {PlaylistIndex}"
    )]
    private partial void LogGettingTracksByPlaylistIndex(string playlistIndex);

    [LoggerMessage(
        EventId = 6802,
        Level = LogLevel.Debug,
        Message = "Getting tracks for playlist index: {PlaylistIndex}"
    )]
    private partial void LogGettingTracksByPlaylistIndex(int playlistIndex);

    [LoggerMessage(
        EventId = 6803,
        Level = LogLevel.Warning,
        Message = "Playlist {PlaylistIndex} not found"
    )]
    private partial void LogPlaylistNotFound(string playlistIndex);

    [LoggerMessage(
        EventId = 6804,
        Level = LogLevel.Warning,
        Message = "Playlist index {PlaylistIndex} not found"
    )]
    private partial void LogPlaylistIndexNotFound(int playlistIndex);

    public PlaylistManager(ILogger<PlaylistManager> logger)
    {
        this._logger = logger;
        this._playlists = new Dictionary<string, PlaylistInfo>();
        this._playlistTracks = new Dictionary<string, List<TrackInfo>>();

        // FIXME: Initialize with placeholder playlists
        this.InitializePlaceholderPlaylists();
    }

    public async Task<Result<List<PlaylistInfo>>> GetAllPlaylistsAsync()
    {
        this.LogGettingAllPlaylists();

        await Task.Delay(1); // TODO: Fix simulation async operation

        var allPlaylists = this._playlists.Values.ToList();
        return Result<List<PlaylistInfo>>.Success(allPlaylists);
    }

    public async Task<Result<List<TrackInfo>>> GetPlaylistTracksByIdAsync(string playlistIndex)
    {
        this.LogGettingTracksByPlaylistIndex(playlistIndex);

        await Task.Delay(1); // TODO: Fix simulation async operation

        if (this._playlistTracks.TryGetValue(playlistIndex, out var tracks))
        {
            return Result<List<TrackInfo>>.Success(tracks);
        }

        this.LogPlaylistNotFound(playlistIndex);
        return Result<List<TrackInfo>>.Failure($"Playlist {playlistIndex} not found");
    }

    public async Task<Result<List<TrackInfo>>> GetPlaylistTracksByIndexAsync(int playlistIndex)
    {
        this.LogGettingTracksByPlaylistIndex(playlistIndex);

        await Task.Delay(1); // TODO: Fix simulation async operation

        var playlist = this._playlists.Values.FirstOrDefault(p => p.Index == playlistIndex);
        if (playlist != null && this._playlistTracks.TryGetValue(playlist.SubsonicPlaylistId, out var tracks))
        {
            return Result<List<TrackInfo>>.Success(tracks);
        }

        this.LogPlaylistIndexNotFound(playlistIndex);
        return Result<List<TrackInfo>>.Failure($"Playlist at index {playlistIndex} not found");
    }

    public async Task<Result<PlaylistInfo>> GetPlaylistByIdAsync(string playlistIndex)
    {
        await Task.Delay(1); // TODO: Fix simulation async operation

        if (this._playlists.TryGetValue(playlistIndex, out var playlist))
        {
            return Result<PlaylistInfo>.Success(playlist);
        }

        this.LogPlaylistNotFound(playlistIndex);
        return Result<PlaylistInfo>.Failure($"Playlist {playlistIndex} not found");
    }

    public async Task<Result<PlaylistInfo>> GetPlaylistByIndexAsync(int playlistIndex)
    {
        await Task.Delay(1); // TODO: Fix simulation async operation

        var playlist = this._playlists.Values.FirstOrDefault(p => p.Index == playlistIndex);
        if (playlist != null)
        {
            return Result<PlaylistInfo>.Success(playlist);
        }

        this.LogPlaylistIndexNotFound(playlistIndex);
        return Result<PlaylistInfo>.Failure($"Playlist at index {playlistIndex} not found");
    }

    private void InitializePlaceholderPlaylists()
    {
        // FIXME: Create placeholder playlists with sample tracks
        var playlists = new[]
        {
            new
            {
                Id = "rock_classics",
                Name = "Rock Classics",
                Index = 1,
            },
            new
            {
                Id = "jazz_standards",
                Name = "Jazz Standards",
                Index = 2,
            },
            new
            {
                Id = "electronic_mix",
                Name = "Electronic Mix",
                Index = 3,
            },
            new
            {
                Id = "acoustic_favorites",
                Name = "Acoustic Favorites",
                Index = 4,
            },
            new
            {
                Id = "workout_hits",
                Name = "Workout Hits",
                Index = 5,
            },
        };

        foreach (var playlistInfo in playlists)
        {
            var playlist = new PlaylistInfo
            {
                SubsonicPlaylistId = playlistInfo.Id,
                Source = "placeholder",
                Index = playlistInfo.Index,
                Name = playlistInfo.Name,
                TrackCount = 10 + playlistInfo.Index * 5, // Varying track counts
            };

            this._playlists[playlistInfo.Id] = playlist;

            // Create sample tracks for each playlist
            var tracks = new List<TrackInfo>();
            for (var i = 1; i <= playlist.TrackCount; i++)
            {
                var track = new TrackInfo
                {
                    Source = "placeholder",
                    Index = i,
                    Title = $"{playlistInfo.Name} Track {i}",
                    Artist = $"Artist {i}",
                    Album = $"{playlistInfo.Name} Album",
                    DurationMs = (180 + i % 4 * 60) * 1000, // 3-6 minute tracks in milliseconds
                    PositionMs = 0,
                    CoverArtUrl = null,
                    TimestampUtc = DateTime.UtcNow,
                    Url = $"placeholder://track/{playlistInfo.Id}/{i}",
                };

                tracks.Add(track);
            }

            this._playlistTracks[playlistInfo.Id] = tracks;
        }
    }
}
