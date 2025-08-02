namespace SnapDog2.Server.Features.Clients.Handlers;

using System.Threading;
using System.Threading.Tasks;
using Cortex.Mediator.Commands;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Models;
using SnapDog2.Server.Features.Clients.Commands;

/// <summary>
/// Handles the AssignClientToZoneCommand.
/// </summary>
public partial class AssignClientToZoneCommandHandler : ICommandHandler<AssignClientToZoneCommand, Result>
{
    private readonly IClientManager _clientManager;
    private readonly IZoneManager _zoneManager;
    private readonly ILogger<AssignClientToZoneCommandHandler> _logger;

    [LoggerMessage(3101, LogLevel.Information, "Assigning Client {ClientId} to Zone {ZoneId} from {Source}")]
    private partial void LogHandling(int clientId, int zoneId, CommandSource source);

    [LoggerMessage(3102, LogLevel.Warning, "Client {ClientId} not found for AssignClientToZoneCommand")]
    private partial void LogClientNotFound(int clientId);

    [LoggerMessage(3103, LogLevel.Warning, "Zone {ZoneId} not found for AssignClientToZoneCommand")]
    private partial void LogZoneNotFound(int zoneId);

    public AssignClientToZoneCommandHandler(
        IClientManager clientManager,
        IZoneManager zoneManager,
        ILogger<AssignClientToZoneCommandHandler> logger
    )
    {
        _clientManager = clientManager;
        _zoneManager = zoneManager;
        _logger = logger;
    }

    public async Task<Result> Handle(AssignClientToZoneCommand request, CancellationToken cancellationToken)
    {
        LogHandling(request.ClientId, request.ZoneId, request.Source);

        // Validate client exists
        var clientResult = await _clientManager.GetClientAsync(request.ClientId).ConfigureAwait(false);
        if (clientResult.IsFailure)
        {
            LogClientNotFound(request.ClientId);
            return clientResult;
        }

        // Validate zone exists
        var zoneResult = await _zoneManager.GetZoneAsync(request.ZoneId).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            LogZoneNotFound(request.ZoneId);
            return zoneResult;
        }

        // Perform the assignment
        var result = await _clientManager
            .AssignClientToZoneAsync(request.ClientId, request.ZoneId)
            .ConfigureAwait(false);

        return result;
    }
}
