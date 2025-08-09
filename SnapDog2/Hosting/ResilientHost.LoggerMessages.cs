using Microsoft.Extensions.Logging;

namespace SnapDog2.Hosting;

/// <summary>
/// High-performance LoggerMessage definitions for ResilientHost.
/// Eliminates boxing, reduces allocations, and provides compile-time safety.
/// </summary>
public partial class ResilientHost
{
    // Host Shutdown Error Operations (10501-10502)
    [LoggerMessage(10501, LogLevel.Error, "Error during host shutdown")]
    private partial void LogErrorDuringHostShutdown(Exception ex);

    [LoggerMessage(10502, LogLevel.Error, "Error during host shutdown: {ErrorType} - {ErrorMessage}")]
    private partial void LogErrorDuringHostShutdownProduction(string errorType, string errorMessage);
}
