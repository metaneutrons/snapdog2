namespace SnapDog2.Server.Features.Clients.Handlers;

using System.Threading;
using System.Threading.Tasks;
using Cortex.Mediator.Commands;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Models;
using SnapDog2.Server.Features.Clients.Commands.Config;

/// <summary>
/// Handles the AssignClientToZoneCommand.
/// </summary>
public partial class AssignClientToZoneCommandHandler(
    IClientManager clientManager,
    IZoneManager zoneManager,
    ILogger<AssignClientToZoneCommandHandler> logger
) : ICommandHandler<AssignClientToZoneCommand, Result>
{
    private readonly IClientManager _clientManager = clientManager;
    private readonly IZoneManager _zoneManager = zoneManager;
    private readonly ILogger<AssignClientToZoneCommandHandler> _logger = logger;

    [LoggerMessage(3101, LogLevel.Information, "Assigning Client {ClientIndex} to Zone {ZoneIndex} from {Source}")]
    private partial void LogHandling(int clientIndex, int zoneIndex, CommandSource source);

    [LoggerMessage(3102, LogLevel.Warning, "Client {ClientIndex} not found for AssignClientToZoneCommand")]
    private partial void LogClientNotFound(int clientIndex);

    [LoggerMessage(3103, LogLevel.Warning, "Zone {ZoneIndex} not found for AssignClientToZoneCommand")]
    private partial void LogZoneNotFound(int zoneIndex);

    public async Task<Result> Handle(AssignClientToZoneCommand request, CancellationToken cancellationToken)
    {
        this.LogHandling(request.ClientIndex, request.ZoneIndex, request.Source);

        // Validate client exists
        var clientResult = await this._clientManager.GetClientAsync(request.ClientIndex).ConfigureAwait(false);
        if (clientResult.IsFailure)
        {
            this.LogClientNotFound(request.ClientIndex);
            return clientResult;
        }

        var client = clientResult.Value!;

        // Validate zone exists
        var zoneResult = await this._zoneManager.GetZoneAsync(request.ZoneIndex).ConfigureAwait(false);
        if (zoneResult.IsFailure)
        {
            this.LogZoneNotFound(request.ZoneIndex);
            return zoneResult;
        }

        // Use the IClient method directly
        var result = await client.AssignToZoneAsync(request.ZoneIndex).ConfigureAwait(false);

        return result;
    }
}
