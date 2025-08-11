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
    /// Gets the index of the zone to query (1-based).
    /// </summary>
    public required int ZoneIndex { get; init; }
}

/// <summary>
/// Query to get the states of all zones.
/// </summary>
public record GetAllZoneStatesQuery : IQuery<Result<IEnumerable<ZoneState>>> { }

/// <summary>
/// Query to get the current playback state of a zone.
/// </summary>
public record GetZonePlaybackStateQuery : IQuery<Result<PlaybackState>>
{
    /// <summary>
    /// Gets the index of the zone to query (1-based).
    /// </summary>
    public required int ZoneIndex { get; init; }
}

/// <summary>
/// Query to get the current volume of a zone.
/// </summary>
public record GetZoneVolumeQuery : IQuery<Result<int>>
{
    /// <summary>
    /// Gets the index of the zone to query (1-based).
    /// </summary>
    public required int ZoneIndex { get; init; }
}
