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
        LogGettingAllPlaylists(this._logger);

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

                LogSubsonicPlaylistsAdded(this._logger, indexedPlaylists.Count);
            }
            else
            {
                LogSubsonicPlaylistsError(this._logger, subsonicResult.ErrorMessage ?? "Unknown error");
            }
        }

        LogAllPlaylistsRetrieved(this._logger, playlists.Count);
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

    [LoggerMessage(EventId = 112900, Level = LogLevel.Debug, Message = "Getting all playlists (radio + subsonic)"
)]
    private static partial void LogGettingAllPlaylists(ILogger logger);

    [LoggerMessage(EventId = 112901, Level = LogLevel.Information, Message = "Added {Count} Subsonic playlists"
)]
    private static partial void LogSubsonicPlaylistsAdded(ILogger logger, int count);

    [LoggerMessage(EventId = 112902, Level = LogLevel.Warning, Message = "Failed → get Subsonic playlists: {Error}"
)]
    private static partial void LogSubsonicPlaylistsError(ILogger logger, string error);

    [LoggerMessage(EventId = 112903, Level = LogLevel.Information, Message = "Retrieved {Count} total playlists"
)]
    private static partial void LogAllPlaylistsRetrieved(ILogger logger, int count);

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
        LogGettingPlaylist(this._logger, query.PlaylistIndex);

        // Handle radio stations playlist (index 1)
        if (query.PlaylistIndex == 1)
        {
            return this.CreateRadioPlaylistWithTracks();
        }

        // Handle Subsonic playlists (index 2+)
        if (!this._config.Services.Subsonic.Enabled)
        {
            LogSubsonicDisabled(this._logger);
            return Result<PlaylistWithTracks>.Failure("Subsonic service is disabled");
        }

        // Get all Subsonic playlists to find the one at the requested index
        var subsonicPlaylistsResult = await this._subsonicService.GetPlaylistsAsync(cancellationToken);
        if (subsonicPlaylistsResult.IsFailure)
        {
            LogPlaylistError(
                this._logger,
                query.PlaylistIndex.ToString(),
                subsonicPlaylistsResult.ErrorMessage ?? "Unknown error"
            );
            return Result<PlaylistWithTracks>.Failure(
                $"Failed to get Subsonic playlists: {subsonicPlaylistsResult.ErrorMessage}"
            );
        }

        var subsonicPlaylists = subsonicPlaylistsResult.Value ?? new List<PlaylistInfo>();

        // Find the playlist at the requested index (index 2 = first Subsonic playlist, index 3 = second, etc.)
        var subsonicIndex = query.PlaylistIndex - 2; // Convert to 0-based index for Subsonic playlists
        if (subsonicIndex < 0 || subsonicIndex >= subsonicPlaylists.Count)
        {
            LogPlaylistError(this._logger, query.PlaylistIndex.ToString(), "Playlist index out of range");
            return Result<PlaylistWithTracks>.Failure($"Playlist {query.PlaylistIndex} not found");
        }

        var targetPlaylist = subsonicPlaylists[subsonicIndex];

        // Get the full playlist with tracks using the Subsonic ID
        var result = await this._subsonicService.GetPlaylistAsync(targetPlaylist.SubsonicPlaylistId, cancellationToken);
        if (result.IsSuccess)
        {
            LogPlaylistRetrieved(this._logger, query.PlaylistIndex.ToString(), result.Value?.Tracks.Count ?? 0);
        }
        else
        {
            LogPlaylistError(this._logger, query.PlaylistIndex.ToString(), result.ErrorMessage ?? "Unknown error");
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

        LogRadioPlaylistCreated(this._logger, tracks.Count);
        return Result<PlaylistWithTracks>.Success(playlistWithTracks);
    }

    #region Logging

    [LoggerMessage(EventId = 112904, Level = LogLevel.Debug, Message = "Getting playlist: {PlaylistIndex}"
)]
    private static partial void LogGettingPlaylist(ILogger logger, int playlistIndex);

    [LoggerMessage(EventId = 112905, Level = LogLevel.Warning, Message = "Subsonic service is disabled"
)]
    private static partial void LogSubsonicDisabled(ILogger logger);

    [LoggerMessage(EventId = 112906, Level = LogLevel.Information, Message = "Retrieved playlist: {PlaylistIndex} with {TrackCount} tracks"
)]
    private static partial void LogPlaylistRetrieved(ILogger logger, string playlistIndex, int trackCount);

    [LoggerMessage(EventId = 112907, Level = LogLevel.Error, Message = "Failed → get playlist: {PlaylistIndex}, error: {Error}"
)]
    private static partial void LogPlaylistError(ILogger logger, string playlistIndex, string error);

    [LoggerMessage(EventId = 112908, Level = LogLevel.Debug, Message = "Created radio playlist with {TrackCount} stations"
)]
    private static partial void LogRadioPlaylistCreated(ILogger logger, int trackCount);

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
        LogGettingStreamUrl(this._logger, query.TrackId);

        // For radio stations, the track ID is already the stream URL
        if (IsRadioStreamUrl(query.TrackId))
        {
            LogRadioStreamUrl(this._logger, query.TrackId);
            return Result<string>.Success(query.TrackId);
        }

        // Handle Subsonic tracks
        if (!this._config.Services.Subsonic.Enabled)
        {
            LogSubsonicDisabled(this._logger);
            return Result<string>.Failure("Subsonic service is disabled");
        }

        var result = await this._subsonicService.GetStreamUrlAsync(query.TrackId, cancellationToken);
        if (result.IsSuccess)
        {
            LogStreamUrlRetrieved(this._logger, query.TrackId);
        }
        else
        {
            LogStreamUrlError(this._logger, query.TrackId, result.ErrorMessage ?? "Unknown error");
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

    [LoggerMessage(EventId = 112909, Level = LogLevel.Debug, Message = "Getting stream URL for track: {TrackId}"
)]
    private static partial void LogGettingStreamUrl(ILogger logger, string trackId);

    [LoggerMessage(EventId = 112910, Level = LogLevel.Debug, Message = "Track is radio stream URL: {TrackId}"
)]
    private static partial void LogRadioStreamUrl(ILogger logger, string trackId);

    [LoggerMessage(EventId = 112911, Level = LogLevel.Warning, Message = "Subsonic service is disabled"
)]
    private static partial void LogSubsonicDisabled(ILogger logger);

    [LoggerMessage(EventId = 112912, Level = LogLevel.Debug, Message = "Retrieved stream URL for track: {TrackId}"
)]
    private static partial void LogStreamUrlRetrieved(ILogger logger, string trackId);

    [LoggerMessage(EventId = 112913, Level = LogLevel.Error, Message = "Failed → get stream URL for track: {TrackId}, error: {Error}"
)]
    private static partial void LogStreamUrlError(ILogger logger, string trackId, string error);

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
        LogGettingTrack(logger, query.TrackId);

        // For radio stations, check if it's a radio URL and create a TrackInfo
        if (IsRadioStreamUrl(query.TrackId))
        {
            var radioTrack = this.CreateRadioTrackInfo(query.TrackId);
            LogRadioTrackRetrieved(logger, query.TrackId);
            return Task.FromResult(Result<TrackInfo>.Success(radioTrack));
        }

        // Handle Subsonic tracks - for now, we don't have a direct track lookup in Subsonic service
        // This would require extending the Subsonic service or searching through playlists
        LogTrackNotImplemented(logger, query.TrackId);
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

    [LoggerMessage(EventId = 112914, Level = LogLevel.Debug, Message = "Getting track: {TrackId}"
)]
    private static partial void LogGettingTrack(ILogger logger, string trackId);

    [LoggerMessage(EventId = 112915, Level = LogLevel.Debug, Message = "Retrieved radio track: {TrackId}"
)]
    private static partial void LogRadioTrackRetrieved(ILogger logger, string trackId);

    [LoggerMessage(EventId = 112916, Level = LogLevel.Warning, Message = "Track lookup not implemented for: {TrackId}"
)]
    private static partial void LogTrackNotImplemented(ILogger logger, string trackId);

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
        LogTestingSubsonicConnection(this._logger);

        if (!this._config.Services.Subsonic.Enabled)
        {
            LogSubsonicDisabled(this._logger);
            return Result.Failure("Subsonic service is disabled");
        }

        var result = await this._subsonicService.TestConnectionAsync(cancellationToken);
        if (result.IsSuccess)
        {
            LogConnectionTestSuccessful(this._logger);
        }
        else
        {
            LogConnectionTestFailed(this._logger, result.ErrorMessage ?? "Unknown error");
        }

        return result;
    }

    #region Logging

    [LoggerMessage(EventId = 112917, Level = LogLevel.Debug, Message = "Testing Subsonic connection"
)]
    private static partial void LogTestingSubsonicConnection(ILogger logger);

    [LoggerMessage(EventId = 112918, Level = LogLevel.Warning, Message = "Subsonic service is disabled"
)]
    private static partial void LogSubsonicDisabled(ILogger logger);

    [LoggerMessage(EventId = 112919, Level = LogLevel.Information, Message = "Subsonic connection test successful"
)]
    private static partial void LogConnectionTestSuccessful(ILogger logger);

    [LoggerMessage(EventId = 112920, Level = LogLevel.Error, Message = "Subsonic connection test failed: {Error}"
)]
    private static partial void LogConnectionTestFailed(ILogger logger, string error);

    #endregion
}
