using Microsoft.Extensions.Logging;

namespace SnapDog2.Services;

/// <summary>
/// High-performance LoggerMessage definitions for StatePublishingService.
/// Eliminates boxing, reduces allocations, and provides compile-time safety.
/// </summary>
public partial class StatePublishingService
{
    // State Publishing Operations (10701)
    [LoggerMessage(10701, LogLevel.Information, "State publishing cancelled during shutdown")]
    private partial void LogStatePublishingCancelledDuringShutdown();
}
