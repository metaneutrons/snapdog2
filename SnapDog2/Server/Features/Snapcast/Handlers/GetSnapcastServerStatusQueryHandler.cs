namespace SnapDog2.Server.Features.Snapcast.Handlers;

using Cortex.Mediator.Queries;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Models;
using SnapDog2.Server.Features.Snapcast.Queries;

/// <summary>
/// Handler for getting Snapcast server status.
/// </summary>
public partial class GetSnapcastServerStatusQueryHandler
    : IQueryHandler<GetSnapcastServerStatusQuery, Result<SnapcastServerStatus>>
{
    private readonly ISnapcastService _snapcastService;
    private readonly ILogger<GetSnapcastServerStatusQueryHandler> _logger;

    public GetSnapcastServerStatusQueryHandler(
        ISnapcastService snapcastService,
        ILogger<GetSnapcastServerStatusQueryHandler> logger
    )
    {
        this._snapcastService = snapcastService;
        this._logger = logger;
    }

    [LoggerMessage(2001, LogLevel.Information, "Getting Snapcast server status")]
    private partial void LogGettingServerStatus();

    [LoggerMessage(2002, LogLevel.Error, "Failed to get Snapcast server status")]
    private partial void LogGetServerStatusFailed(Exception ex);

    public async Task<Result<SnapcastServerStatus>> Handle(
        GetSnapcastServerStatusQuery query,
        CancellationToken cancellationToken
    )
    {
        this.LogGettingServerStatus();

        try
        {
            var result = await this._snapcastService.GetServerStatusAsync(cancellationToken).ConfigureAwait(false);

            if (result.IsFailure)
            {
                this.LogGetServerStatusFailed(new InvalidOperationException(result.ErrorMessage ?? "Unknown error"));
                return Result<SnapcastServerStatus>.Failure(result.ErrorMessage ?? "Unknown error");
            }

            return Result<SnapcastServerStatus>.Success(result.Value!);
        }
        catch (Exception ex)
        {
            this.LogGetServerStatusFailed(ex);
            return Result<SnapcastServerStatus>.Failure(ex);
        }
    }
}
