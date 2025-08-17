namespace SnapDog2.Server.Features.Playlists.Handlers;

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cortex.Mediator.Queries;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Configuration;
using SnapDog2.Core.Models;
using SnapDog2.Server.Features.Playlists.Queries;

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
        var radioStations = this._config.RadioStations ?? new List<RadioStationConfig>();

        return new PlaylistInfo
        {
            Id = "radio",
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

    [LoggerMessage(2920, LogLevel.Debug, "Getting all playlists (radio + subsonic)")]
    private static partial void LogGettingAllPlaylists(ILogger logger);

    [LoggerMessage(2921, LogLevel.Information, "Added {Count} Subsonic playlists")]
    private static partial void LogSubsonicPlaylistsAdded(ILogger logger, int count);

    [LoggerMessage(2922, LogLevel.Warning, "Failed to get Subsonic playlists: {Error}")]
    private static partial void LogSubsonicPlaylistsError(ILogger logger, string error);

    [LoggerMessage(2923, LogLevel.Information, "Retrieved {Count} total playlists")]
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
) : IQueryHandler<GetPlaylistQuery, Result<Api.Models.PlaylistWithTracks>>
{
    private readonly ISubsonicService _subsonicService = subsonicService;
    private readonly SnapDogConfiguration _config = configOptions.Value;
    private readonly ILogger<GetPlaylistQueryHandler> _logger = logger;

    public async Task<Result<Api.Models.PlaylistWithTracks>> Handle(
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
            return Result<Api.Models.PlaylistWithTracks>.Failure("Subsonic service is disabled");
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
            return Result<Api.Models.PlaylistWithTracks>.Failure(
                $"Failed to get Subsonic playlists: {subsonicPlaylistsResult.ErrorMessage}"
            );
        }

        var subsonicPlaylists = subsonicPlaylistsResult.Value ?? new List<PlaylistInfo>();

        // Find the playlist at the requested index (index 2 = first Subsonic playlist, index 3 = second, etc.)
        var subsonicIndex = query.PlaylistIndex - 2; // Convert to 0-based index for Subsonic playlists
        if (subsonicIndex < 0 || subsonicIndex >= subsonicPlaylists.Count)
        {
            LogPlaylistError(this._logger, query.PlaylistIndex.ToString(), "Playlist index out of range");
            return Result<Api.Models.PlaylistWithTracks>.Failure($"Playlist {query.PlaylistIndex} not found");
        }

        var targetPlaylist = subsonicPlaylists[subsonicIndex];

        // Get the full playlist with tracks using the Subsonic ID
        var result = await this._subsonicService.GetPlaylistAsync(targetPlaylist.Id, cancellationToken);
        if (result.IsSuccess)
        {
            LogPlaylistRetrieved(this._logger, query.PlaylistIndex.ToString(), result.Value?.Tracks?.Count ?? 0);
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
    private Result<Api.Models.PlaylistWithTracks> CreateRadioPlaylistWithTracks()
    {
        var radioStations = this._config.RadioStations ?? new List<RadioStationConfig>();

        var playlist = new PlaylistInfo
        {
            Id = "radio",
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
                        Id = station.Url ?? string.Empty,
                        Title = station.Name ?? "Unknown Station",
                        Artist = "Radio",
                        Album = "Radio Stations",
                        DurationMs = null, // Radio streams don't have duration
                        PositionMs = 0,
                        CoverArtUrl = null,
                        Source = "radio",
                    }
            )
            .ToList();

        var playlistWithTracks = new Api.Models.PlaylistWithTracks(playlist, tracks);

        LogRadioPlaylistCreated(this._logger, tracks.Count);
        return Result<Api.Models.PlaylistWithTracks>.Success(playlistWithTracks);
    }

    #region Logging

    [LoggerMessage(2930, LogLevel.Debug, "Getting playlist: {PlaylistIndex}")]
    private static partial void LogGettingPlaylist(ILogger logger, int playlistIndex);

    [LoggerMessage(2931, LogLevel.Warning, "Subsonic service is disabled")]
    private static partial void LogSubsonicDisabled(ILogger logger);

    [LoggerMessage(2932, LogLevel.Information, "Retrieved playlist: {PlaylistIndex} with {TrackCount} tracks")]
    private static partial void LogPlaylistRetrieved(ILogger logger, string playlistIndex, int trackCount);

    [LoggerMessage(2933, LogLevel.Error, "Failed to get playlist: {PlaylistIndex}, error: {Error}")]
    private static partial void LogPlaylistError(ILogger logger, string playlistIndex, string error);

    [LoggerMessage(2934, LogLevel.Debug, "Created radio playlist with {TrackCount} stations")]
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

    [LoggerMessage(2940, LogLevel.Debug, "Getting stream URL for track: {TrackId}")]
    private static partial void LogGettingStreamUrl(ILogger logger, string trackId);

    [LoggerMessage(2941, LogLevel.Debug, "Track is radio stream URL: {TrackId}")]
    private static partial void LogRadioStreamUrl(ILogger logger, string trackId);

    [LoggerMessage(2942, LogLevel.Warning, "Subsonic service is disabled")]
    private static partial void LogSubsonicDisabled(ILogger logger);

    [LoggerMessage(2943, LogLevel.Debug, "Retrieved stream URL for track: {TrackId}")]
    private static partial void LogStreamUrlRetrieved(ILogger logger, string trackId);

    [LoggerMessage(2944, LogLevel.Error, "Failed to get stream URL for track: {TrackId}, error: {Error}")]
    private static partial void LogStreamUrlError(ILogger logger, string trackId, string error);

    #endregion
}

/// <summary>
/// Handler for getting track details.
/// </summary>
public partial class GetTrackQueryHandler(
    ISubsonicService subsonicService,
    IOptions<SnapDogConfiguration> configOptions,
    ILogger<GetTrackQueryHandler> logger
) : IQueryHandler<GetTrackQuery, Result<TrackInfo>>
{
    private readonly ISubsonicService _subsonicService = subsonicService;
    private readonly SnapDogConfiguration _config = configOptions.Value;
    private readonly ILogger<GetTrackQueryHandler> _logger = logger;

    public Task<Result<TrackInfo>> Handle(GetTrackQuery query, CancellationToken cancellationToken)
    {
        LogGettingTrack(this._logger, query.TrackId);

        // For radio stations, check if it's a radio URL and create a TrackInfo
        if (IsRadioStreamUrl(query.TrackId))
        {
            var radioTrack = this.CreateRadioTrackInfo(query.TrackId);
            LogRadioTrackRetrieved(this._logger, query.TrackId);
            return Task.FromResult(Result<TrackInfo>.Success(radioTrack));
        }

        // Handle Subsonic tracks - for now, we don't have a direct track lookup in Subsonic service
        // This would require extending the Subsonic service or searching through playlists
        LogTrackNotImplemented(this._logger, query.TrackId);
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
        var radioStations = this._config.RadioStations ?? new List<RadioStationConfig>();
        var station = radioStations.FirstOrDefault(s => s.Url == url);

        return new TrackInfo
        {
            Index = 1,
            Id = url,
            Title = station?.Name ?? "Unknown Radio Station",
            Artist = "Radio",
            Album = "Radio Stations",
            DurationMs = null, // Radio streams don't have duration
            PositionMs = 0,
            CoverArtUrl = null,
            Source = "radio",
        };
    }

    #region Logging

    [LoggerMessage(2960, LogLevel.Debug, "Getting track: {TrackId}")]
    private static partial void LogGettingTrack(ILogger logger, string trackId);

    [LoggerMessage(2961, LogLevel.Debug, "Retrieved radio track: {TrackId}")]
    private static partial void LogRadioTrackRetrieved(ILogger logger, string trackId);

    [LoggerMessage(2962, LogLevel.Warning, "Track lookup not implemented for: {TrackId}")]
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

    [LoggerMessage(2950, LogLevel.Debug, "Testing Subsonic connection")]
    private static partial void LogTestingSubsonicConnection(ILogger logger);

    [LoggerMessage(2951, LogLevel.Warning, "Subsonic service is disabled")]
    private static partial void LogSubsonicDisabled(ILogger logger);

    [LoggerMessage(2952, LogLevel.Information, "Subsonic connection test successful")]
    private static partial void LogConnectionTestSuccessful(ILogger logger);

    [LoggerMessage(2953, LogLevel.Error, "Subsonic connection test failed: {Error}")]
    private static partial void LogConnectionTestFailed(ILogger logger, string error);

    #endregion
}
