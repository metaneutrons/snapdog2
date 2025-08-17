namespace SnapDog2.Server.Features.Global.Handlers;

using System.Threading;
using System.Threading.Tasks;
using Cortex.Mediator.Queries;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Models;
using SnapDog2.Server.Features.Global.Queries;

/// <summary>
/// Handles the CommandStatusQuery.
/// </summary>
public partial class CommandStatusQueryHandler : IQueryHandler<CommandStatusQuery, Result<string>>
{
    private readonly ICommandStatusService _commandStatusService;
    private readonly ILogger<CommandStatusQueryHandler> _logger;

    [LoggerMessage(5001, LogLevel.Debug, "Retrieving command processing status")]
    private partial void LogRetrievingCommandStatus();

    [LoggerMessage(5002, LogLevel.Warning, "Failed to retrieve command status: {ErrorMessage}")]
    private partial void LogFailedToRetrieveCommandStatus(string errorMessage);

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandStatusQueryHandler"/> class.
    /// </summary>
    /// <param name="commandStatusService">The command status service.</param>
    /// <param name="logger">The logger instance.</param>
    public CommandStatusQueryHandler(
        ICommandStatusService commandStatusService,
        ILogger<CommandStatusQueryHandler> logger
    )
    {
        this._commandStatusService = commandStatusService;
        this._logger = logger;
    }

    /// <summary>
    /// Handles the CommandStatusQuery.
    /// </summary>
    /// <param name="request">The query request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The command processing status.</returns>
    public async Task<Result<string>> Handle(CommandStatusQuery request, CancellationToken cancellationToken)
    {
        this.LogRetrievingCommandStatus();

        try
        {
            var status = await this._commandStatusService.GetStatusAsync().ConfigureAwait(false);
            return Result<string>.Success(status);
        }
        catch (System.Exception ex)
        {
            this.LogFailedToRetrieveCommandStatus(ex.Message);
            return Result<string>.Failure($"Failed to retrieve command status: {ex.Message}");
        }
    }
}
