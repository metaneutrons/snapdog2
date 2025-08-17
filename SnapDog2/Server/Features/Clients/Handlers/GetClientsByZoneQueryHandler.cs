namespace SnapDog2.Server.Features.Clients.Handlers;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cortex.Mediator.Queries;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Models;
using SnapDog2.Server.Features.Clients.Queries;

/// <summary>
/// Handles the GetClientsByZoneQuery.
/// </summary>
public partial class GetClientsByZoneQueryHandler(
    IClientManager clientManager,
    ILogger<GetClientsByZoneQueryHandler> logger
) : IQueryHandler<GetClientsByZoneQuery, Result<List<ClientState>>>
{
    private readonly IClientManager _clientManager = clientManager;
    private readonly ILogger<GetClientsByZoneQueryHandler> _logger = logger;

    [LoggerMessage(4201, LogLevel.Information, "Handling GetClientsByZoneQuery for Zone {ZoneIndex}")]
    private partial void LogHandling(int zoneIndex);

    public async Task<Result<List<ClientState>>> Handle(
        GetClientsByZoneQuery request,
        CancellationToken cancellationToken
    )
    {
        this.LogHandling(request.ZoneIndex);

        var result = await this._clientManager.GetClientsByZoneAsync(request.ZoneIndex).ConfigureAwait(false);
        return result;
    }
}
