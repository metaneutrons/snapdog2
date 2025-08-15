using Microsoft.Extensions.Logging;

namespace SnapDog2.Server.Features.Shared.Handlers;

/// <summary>
/// High-performance LoggerMessage definitions for GlobalStateNotificationHandler.
/// Eliminates boxing, reduces allocations, and provides compile-time safety.
/// </summary>
public partial class GlobalStateNotificationHandler
{
    // Global Event Publishing Operations (11101)
    [LoggerMessage(11101, LogLevel.Warning, "Failed to publish {EventType} to external systems")]
    private partial void LogFailedToPublishGlobalEventToExternalSystems(Exception ex, string eventType);
}
