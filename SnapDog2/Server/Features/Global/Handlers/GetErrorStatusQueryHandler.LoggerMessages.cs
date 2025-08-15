using Microsoft.Extensions.Logging;

namespace SnapDog2.Server.Features.Global.Handlers;

/// <summary>
/// High-performance LoggerMessage definitions for GetErrorStatusQueryHandler.
/// Eliminates boxing, reduces allocations, and provides compile-time safety.
/// </summary>
public partial class GetErrorStatusQueryHandler
{
    // Error Status Query Operations (10201-10203)
    [LoggerMessage(10201, LogLevel.Debug, "Getting latest system error status")]
    private partial void LogGettingLatestSystemErrorStatus();

    [LoggerMessage(10202, LogLevel.Debug, "Successfully retrieved error status: {HasError}")]
    private partial void LogSuccessfullyRetrievedErrorStatus(bool hasError);

    [LoggerMessage(10203, LogLevel.Error, "Failed to get error status")]
    private partial void LogFailedToGetErrorStatus(Exception ex);
}
