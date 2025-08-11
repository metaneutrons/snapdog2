using Microsoft.Extensions.Logging;

namespace SnapDog2.Server.Features.Shared.Handlers;

/// <summary>
/// High-performance LoggerMessage definitions for ClientStateNotificationHandler.
/// Eliminates boxing, reduces allocations, and provides compile-time safety.
/// </summary>
public partial class ClientStateNotificationHandler
{
    // Client Event Publishing Operations (10901)
    [LoggerMessage(
        10901,
        LogLevel.Warning,
        "Failed to publish {EventType} for client {ClientIndex} to external systems"
    )]
    private partial void LogFailedToPublishClientEventToExternalSystems(
        Exception ex,
        string eventType,
        string clientIndex
    );
}
