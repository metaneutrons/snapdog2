//
// SnapDog
// The Snapcast-based Smart Home Audio System with MQTT & KNX integration
// Copyright (C) 2025 Fabian Schmieder
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <https://www.gnu.org/licenses/>.
//
using Microsoft.Extensions.Logging;

namespace SnapDog2.Infrastructure.Integrations.Mqtt;

/// <summary>
/// High-performance LoggerMessage definitions for MqttService.
/// Eliminates boxing, reduces allocations, and provides compile-time safety.
/// </summary>
public sealed partial class MqttService
{
    // Initialization and Configuration (9301)
    [LoggerMessage(
        EventId = 4300,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "MQTT integration is disabled"
    )]
    private partial void LogMqttIntegrationIsDisabled();

    // Message Processing Operations (9302-9307)
    [LoggerMessage(
        EventId = 4301,
        Level = Microsoft.Extensions.Logging.LogLevel.Debug,
        Message = "Processing MQTT message on topic {Topic}: {Payload}"
    )]
    private partial void LogProcessingMqttMessageOnTopic(string topic, string payload);

    [LoggerMessage(
        EventId = 4302,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Unknown command type: {CommandType}"
    )]
    private partial void LogUnknownCommandType(string commandType);

    [LoggerMessage(
        EventId = 4303,
        Level = Microsoft.Extensions.Logging.LogLevel.Debug,
        Message = "Successfully processed MQTT command for topic {Topic}"
    )]
    private partial void LogSuccessfullyProcessedMqttCommandForTopic(string topic);

    [LoggerMessage(
        EventId = 4304,
        Level = Microsoft.Extensions.Logging.LogLevel.Debug,
        Message = "No command mapping found for topic {Topic}"
    )]
    private partial void LogNoCommandMappingFoundForTopic(string topic);

    [LoggerMessage(
        EventId = 4305,
        Level = Microsoft.Extensions.Logging.LogLevel.Error,
        Message = "Failed to process MQTT message on topic {Topic}"
    )]
    private partial void LogFailedToProcessMqttMessageOnTopic(Exception ex, string topic);

    [LoggerMessage(
        EventId = 4306,
        Level = Microsoft.Extensions.Logging.LogLevel.Error,
        Message = "Failed to map topic {Topic} to command"
    )]
    private partial void LogFailedToMapTopicToCommand(Exception ex, string topic);

    // Client Status Publishing Operations (9308-9312)
    [LoggerMessage(
        EventId = 4307,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Invalid client Index format: {ClientIndex}. Expected integer."
    )]
    private partial void LogInvalidClientIndexFormat(string clientIndex);

    [LoggerMessage(
        EventId = 4308,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "No MQTT configuration for client {ClientIndex}"
    )]
    private partial void LogNoMqttConfigurationForClient(string clientIndex);

    [LoggerMessage(
        EventId = 4309,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Client {ClientIndex} has no MQTT configuration"
    )]
    private partial void LogClientHasNoMqttConfiguration(string clientIndex);

    [LoggerMessage(
        EventId = 4310,
        Level = Microsoft.Extensions.Logging.LogLevel.Debug,
        Message = "No MQTT topic mapping for event type {EventType}"
    )]
    private partial void LogNoMqttTopicMappingForEventType(string eventType);

    [LoggerMessage(
        EventId = 4311,
        Level = Microsoft.Extensions.Logging.LogLevel.Error,
        Message = "Failed to publish client status {EventType} for client {ClientIndex}"
    )]
    private partial void LogFailedToPublishClientStatus(Exception ex, string eventType, string clientIndex);

    // Zone Status Publishing Operations (9313-9315)
    [LoggerMessage(
        EventId = 4312,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "No MQTT configuration for zone {ZoneIndex}"
    )]
    private partial void LogNoMqttConfigurationForZone(int zoneIndex);

    [LoggerMessage(
        EventId = 4313,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Zone {ZoneIndex} has no MQTT configuration"
    )]
    private partial void LogZoneHasNoMqttConfiguration(int zoneIndex);

    [LoggerMessage(
        EventId = 4314,
        Level = Microsoft.Extensions.Logging.LogLevel.Error,
        Message = "Failed to publish zone status {EventType} for zone {ZoneIndex}"
    )]
    private partial void LogFailedToPublishZoneStatus(Exception ex, string eventType, int zoneIndex);

    // Global Status Publishing Operations (9316-9317)
    [LoggerMessage(
        EventId = 4315,
        Level = Microsoft.Extensions.Logging.LogLevel.Debug,
        Message = "No MQTT topic mapping for global event type {EventType}"
    )]
    private partial void LogNoMqttTopicMappingForGlobalEventType(string eventType);

    [LoggerMessage(
        EventId = 4316,
        Level = Microsoft.Extensions.Logging.LogLevel.Error,
        Message = "Failed to publish global status {EventType}"
    )]
    private partial void LogFailedToPublishGlobalStatus(Exception ex, string eventType);

    // Registry Validation Operations (9318)
    [LoggerMessage(
        EventId = 4317,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Unknown MQTT command '{Command}' with command ID '{CommandId}' not found in registry"
    )]
    private partial void LogUnknownMqttCommand(string command, string commandId);
}
