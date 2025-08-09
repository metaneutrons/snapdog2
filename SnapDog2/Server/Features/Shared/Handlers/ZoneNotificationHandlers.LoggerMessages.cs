using Microsoft.Extensions.Logging;

namespace SnapDog2.Server.Features.Shared.Handlers;

/// <summary>
/// High-performance LoggerMessage definitions for ZoneStateNotificationHandler.
/// Eliminates boxing, reduces allocations, and provides compile-time safety.
/// </summary>
public partial class ZoneStateNotificationHandler
{
    // Zone Event Publishing Operations (11001)
    [LoggerMessage(11001, LogLevel.Warning, "Failed to publish {EventType} for zone {ZoneId} to external systems")]
    private partial void LogFailedToPublishZoneEventToExternalSystems(Exception ex, string eventType, string zoneId);
}
