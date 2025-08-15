namespace SnapDog2.Server.Features.Global.Handlers;

using System.Threading;
using System.Threading.Tasks;
using Cortex.Mediator.Queries;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Models;
using SnapDog2.Server.Features.Global.Queries;

/// <summary>
/// Handles the GetSystemStatusQuery.
/// </summary>
public partial class GetSystemStatusQueryHandler : IQueryHandler<GetSystemStatusQuery, Result<SystemStatus>>
{
    private readonly IAppStatusService _systemStatusService;
    private readonly ILogger<GetSystemStatusQueryHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetSystemStatusQueryHandler"/> class.
    /// </summary>
    /// <param name="systemStatusService">The system status service.</param>
    /// <param name="logger">The logger instance.</param>
    public GetSystemStatusQueryHandler(
        IAppStatusService systemStatusService,
        ILogger<GetSystemStatusQueryHandler> logger
    )
    {
        this._systemStatusService = systemStatusService;
        this._logger = logger;
    }

    /// <inheritdoc/>
    public async Task<Result<SystemStatus>> Handle(GetSystemStatusQuery request, CancellationToken cancellationToken)
    {
        this.LogHandling();

        try
        {
            var status = await this._systemStatusService.GetCurrentStatusAsync().ConfigureAwait(false);
            return Result<SystemStatus>.Success(status);
        }
        catch (Exception ex)
        {
            this.LogError(ex);
            return Result<SystemStatus>.Failure(ex);
        }
    }

    [LoggerMessage(1001, LogLevel.Information, "Handling GetSystemStatusQuery")]
    private partial void LogHandling();

    [LoggerMessage(1002, LogLevel.Error, "Error retrieving system status")]
    private partial void LogError(Exception ex);
}
