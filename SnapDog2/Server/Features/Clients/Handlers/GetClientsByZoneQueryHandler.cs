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
public partial class GetClientsByZoneQueryHandler : IQueryHandler<GetClientsByZoneQuery, Result<List<ClientState>>>
{
    private readonly IClientManager _clientManager;
    private readonly ILogger<GetClientsByZoneQueryHandler> _logger;

    [LoggerMessage(4201, LogLevel.Information, "Handling GetClientsByZoneQuery for Zone {ZoneId}")]
    private partial void LogHandling(int zoneId);

    public GetClientsByZoneQueryHandler(IClientManager clientManager, ILogger<GetClientsByZoneQueryHandler> logger)
    {
        _clientManager = clientManager;
        _logger = logger;
    }

    public async Task<Result<List<ClientState>>> Handle(
        GetClientsByZoneQuery request,
        CancellationToken cancellationToken
    )
    {
        LogHandling(request.ZoneId);

        var result = await _clientManager.GetClientsByZoneAsync(request.ZoneId).ConfigureAwait(false);
        return result;
    }
}
