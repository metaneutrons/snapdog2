namespace SnapDog2.Server.Features.Zones.Queries;

using Cortex.Mediator.Queries;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Models;

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
public record GetAllZoneStatesQuery : IQuery<Result<IEnumerable<ZoneState>>>
{
}

/// <summary>
/// Query to get the current playback state of a zone.
/// </summary>
public record GetZonePlaybackStateQuery : IQuery<Result<PlaybackStatus>>
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
