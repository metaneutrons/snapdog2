namespace SnapDog2.Server.Features.Clients.Handlers;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cortex.Mediator.Queries;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Models;
using SnapDog2.Server.Features.Clients.Queries;

/// <summary>
/// Handles the GetAllClientsQuery.
/// </summary>
public partial class GetAllClientsQueryHandler : IQueryHandler<GetAllClientsQuery, Result<List<ClientState>>>
{
    private readonly IClientManager _clientManager;
    private readonly ILogger<GetAllClientsQueryHandler> _logger;

    [LoggerMessage(4001, LogLevel.Information, "Handling GetAllClientsQuery")]
    private partial void LogHandling();

    [LoggerMessage(4002, LogLevel.Error, "Error retrieving all clients: {ErrorMessage}")]
    private partial void LogError(string errorMessage);

    public GetAllClientsQueryHandler(IClientManager clientManager, ILogger<GetAllClientsQueryHandler> logger)
    {
        this._clientManager = clientManager;
        this._logger = logger;
    }

    public async Task<Result<List<ClientState>>> Handle(GetAllClientsQuery request, CancellationToken cancellationToken)
    {
        this.LogHandling();

        try
        {
            var result = await this._clientManager.GetAllClientsAsync().ConfigureAwait(false);
            return result;
        }
        catch (Exception ex)
        {
            this.LogError(ex.Message);
            return Result<List<ClientState>>.Failure(ex.Message ?? "An error occurred while retrieving all clients");
        }
    }
}
