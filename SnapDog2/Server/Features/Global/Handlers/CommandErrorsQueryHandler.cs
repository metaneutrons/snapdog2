namespace SnapDog2.Server.Features.Global.Handlers;

using System.Threading;
using System.Threading.Tasks;
using Cortex.Mediator.Queries;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Models;
using SnapDog2.Server.Features.Global.Queries;

/// <summary>
/// Handles the CommandErrorsQuery.
/// </summary>
public partial class CommandErrorsQueryHandler : IQueryHandler<CommandErrorsQuery, Result<string[]>>
{
    private readonly ICommandStatusService _commandStatusService;
    private readonly ILogger<CommandErrorsQueryHandler> _logger;

    [LoggerMessage(5003, LogLevel.Debug, "Retrieving recent command errors")]
    private partial void LogRetrievingCommandErrors();

    [LoggerMessage(5004, LogLevel.Warning, "Failed to retrieve command errors: {ErrorMessage}")]
    private partial void LogFailedToRetrieveCommandErrors(string errorMessage);

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandErrorsQueryHandler"/> class.
    /// </summary>
    /// <param name="commandStatusService">The command status service.</param>
    /// <param name="logger">The logger instance.</param>
    public CommandErrorsQueryHandler(
        ICommandStatusService commandStatusService,
        ILogger<CommandErrorsQueryHandler> logger
    )
    {
        this._commandStatusService = commandStatusService;
        this._logger = logger;
    }

    /// <summary>
    /// Handles the CommandErrorsQuery.
    /// </summary>
    /// <param name="request">The query request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Array of recent command error messages.</returns>
    public async Task<Result<string[]>> Handle(CommandErrorsQuery request, CancellationToken cancellationToken)
    {
        this.LogRetrievingCommandErrors();

        try
        {
            var errors = await this._commandStatusService.GetRecentErrorsAsync().ConfigureAwait(false);
            return Result<string[]>.Success(errors);
        }
        catch (System.Exception ex)
        {
            this.LogFailedToRetrieveCommandErrors(ex.Message);
            return Result<string[]>.Failure($"Failed to retrieve command errors: {ex.Message}");
        }
    }
}
