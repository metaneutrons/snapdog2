namespace SnapDog2.Server.Features.Playlists.Queries;

using System.Collections.Generic;
using Cortex.Mediator.Queries;
using SnapDog2.Core.Models;

/// <summary>
/// Query to retrieve all available playlists (including radio stations as first playlist).
/// </summary>
public record GetAllPlaylistsQuery : IQuery<Result<List<PlaylistInfo>>>;

/// <summary>
/// Query to retrieve a specific playlist with all its tracks.
/// </summary>
public record GetPlaylistQuery : IQuery<Result<Api.Models.PlaylistWithTracks>>
{
    /// <summary>
    /// Gets the playlist identifier.
    /// </summary>
    public required string PlaylistIndex { get; init; }
}

/// <summary>
/// Query to get the streaming URL for a specific track.
/// </summary>
public record GetStreamUrlQuery : IQuery<Result<string>>
{
    /// <summary>
    /// Gets the track identifier.
    /// </summary>
    public required string TrackId { get; init; }
}

/// <summary>
/// Query to test Subsonic server connection.
/// </summary>
public record TestSubsonicConnectionQuery : IQuery<Result>;
