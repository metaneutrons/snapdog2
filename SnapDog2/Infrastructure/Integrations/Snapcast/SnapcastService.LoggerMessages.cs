using Microsoft.Extensions.Logging;

namespace SnapDog2.Infrastructure.Integrations.Snapcast;

/// <summary>
/// High-performance LoggerMessage definitions for SnapcastService.
/// Eliminates boxing, reduces allocations, and provides compile-time safety.
/// </summary>
public partial class SnapcastService
{
    // Service Disposal Operations (10801)
    [LoggerMessage(10801, LogLevel.Error, "Error during SnapcastService disposal")]
    private partial void LogErrorDuringSnapcastServiceDisposal(Exception ex);
}
