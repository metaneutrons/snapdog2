namespace SnapDog2.Server.Features.Clients.Handlers;

using System.Threading;
using System.Threading.Tasks;
using Cortex.Mediator.Queries;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Models;
using SnapDog2.Server.Features.Clients.Queries;

/// <summary>
/// Handles the GetClientQuery.
/// </summary>
public partial class GetClientQueryHandler : IQueryHandler<GetClientQuery, Result<ClientState>>
{
    private readonly IClientManager _clientManager;
    private readonly ILogger<GetClientQueryHandler> _logger;

    [LoggerMessage(4101, LogLevel.Information, "Handling GetClientQuery for Client {ClientIndex}")]
    private partial void LogHandling(int clientIndex);

    public GetClientQueryHandler(IClientManager clientManager, ILogger<GetClientQueryHandler> logger)
    {
        this._clientManager = clientManager;
        this._logger = logger;
    }

    public async Task<Result<ClientState>> Handle(GetClientQuery request, CancellationToken cancellationToken)
    {
        this.LogHandling(request.ClientIndex);

        var result = await this._clientManager.GetClientStateAsync(request.ClientIndex).ConfigureAwait(false);
        return result;
    }
}
