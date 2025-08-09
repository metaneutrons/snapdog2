using Microsoft.Extensions.Logging;

namespace KnxMonitor.Services;

/// <summary>
/// High-performance LoggerMessage definitions for DisplayService.
/// Eliminates boxing, reduces allocations, and provides compile-time safety.
/// </summary>
public partial class DisplayService
{
    // Display Error Operations (10401-10403)
    [LoggerMessage(10401, LogLevel.Error, "Error updating display")]
    private partial void LogErrorUpdatingDisplay(Exception ex);

    [LoggerMessage(10402, LogLevel.Error, "Error stopping display service")]
    private partial void LogErrorStoppingDisplayService(Exception ex);

    [LoggerMessage(10403, LogLevel.Error, "Error updating visual display")]
    private partial void LogErrorUpdatingVisualDisplay(Exception ex);
}
