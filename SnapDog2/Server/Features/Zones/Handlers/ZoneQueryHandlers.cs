namespace SnapDog2.Server.Features.Zones.Handlers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cortex.Mediator.Queries;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Models;
using SnapDog2.Server.Features.Zones.Queries;

/// <summary>
/// Handles the GetAllZonesQuery.
/// </summary>
public partial class GetAllZonesQueryHandler(IZoneManager zoneManager, ILogger<GetAllZonesQueryHandler> logger)
    : IQueryHandler<GetAllZonesQuery, Result<List<ZoneState>>>
{
    private readonly IZoneManager _zoneManager = zoneManager;
    private readonly ILogger<GetAllZonesQueryHandler> _logger = logger;

    [LoggerMessage(5001, LogLevel.Information, "Handling GetAllZonesQuery")]
    private partial void LogHandling();

    [LoggerMessage(5002, LogLevel.Error, "Error retrieving all zones: {ErrorMessage}")]
    private partial void LogError(string errorMessage);

    public async Task<Result<List<ZoneState>>> Handle(GetAllZonesQuery request, CancellationToken cancellationToken)
    {
        this.LogHandling();

        try
        {
            var result = await this._zoneManager.GetAllZoneStatesAsync().ConfigureAwait(false);
            return result;
        }
        catch (Exception ex)
        {
            this.LogError(ex.Message);
            return Result<List<ZoneState>>.Failure(ex.Message ?? "An error occurred while retrieving all zones");
        }
    }
}

/// <summary>
/// Handles the GetZoneStateQuery.
/// </summary>
public partial class GetZoneStateQueryHandler(IZoneManager zoneManager, ILogger<GetZoneStateQueryHandler> logger)
    : IQueryHandler<GetZoneStateQuery, Result<ZoneState>>
{
    private readonly IZoneManager _zoneManager = zoneManager;
    private readonly ILogger<GetZoneStateQueryHandler> _logger = logger;

    [LoggerMessage(5101, LogLevel.Information, "Handling GetZoneStateQuery for Zone {ZoneIndex}")]
    private partial void LogHandling(int zoneIndex);

    [LoggerMessage(5102, LogLevel.Warning, "Zone {ZoneIndex} not found for GetZoneStateQuery")]
    private partial void LogZoneNotFound(int zoneIndex);

    public async Task<Result<ZoneState>> Handle(GetZoneStateQuery request, CancellationToken cancellationToken)
    {
        this.LogHandling(request.ZoneIndex);

        var result = await this._zoneManager.GetZoneStateAsync(request.ZoneIndex).ConfigureAwait(false);

        if (result.IsFailure)
        {
            this.LogZoneNotFound(request.ZoneIndex);
        }

        return result;
    }
}

/// <summary>
/// Handles the GetAllZoneStatesQuery.
/// </summary>
public partial class GetAllZoneStatesQueryHandler(
    IZoneManager zoneManager,
    ILogger<GetAllZoneStatesQueryHandler> logger
) : IQueryHandler<GetAllZoneStatesQuery, Result<IEnumerable<ZoneState>>>
{
    private readonly IZoneManager _zoneManager = zoneManager;
    private readonly ILogger<GetAllZoneStatesQueryHandler> _logger = logger;

    [LoggerMessage(5201, LogLevel.Information, "Handling GetAllZoneStatesQuery")]
    private partial void LogHandling();

    [LoggerMessage(5202, LogLevel.Error, "Error retrieving all zone states: {ErrorMessage}")]
    private partial void LogError(string errorMessage);

    public async Task<Result<IEnumerable<ZoneState>>> Handle(
        GetAllZoneStatesQuery request,
        CancellationToken cancellationToken
    )
    {
        this.LogHandling();

        try
        {
            var result = await this._zoneManager.GetAllZoneStatesAsync().ConfigureAwait(false);
            if (result.IsSuccess)
            {
                return Result<IEnumerable<ZoneState>>.Success(result.Value!.AsEnumerable());
            }

            return Result<IEnumerable<ZoneState>>.Failure(result.ErrorMessage ?? "Failed to get zone states");
        }
        catch (Exception ex)
        {
            this.LogError(ex.Message);
            return Result<IEnumerable<ZoneState>>.Failure(
                ex.Message ?? "An error occurred while retrieving all zone states"
            );
        }
    }
}

/// <summary>
/// Handles the GetZonePlaybackStateQuery.
/// </summary>
public partial class GetZonePlaybackStateQueryHandler(
    IZoneManager zoneManager,
    ILogger<GetZonePlaybackStateQueryHandler> logger
) : IQueryHandler<GetZonePlaybackStateQuery, Result<SnapDog2.Core.Enums.PlaybackState>>
{
    private readonly IZoneManager _zoneManager = zoneManager;
    private readonly ILogger<GetZonePlaybackStateQueryHandler> _logger = logger;

    [LoggerMessage(5301, LogLevel.Information, "Handling GetZonePlaybackStateQuery for Zone {ZoneIndex}")]
    private partial void LogHandling(int zoneIndex);

    [LoggerMessage(5302, LogLevel.Warning, "Zone {ZoneIndex} not found for GetZonePlaybackStateQuery")]
    private partial void LogZoneNotFound(int zoneIndex);

    public async Task<Result<SnapDog2.Core.Enums.PlaybackState>> Handle(
        GetZonePlaybackStateQuery request,
        CancellationToken cancellationToken
    )
    {
        this.LogHandling(request.ZoneIndex);

        var zoneResult = await this._zoneManager.GetZoneAsync(request.ZoneIndex).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            this.LogZoneNotFound(request.ZoneIndex);
            return SnapDog2.Core.Models.Result<SnapDog2.Core.Enums.PlaybackState>.Failure(
                zoneResult.ErrorMessage ?? "Zone not found"
            );
        }

        var zone = zoneResult.Value!;
        var stateResult = await zone.GetStateAsync().ConfigureAwait(false);

        if (stateResult.IsFailure)
        {
            return SnapDog2.Core.Models.Result<SnapDog2.Core.Enums.PlaybackState>.Failure(
                stateResult.ErrorMessage ?? "Failed to get zone state"
            );
        }

        return SnapDog2.Core.Models.Result<SnapDog2.Core.Enums.PlaybackState>.Success(
            Enum.Parse<SnapDog2.Core.Enums.PlaybackState>(stateResult.Value!.PlaybackState, true)
        );
    }
}

/// <summary>
/// Handles the GetZoneVolumeQuery.
/// </summary>
public partial class GetZoneVolumeQueryHandler(IZoneManager zoneManager, ILogger<GetZoneVolumeQueryHandler> logger)
    : IQueryHandler<GetZoneVolumeQuery, Result<int>>
{
    private readonly IZoneManager _zoneManager = zoneManager;
    private readonly ILogger<GetZoneVolumeQueryHandler> _logger = logger;

    [LoggerMessage(5401, LogLevel.Information, "Handling GetZoneVolumeQuery for Zone {ZoneIndex}")]
    private partial void LogHandling(int zoneIndex);

    [LoggerMessage(5402, LogLevel.Warning, "Zone {ZoneIndex} not found for GetZoneVolumeQuery")]
    private partial void LogZoneNotFound(int zoneIndex);

    public async Task<Result<int>> Handle(GetZoneVolumeQuery request, CancellationToken cancellationToken)
    {
        this.LogHandling(request.ZoneIndex);

        var zoneResult = await this._zoneManager.GetZoneAsync(request.ZoneIndex).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            this.LogZoneNotFound(request.ZoneIndex);
            return Result<int>.Failure(zoneResult.ErrorMessage ?? "Zone not found");
        }

        var zone = zoneResult.Value!;
        var stateResult = await zone.GetStateAsync().ConfigureAwait(false);

        if (stateResult.IsFailure)
        {
            return Result<int>.Failure(stateResult.ErrorMessage ?? "Failed to get zone state");
        }

        return Result<int>.Success(stateResult.Value!.Volume);
    }
}

/// <summary>
/// Handles the GetZoneTrackInfoQuery.
/// </summary>
public partial class GetZoneTrackInfoQueryHandler(
    IZoneManager zoneManager,
    ILogger<GetZoneTrackInfoQueryHandler> logger
) : IQueryHandler<GetZoneTrackInfoQuery, Result<TrackInfo>>
{
    private readonly IZoneManager _zoneManager = zoneManager;
    private readonly ILogger<GetZoneTrackInfoQueryHandler> _logger = logger;

    [LoggerMessage(5501, LogLevel.Information, "Handling GetZoneTrackInfoQuery for Zone {ZoneIndex}")]
    private partial void LogHandling(int zoneIndex);

    [LoggerMessage(5502, LogLevel.Warning, "Zone {ZoneIndex} not found for GetZoneTrackInfoQuery")]
    private partial void LogZoneNotFound(int zoneIndex);

    public async Task<Result<TrackInfo>> Handle(GetZoneTrackInfoQuery request, CancellationToken cancellationToken)
    {
        this.LogHandling(request.ZoneIndex);

        var zoneResult = await this._zoneManager.GetZoneAsync(request.ZoneIndex).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            this.LogZoneNotFound(request.ZoneIndex);
            return Result<TrackInfo>.Failure(zoneResult.ErrorMessage ?? "Zone not found");
        }

        var zone = zoneResult.Value!;
        var stateResult = await zone.GetStateAsync().ConfigureAwait(false);

        if (stateResult.IsFailure)
        {
            return Result<TrackInfo>.Failure(stateResult.ErrorMessage ?? "Failed to get zone state");
        }

        var track = stateResult.Value!.Track;
        if (track == null)
        {
            return Result<TrackInfo>.Failure("No track information available");
        }

        return Result<TrackInfo>.Success(track);
    }
}

/// <summary>
/// Handles the GetZonePlaylistInfoQuery.
/// </summary>
public partial class GetZonePlaylistInfoQueryHandler(
    IZoneManager zoneManager,
    ILogger<GetZonePlaylistInfoQueryHandler> logger
) : IQueryHandler<GetZonePlaylistInfoQuery, Result<PlaylistInfo>>
{
    private readonly IZoneManager _zoneManager = zoneManager;
    private readonly ILogger<GetZonePlaylistInfoQueryHandler> _logger = logger;

    [LoggerMessage(5601, LogLevel.Information, "Handling GetZonePlaylistInfoQuery for Zone {ZoneIndex}")]
    private partial void LogHandling(int zoneIndex);

    [LoggerMessage(5602, LogLevel.Warning, "Zone {ZoneIndex} not found for GetZonePlaylistInfoQuery")]
    private partial void LogZoneNotFound(int zoneIndex);

    public async Task<Result<PlaylistInfo>> Handle(
        GetZonePlaylistInfoQuery request,
        CancellationToken cancellationToken
    )
    {
        this.LogHandling(request.ZoneIndex);

        var zoneResult = await this._zoneManager.GetZoneAsync(request.ZoneIndex).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            this.LogZoneNotFound(request.ZoneIndex);
            return Result<PlaylistInfo>.Failure(zoneResult.ErrorMessage ?? "Zone not found");
        }

        var zone = zoneResult.Value!;
        var stateResult = await zone.GetStateAsync().ConfigureAwait(false);

        if (stateResult.IsFailure)
        {
            return Result<PlaylistInfo>.Failure(stateResult.ErrorMessage ?? "Failed to get zone state");
        }

        var playlist = stateResult.Value!.Playlist;
        if (playlist == null)
        {
            return Result<PlaylistInfo>.Failure("No playlist information available");
        }

        return Result<PlaylistInfo>.Success(playlist);
    }
}

/// <summary>
/// Handles the GetAllPlaylistsQuery.
/// </summary>
public partial class GetAllPlaylistsQueryHandler(
    IPlaylistManager playlistManager,
    ILogger<GetAllPlaylistsQueryHandler> logger
) : IQueryHandler<GetAllPlaylistsQuery, Result<List<PlaylistInfo>>>
{
    private readonly IPlaylistManager _playlistManager = playlistManager;
    private readonly ILogger<GetAllPlaylistsQueryHandler> _logger = logger;

    [LoggerMessage(5701, LogLevel.Information, "Handling GetAllPlaylistsQuery")]
    private partial void LogHandling();

    [LoggerMessage(5702, LogLevel.Error, "Error retrieving all playlists: {ErrorMessage}")]
    private partial void LogError(string errorMessage);

    public async Task<Result<List<PlaylistInfo>>> Handle(
        GetAllPlaylistsQuery request,
        CancellationToken cancellationToken
    )
    {
        this.LogHandling();

        try
        {
            var result = await this._playlistManager.GetAllPlaylistsAsync().ConfigureAwait(false);
            return result;
        }
        catch (Exception ex)
        {
            this.LogError(ex.Message);
            return Result<List<PlaylistInfo>>.Failure(ex.Message ?? "An error occurred while retrieving all playlists");
        }
    }
}

/// <summary>
/// Handles the GetPlaylistTracksQuery.
/// </summary>
public partial class GetPlaylistTracksQueryHandler(
    IPlaylistManager playlistManager,
    ILogger<GetPlaylistTracksQueryHandler> logger
) : IQueryHandler<GetPlaylistTracksQuery, Result<List<TrackInfo>>>
{
    private readonly IPlaylistManager _playlistManager = playlistManager;
    private readonly ILogger<GetPlaylistTracksQueryHandler> _logger = logger;

    [LoggerMessage(5801, LogLevel.Information, "Handling GetPlaylistTracksQuery for PlaylistIndex: {PlaylistIndex}")]
    private partial void LogHandling(int playlistIndex);

    [LoggerMessage(5802, LogLevel.Warning, "PlaylistIndex is required for GetPlaylistTracksQuery")]
    private partial void LogMissingParameters();

    [LoggerMessage(5803, LogLevel.Error, "Error retrieving playlist tracks: {ErrorMessage}")]
    private partial void LogError(string errorMessage);

    public async Task<Result<List<TrackInfo>>> Handle(
        GetPlaylistTracksQuery request,
        CancellationToken cancellationToken
    )
    {
        this.LogHandling(request.PlaylistIndex);

        try
        {
            return await this
                ._playlistManager.GetPlaylistTracksByIndexAsync(request.PlaylistIndex)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this.LogError(ex.Message);
            return Result<List<TrackInfo>>.Failure(ex.Message ?? "An error occurred while retrieving playlist tracks");
        }
    }
}
