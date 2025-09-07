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
namespace SnapDog2.Server.Playlists.Handlers;

using Cortex.Mediator.Queries;
using Microsoft.Extensions.Options;
using SnapDog2.Api.Models;
using SnapDog2.Domain.Abstractions;
using SnapDog2.Server.Playlists.Queries;
using SnapDog2.Shared.Configuration;
using SnapDog2.Shared.Models;

/// <summary>
/// Handler for getting all playlists including radio stations.
/// </summary>
public partial class GetAllPlaylistsQueryHandler(
    ISubsonicService subsonicService,
    IOptions<SnapDogConfiguration> configOptions,
    ILogger<GetAllPlaylistsQueryHandler> logger
) : IQueryHandler<GetAllPlaylistsQuery, Result<List<PlaylistInfo>>>
{
    private readonly ISubsonicService _subsonicService = subsonicService;
    private readonly SnapDogConfiguration _config = configOptions.Value;
    private readonly ILogger<GetAllPlaylistsQueryHandler> _logger = logger;

    public async Task<Result<List<PlaylistInfo>>> Handle(
        GetAllPlaylistsQuery query,
        CancellationToken cancellationToken
    )
    {
        _logger.LogInformation("GettingAllPlaylists");

        var playlists = new List<PlaylistInfo>();

        // First playlist is always radio stations (from environment variables)
        var radioPlaylist = this.CreateRadioPlaylist();
        playlists.Add(radioPlaylist);

        // Add Subsonic playlists if enabled
        if (this._config.Services.Subsonic.Enabled)
        {
            var subsonicResult = await this._subsonicService.GetPlaylistsAsync(cancellationToken);
            if (subsonicResult.IsSuccess)
            {
                // Add index numbers starting from 2 (radio is 1)
                var indexedPlaylists = (subsonicResult.Value ?? [])
                    .Select((playlist, index) => playlist with { Index = index + 2 })
                    .ToList();
                playlists.AddRange(indexedPlaylists);

                _logger.LogInformation("SubsonicPlaylistsAdded: {Details}", indexedPlaylists.Count);
            }
            else
            {
                _logger.LogInformation("SubsonicPlaylistsError: {Details}", subsonicResult.ErrorMessage ?? "Unknown error");
            }
        }

        _logger.LogInformation("AllPlaylistsRetrieved: {Details}", playlists.Count);
        return Result<List<PlaylistInfo>>.Success(playlists);
    }

    /// <summary>
    /// Creates the radio stations playlist from environment configuration.
    /// </summary>
    private PlaylistInfo CreateRadioPlaylist()
    {
        var radioStations = this._config.RadioStations;

        return new PlaylistInfo
        {
            SubsonicPlaylistId = "radio",
            Name = "Radio Stations",
            Index = 1,
            TrackCount = radioStations.Count,
            TotalDurationSec = null, // Radio streams don't have duration
            Description = "Internet radio stations",
            CoverArtUrl = null,
            Source = "radio",
        };
    }

    #region Logging

    #endregion
}

/// <summary>
/// Handler for getting a specific playlist with tracks.
/// </summary>
public partial class GetPlaylistQueryHandler(
    ISubsonicService subsonicService,
    IOptions<SnapDogConfiguration> configOptions,
    ILogger<GetPlaylistQueryHandler> logger
) : IQueryHandler<GetPlaylistQuery, Result<PlaylistWithTracks>>
{
    private readonly ISubsonicService _subsonicService = subsonicService;
    private readonly SnapDogConfiguration _config = configOptions.Value;
    private readonly ILogger<GetPlaylistQueryHandler> _logger = logger;

    public async Task<Result<PlaylistWithTracks>> Handle(
        GetPlaylistQuery query,
        CancellationToken cancellationToken
    )
    {
        _logger.LogInformation("GettingPlaylist: {Details}", query.PlaylistIndex);

        // Handle radio stations playlist (index 1)
        if (query.PlaylistIndex == 1)
        {
            return this.CreateRadioPlaylistWithTracks();
        }

        // Handle Subsonic playlists (index 2+)
        if (!this._config.Services.Subsonic.Enabled)
        {
            _logger.LogInformation("SubsonicDisabled");
            return Result<PlaylistWithTracks>.Failure("Subsonic service is disabled");
        }

        // Get all Subsonic playlists to find the one at the requested index
        var subsonicPlaylistsResult = await this._subsonicService.GetPlaylistsAsync(cancellationToken);
        if (subsonicPlaylistsResult.IsFailure)
        {
            _logger.LogError("PlaylistError: {PlaylistIndex} {Error}", query.PlaylistIndex.ToString(), subsonicPlaylistsResult.ErrorMessage ?? "Unknown error");
            return Result<PlaylistWithTracks>.Failure(
                $"Failed to get Subsonic playlists: {subsonicPlaylistsResult.ErrorMessage}"
            );
        }

        var subsonicPlaylists = subsonicPlaylistsResult.Value ?? new List<PlaylistInfo>();

        // Find the playlist at the requested index (index 2 = first Subsonic playlist, index 3 = second, etc.)
        var subsonicIndex = query.PlaylistIndex - 2; // Convert to 0-based index for Subsonic playlists
        if (subsonicIndex < 0 || subsonicIndex >= subsonicPlaylists.Count)
        {
            _logger.LogInformation("Operation completed: {Param1} {Param2}", query.PlaylistIndex.ToString(), "Playlist index out of range");
            return Result<PlaylistWithTracks>.Failure($"Playlist {query.PlaylistIndex} not found");
        }

        var targetPlaylist = subsonicPlaylists[subsonicIndex];

        // Get the full playlist with tracks using the Subsonic ID
        var result = await this._subsonicService.GetPlaylistAsync(targetPlaylist.SubsonicPlaylistId, cancellationToken);
        if (result.IsSuccess)
        {
            _logger.LogInformation("Operation completed: {Param1} {Param2}", query.PlaylistIndex.ToString(), result.Value?.Tracks.Count ?? 0);
        }
        else
        {
            _logger.LogInformation("Operation completed: {Param1} {Param2}", query.PlaylistIndex.ToString(), result.ErrorMessage ?? "Unknown error");
        }

        return result;
    }

    /// <summary>
    /// Creates the radio stations playlist with tracks from environment configuration.
    /// </summary>
    private Result<PlaylistWithTracks> CreateRadioPlaylistWithTracks()
    {
        var radioStations = this._config.RadioStations;

        var playlist = new PlaylistInfo
        {
            SubsonicPlaylistId = "radio",
            Name = "Radio Stations",
            Index = 1,
            TrackCount = radioStations.Count,
            TotalDurationSec = null,
            Description = "Internet radio stations",
            CoverArtUrl = null,
            Source = "radio",
        };

        var tracks = radioStations
            .Select(
                (station, index) =>
                    new TrackInfo
                    {
                        Index = index + 1,
                        Title = station.Name,
                        Artist = "Radio",
                        Album = "Radio Stations",
                        DurationMs = null, // Radio streams don't have duration
                        PositionMs = 0,
                        CoverArtUrl = null,
                        Source = "radio",
                        Url = station.Url,
                    }
            )
            .ToList();

        var playlistWithTracks = new PlaylistWithTracks(playlist, tracks);

        _logger.LogInformation("RadioPlaylistCreated: {Details}", tracks.Count);
        return Result<PlaylistWithTracks>.Success(playlistWithTracks);
    }

    #region Logging

    #endregion
}

/// <summary>
/// Handler for getting stream URL for a track.
/// </summary>
public partial class GetStreamUrlQueryHandler(
    ISubsonicService subsonicService,
    IOptions<SnapDogConfiguration> configOptions,
    ILogger<GetStreamUrlQueryHandler> logger
) : IQueryHandler<GetStreamUrlQuery, Result<string>>
{
    private readonly ISubsonicService _subsonicService = subsonicService;
    private readonly SnapDogConfiguration _config = configOptions.Value;
    private readonly ILogger<GetStreamUrlQueryHandler> _logger = logger;

    public async Task<Result<string>> Handle(GetStreamUrlQuery query, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GettingStreamUrl: {Details}", query.TrackId);

        // For radio stations, the track ID is already the stream URL
        if (IsRadioStreamUrl(query.TrackId))
        {
            _logger.LogInformation("RadioStreamUrl: {Details}", query.TrackId);
            return Result<string>.Success(query.TrackId);
        }

        // Handle Subsonic tracks
        if (!this._config.Services.Subsonic.Enabled)
        {
            _logger.LogInformation("SubsonicDisabled");
            return Result<string>.Failure("Subsonic service is disabled");
        }

        var result = await this._subsonicService.GetStreamUrlAsync(query.TrackId, cancellationToken);
        if (result.IsSuccess)
        {
            _logger.LogInformation("StreamUrlRetrieved: {Details}", query.TrackId);
        }
        else
        {
            _logger.LogInformation("Operation completed: {Param1} {Param2}", query.TrackId, result.ErrorMessage ?? "Unknown error");
        }

        return result;
    }

    /// <summary>
    /// Determines if the track ID is a radio stream URL.
    /// </summary>
    private static bool IsRadioStreamUrl(string trackId)
    {
        return trackId.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || trackId.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
    }

    #region Logging

    #endregion
}

/// <summary>
/// Handler for getting track details.
/// </summary>
public partial class GetTrackQueryHandler(
    IOptions<SnapDogConfiguration> configOptions,
    ILogger<GetTrackQueryHandler> logger
) : IQueryHandler<GetTrackQuery, Result<TrackInfo>>
{
    private readonly SnapDogConfiguration _config = configOptions.Value;

    public Task<Result<TrackInfo>> Handle(GetTrackQuery query, CancellationToken cancellationToken)
    {
        logger.LogInformation("GettingTrack: {Details}", query.TrackId);

        // For radio stations, check if it's a radio URL and create a TrackInfo
        if (IsRadioStreamUrl(query.TrackId))
        {
            var radioTrack = this.CreateRadioTrackInfo(query.TrackId);
            logger.LogInformation("RadioTrackRetrieved: {Details}", query.TrackId);
            return Task.FromResult(Result<TrackInfo>.Success(radioTrack));
        }

        // Handle Subsonic tracks - for now, we don't have a direct track lookup in Subsonic service
        // This would require extending the Subsonic service or searching through playlists
        logger.LogInformation("TrackNotImplemented: {Details}", query.TrackId);
        return Task.FromResult(
            Result<TrackInfo>.Failure($"Track lookup not implemented for track ID: {query.TrackId}")
        );
    }

    /// <summary>
    /// Determines if the track ID is a radio stream URL.
    /// </summary>
    private static bool IsRadioStreamUrl(string trackId)
    {
        return trackId.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || trackId.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Creates a TrackInfo for a radio stream URL.
    /// </summary>
    private TrackInfo CreateRadioTrackInfo(string url)
    {
        // Try to find the radio station in configuration
        var radioStations = this._config.RadioStations;
        var station = radioStations.FirstOrDefault(s => s.Url == url);

        return new TrackInfo
        {
            Index = 1,
            Title = station?.Name ?? "Unknown Radio Station",
            Artist = "Radio",
            Album = "Radio Stations",
            DurationMs = null, // Radio streams don't have duration
            PositionMs = 0,
            CoverArtUrl = null,
            Source = "radio",
            Url = url,
        };
    }

    #region Logging

    #endregion
}

/// <summary>
/// Handler for testing Subsonic connection.
/// </summary>
public partial class TestSubsonicConnectionQueryHandler(
    ISubsonicService subsonicService,
    IOptions<SnapDogConfiguration> configOptions,
    ILogger<TestSubsonicConnectionQueryHandler> logger
) : IQueryHandler<TestSubsonicConnectionQuery, Result>
{
    private readonly ISubsonicService _subsonicService = subsonicService;
    private readonly SnapDogConfiguration _config = configOptions.Value;
    private readonly ILogger<TestSubsonicConnectionQueryHandler> _logger = logger;

    public async Task<Result> Handle(TestSubsonicConnectionQuery query, CancellationToken cancellationToken)
    {
        _logger.LogInformation("TestingSubsonicConnection");

        if (!this._config.Services.Subsonic.Enabled)
        {
            _logger.LogInformation("SubsonicDisabled");
            return Result.Failure("Subsonic service is disabled");
        }

        var result = await this._subsonicService.TestConnectionAsync(cancellationToken);
        if (result.IsSuccess)
        {
            _logger.LogInformation("ConnectionTestSuccessful");
        }
        else
        {
            _logger.LogInformation("ConnectionTestFailed: {Details}", result.ErrorMessage ?? "Unknown error");
        }

        return result;
    }

    #region Logging

    #endregion
}
