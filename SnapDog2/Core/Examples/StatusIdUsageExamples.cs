namespace SnapDog2.Core.Examples;

using SnapDog2.Core.Attributes;
using SnapDog2.Core.Constants;
using SnapDog2.Core.Enums;
using SnapDog2.Server.Features.Clients.Notifications;

/// <summary>
/// Examples demonstrating the DRY StatusId system usage.
/// This file shows different approaches to eliminate hardcoded strings.
/// </summary>
public static class StatusIdUsageExamples
{
    /// <summary>
    /// Example 1: Using strongly-typed constants (recommended for most cases)
    /// </summary>
    public static void UsingStatusIdConstants()
    {
        // Instead of hardcoded strings like "CLIENT_VOLUME_STATUS"
        var eventType = StatusIds.ClientVolumeStatus; // Compile-time safe!

        // Use in switch statements
        var result = eventType switch
        {
            var x when x == StatusIds.ClientVolumeStatus => "Handle volume change",
            var x when x == StatusIds.ClientMuteStatus => "Handle mute change",
            _ => "Unknown event",
        };
    }

    /// <summary>
    /// Example 2: Using enum-based approach (best for complex logic)
    /// </summary>
    public static void UsingStatusEventTypeEnum()
    {
        // Parse from string (e.g., from external systems)
        var eventType = StatusEventTypeExtensions.FromStatusString("CLIENT_VOLUME_STATUS");

        if (eventType.HasValue)
        {
            var result = eventType.Value switch
            {
                StatusEventType.ClientVolumeStatus => "Handle volume change",
                StatusEventType.ClientMuteStatus => "Handle mute change",
                StatusEventType.ClientLatencyStatus => "Handle latency change",
                _ => "Unknown event",
            };
        }

        // Convert back to string
        var statusString = StatusEventType.ClientVolumeStatus.ToStatusString(); // "CLIENT_VOLUME_STATUS"
    }

    /// <summary>
    /// Example 3: Using attributes directly (for generic scenarios)
    /// </summary>
    public static void UsingStatusIdAttributes()
    {
        // Get StatusId from notification type
        var statusId = StatusIdAttribute.GetStatusId<ClientVolumeChangedNotification>();

        // Safe version that returns null if not found
        var safeStatusId = StatusIdAttribute.TryGetStatusId<ClientVolumeChangedNotification>();
    }

    /// <summary>
    /// Example 4: Using the registry for dynamic scenarios
    /// </summary>
    public static void UsingStatusIdRegistry()
    {
        // Get notification type from StatusId string
        var notificationType = StatusIdRegistry.GetNotificationType("CLIENT_VOLUME_STATUS");

        // Check if a StatusId is registered
        var isRegistered = StatusIdRegistry.IsRegistered("CLIENT_VOLUME_STATUS");

        // Get all registered StatusIds
        var allStatusIds = StatusIdRegistry.GetAllStatusIds();
    }

    /// <summary>
    /// Example 5: MQTT topic mapping (like in your MqttService)
    /// </summary>
    public static string? GetMqttTopicExample(string eventType)
    {
        // Parse to enum for type safety
        var statusEventType = StatusEventTypeExtensions.FromStatusString(eventType);
        if (statusEventType == null)
            return null;

        // Use enum in switch for compile-time safety
        return statusEventType switch
        {
            StatusEventType.ClientVolumeStatus => "client/volume",
            StatusEventType.ClientMuteStatus => "client/mute",
            StatusEventType.ClientLatencyStatus => "client/latency",
            _ => null,
        };
    }

    /// <summary>
    /// Example 6: Validation and error handling
    /// </summary>
    public static bool ValidateEventType(string eventType)
    {
        // Method 1: Using enum parsing
        var parsedEnum = StatusEventTypeExtensions.FromStatusString(eventType);
        if (parsedEnum.HasValue)
            return true;

        // Method 2: Using registry
        return StatusIdRegistry.IsRegistered(eventType);

        // Method 3: Using constants (requires manual checking)
        // return eventType == StatusIds.ClientVolumeStatus ||
        //        eventType == StatusIds.ClientMuteStatus || ...
    }
}

/// <summary>
/// Benefits of this approach:
///
/// 1. **Compile-time Safety**: No more typos in status strings
/// 2. **IntelliSense Support**: IDE autocomplete for all status types
/// 3. **Refactoring Safety**: Rename operations work across the codebase
/// 4. **Single Source of Truth**: StatusId attributes on notification classes
/// 5. **Performance**: Enum switches are optimized by the compiler
/// 6. **Maintainability**: Adding new status types is automatic
/// 7. **Documentation**: Self-documenting code with clear relationships
///
/// Migration Strategy:
/// 1. Replace hardcoded strings with StatusIds constants
/// 2. Use enum-based approach for complex switch statements
/// 3. Leverage registry for dynamic/reflection scenarios
/// 4. Keep StatusId attributes as the single source of truth
/// </summary>
public static class BenefitsAndMigrationStrategy { }
