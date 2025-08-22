using Microsoft.Extensions.Logging;

namespace SnapDog2.Infrastructure.Integrations.Mqtt;

/// <summary>
/// High-performance LoggerMessage definitions for MqttService.
/// Eliminates boxing, reduces allocations, and provides compile-time safety.
/// </summary>
public sealed partial class MqttService
{
    // Initialization and Configuration (9301)
    [LoggerMessage(9301, LogLevel.Information, "MQTT integration is disabled")]
    private partial void LogMqttIntegrationIsDisabled();

    // Message Processing Operations (9302-9307)
    [LoggerMessage(9302, LogLevel.Debug, "Processing MQTT message on topic {Topic}: {Payload}")]
    private partial void LogProcessingMqttMessageOnTopic(string topic, string payload);

    [LoggerMessage(9303, LogLevel.Warning, "Unknown command type: {CommandType}")]
    private partial void LogUnknownCommandType(string commandType);

    [LoggerMessage(9304, LogLevel.Debug, "Successfully processed MQTT command for topic {Topic}")]
    private partial void LogSuccessfullyProcessedMqttCommandForTopic(string topic);

    [LoggerMessage(9305, LogLevel.Debug, "No command mapping found for topic {Topic}")]
    private partial void LogNoCommandMappingFoundForTopic(string topic);

    [LoggerMessage(9306, LogLevel.Error, "Failed to process MQTT message on topic {Topic}")]
    private partial void LogFailedToProcessMqttMessageOnTopic(Exception ex, string topic);

    [LoggerMessage(9307, LogLevel.Error, "Failed to map topic {Topic} to command")]
    private partial void LogFailedToMapTopicToCommand(Exception ex, string topic);

    // Client Status Publishing Operations (9308-9312)
    [LoggerMessage(9308, LogLevel.Warning, "Invalid client ID format: {ClientIndex}. Expected integer.")]
    private partial void LogInvalidClientIndexFormat(string clientIndex);

    [LoggerMessage(9309, LogLevel.Warning, "No MQTT configuration for client {ClientIndex}")]
    private partial void LogNoMqttConfigurationForClient(string clientIndex);

    [LoggerMessage(9310, LogLevel.Warning, "Client {ClientIndex} has no MQTT configuration")]
    private partial void LogClientHasNoMqttConfiguration(string clientIndex);

    [LoggerMessage(9311, LogLevel.Debug, "No MQTT topic mapping for event type {EventType}")]
    private partial void LogNoMqttTopicMappingForEventType(string eventType);

    [LoggerMessage(9312, LogLevel.Error, "Failed to publish client status {EventType} for client {ClientIndex}")]
    private partial void LogFailedToPublishClientStatus(Exception ex, string eventType, string clientIndex);

    // Zone Status Publishing Operations (9313-9315)
    [LoggerMessage(9313, LogLevel.Warning, "No MQTT configuration for zone {ZoneIndex}")]
    private partial void LogNoMqttConfigurationForZone(int zoneIndex);

    [LoggerMessage(9314, LogLevel.Warning, "Zone {ZoneIndex} has no MQTT configuration")]
    private partial void LogZoneHasNoMqttConfiguration(int zoneIndex);

    [LoggerMessage(9315, LogLevel.Error, "Failed to publish zone status {EventType} for zone {ZoneIndex}")]
    private partial void LogFailedToPublishZoneStatus(Exception ex, string eventType, int zoneIndex);

    // Global Status Publishing Operations (9316-9317)
    [LoggerMessage(9316, LogLevel.Debug, "No MQTT topic mapping for global event type {EventType}")]
    private partial void LogNoMqttTopicMappingForGlobalEventType(string eventType);

    [LoggerMessage(9317, LogLevel.Error, "Failed to publish global status {EventType}")]
    private partial void LogFailedToPublishGlobalStatus(Exception ex, string eventType);

    // Registry Validation Operations (9318)
    [LoggerMessage(
        9318,
        LogLevel.Warning,
        "Unknown MQTT command '{Command}' with command ID '{CommandId}' not found in registry"
    )]
    private partial void LogUnknownMqttCommand(string command, string commandId);
}
