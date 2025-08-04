namespace SnapDog2.Server.Features.Zones.Queries;

using System.Collections.Generic;
using Cortex.Mediator.Queries;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Models;

/// <summary>
/// Query to retrieve the state of all zones.
/// </summary>
public record GetAllZonesQuery : IQuery<Result<List<ZoneState>>>;

/// <summary>
/// Query to get the current state of a specific zone.
/// </summary>
public record GetZoneStateQuery : IQuery<Result<ZoneState>>
{
    /// <summary>
    /// Gets the ID of the zone to query.
    /// </summary>
    public required int ZoneId { get; init; }
}

/// <summary>
/// Query to get the states of all zones.
/// </summary>
public record GetAllZoneStatesQuery : IQuery<Result<IEnumerable<ZoneState>>> { }

/// <summary>
/// Query to get the current playback state of a zone.
/// </summary>
public record GetZonePlaybackStateQuery : IQuery<Result<SnapDog2.Core.Enums.PlaybackState>>
{
    /// <summary>
    /// Gets the ID of the zone to query.
    /// </summary>
    public required int ZoneId { get; init; }
}

/// <summary>
/// Query to get the current volume of a zone.
/// </summary>
public record GetZoneVolumeQuery : IQuery<Result<int>>
{
    /// <summary>
    /// Gets the ID of the zone to query.
    /// </summary>
    public required int ZoneId { get; init; }
}

/// <summary>
/// Query to retrieve the current track information for a zone.
/// </summary>
public record GetZoneTrackInfoQuery : IQuery<Result<TrackInfo>>
{
    /// <summary>
    /// Gets the ID of the zone.
    /// </summary>
    public required int ZoneId { get; init; }
}

/// <summary>
/// Query to retrieve the current playlist information for a zone.
/// </summary>
public record GetZonePlaylistInfoQuery : IQuery<Result<PlaylistInfo>>
{
    /// <summary>
    /// Gets the ID of the zone.
    /// </summary>
    public required int ZoneId { get; init; }
}

/// <summary>
/// Query to retrieve all available playlists.
/// </summary>
public record GetAllPlaylistsQuery : IQuery<Result<List<PlaylistInfo>>>;

/// <summary>
/// Query to retrieve tracks for a specific playlist.
/// </summary>
public record GetPlaylistTracksQuery : IQuery<Result<List<TrackInfo>>>
{
    /// <summary>
    /// Gets the playlist ID or index (1-based).
    /// </summary>
    public string? PlaylistId { get; init; }

    /// <summary>
    /// Gets the playlist index (1-based, alternative to ID).
    /// </summary>
    public int? PlaylistIndex { get; init; }
}
