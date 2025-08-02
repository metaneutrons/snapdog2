namespace SnapDog2.Server.Features.Zones.Handlers;

using System.Threading;
using System.Threading.Tasks;
using Cortex.Mediator.Queries;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Models;
using SnapDog2.Server.Features.Zones.Queries;

/// <summary>
/// Handles the GetZoneStateQuery.
/// </summary>
public class GetZoneStateQueryHandler : IQueryHandler<GetZoneStateQuery, Result<ZoneState>>
{
    private readonly IZoneManager _zoneManager;
    private readonly ILogger<GetZoneStateQueryHandler> _logger;

    public GetZoneStateQueryHandler(IZoneManager zoneManager, ILogger<GetZoneStateQueryHandler> logger)
    {
        _zoneManager = zoneManager;
        _logger = logger;
    }

    public async Task<Result<ZoneState>> Handle(GetZoneStateQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting state for Zone {ZoneId}", request.ZoneId);

        var zoneResult = await _zoneManager.GetZoneAsync(request.ZoneId).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            _logger.LogWarning("Zone {ZoneId} not found for GetZoneStateQuery", request.ZoneId);
            return Result<ZoneState>.Failure(zoneResult.ErrorMessage);
        }

        var zone = zoneResult.Value;
        return await zone.GetStateAsync().ConfigureAwait(false);
    }
}

/// <summary>
/// Handles the GetAllZoneStatesQuery.
/// </summary>
public class GetAllZoneStatesQueryHandler : IQueryHandler<GetAllZoneStatesQuery, Result<IEnumerable<ZoneState>>>
{
    private readonly IZoneManager _zoneManager;
    private readonly ILogger<GetAllZoneStatesQueryHandler> _logger;

    public GetAllZoneStatesQueryHandler(IZoneManager zoneManager, ILogger<GetAllZoneStatesQueryHandler> logger)
    {
        _zoneManager = zoneManager;
        _logger = logger;
    }

    public async Task<Result<IEnumerable<ZoneState>>> Handle(GetAllZoneStatesQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting states for all zones");

        var zonesResult = await _zoneManager.GetAllZonesAsync().ConfigureAwait(false);
        if (zonesResult.IsFailure)
        {
            return Result<IEnumerable<ZoneState>>.Failure(zonesResult.ErrorMessage);
        }

        var zones = zonesResult.Value;
        var states = new List<ZoneState>();

        foreach (var zone in zones)
        {
            var stateResult = await zone.GetStateAsync().ConfigureAwait(false);
            if (stateResult.IsSuccess)
            {
                states.Add(stateResult.Value);
            }
        }

        return Result<IEnumerable<ZoneState>>.Success(states);
    }
}

/// <summary>
/// Handles the GetZonePlaybackStateQuery.
/// </summary>
public class GetZonePlaybackStateQueryHandler : IQueryHandler<GetZonePlaybackStateQuery, Result<PlaybackStatus>>
{
    private readonly IZoneManager _zoneManager;
    private readonly ILogger<GetZonePlaybackStateQueryHandler> _logger;

    public GetZonePlaybackStateQueryHandler(IZoneManager zoneManager, ILogger<GetZonePlaybackStateQueryHandler> logger)
    {
        _zoneManager = zoneManager;
        _logger = logger;
    }

    public async Task<Result<PlaybackStatus>> Handle(GetZonePlaybackStateQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting playback state for Zone {ZoneId}", request.ZoneId);

        var zoneResult = await _zoneManager.GetZoneAsync(request.ZoneId).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            _logger.LogWarning("Zone {ZoneId} not found for GetZonePlaybackStateQuery", request.ZoneId);
            return Result<PlaybackStatus>.Failure(zoneResult.ErrorMessage);
        }

        var zone = zoneResult.Value;
        var stateResult = await zone.GetStateAsync().ConfigureAwait(false);
        
        if (stateResult.IsFailure)
        {
            return Result<PlaybackStatus>.Failure(stateResult.ErrorMessage);
        }

        return Result<PlaybackStatus>.Success(Enum.Parse<PlaybackStatus>(stateResult.Value.PlaybackState, true));
    }
}

/// <summary>
/// Handles the GetZoneVolumeQuery.
/// </summary>
public class GetZoneVolumeQueryHandler : IQueryHandler<GetZoneVolumeQuery, Result<int>>
{
    private readonly IZoneManager _zoneManager;
    private readonly ILogger<GetZoneVolumeQueryHandler> _logger;

    public GetZoneVolumeQueryHandler(IZoneManager zoneManager, ILogger<GetZoneVolumeQueryHandler> logger)
    {
        _zoneManager = zoneManager;
        _logger = logger;
    }

    public async Task<Result<int>> Handle(GetZoneVolumeQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting volume for Zone {ZoneId}", request.ZoneId);

        var zoneResult = await _zoneManager.GetZoneAsync(request.ZoneId).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            _logger.LogWarning("Zone {ZoneId} not found for GetZoneVolumeQuery", request.ZoneId);
            return Result<int>.Failure(zoneResult.ErrorMessage);
        }

        var zone = zoneResult.Value;
        var stateResult = await zone.GetStateAsync().ConfigureAwait(false);
        
        if (stateResult.IsFailure)
        {
            return Result<int>.Failure(stateResult.ErrorMessage);
        }

        return Result<int>.Success(stateResult.Value.Volume);
    }
}
