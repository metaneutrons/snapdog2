namespace SnapDog2.Infrastructure.Integrations.Mqtt;

using Cortex.Mediator.Commands;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Models;
using SnapDog2.Server.Features.Shared.Factories;

/// <summary>
/// Modern MQTT command mapper using constants, dictionaries, and type-safe parsing.
/// Eliminates magic strings and provides maintainable command mapping.
/// </summary>
public partial class MqttCommandMapper(ILogger<MqttCommandMapper> logger)
{
    private readonly ILogger<MqttCommandMapper> _logger = logger;

    [LoggerMessage(8001, LogLevel.Debug, "Mapping MQTT command: {Topic} -> {Payload}")]
    private partial void LogMappingCommand(string topic, string payload);

    [LoggerMessage(8002, LogLevel.Warning, "Failed to map MQTT topic: {Topic}")]
    private partial void LogMappingFailed(string topic);

    [LoggerMessage(8003, LogLevel.Error, "Error mapping MQTT command for topic {Topic}: {Error}")]
    private partial void LogMappingError(string topic, string error);

    /// <summary>
    /// Maps MQTT topics to Cortex.Mediator commands using the centralized CommandFactory.
    /// </summary>
    public ICommand<Result>? MapTopicToCommand(string topic, string payload)
    {
        try
        {
            this.LogMappingCommand(topic, payload);

            // Use the factory extension method for MQTT topic parsing
            var command = CommandFactoryExtensions.CreateFromMqttTopic(topic, payload);

            if (command == null)
            {
                this.LogMappingFailed(topic);
            }

            return command;
        }
        catch (Exception ex)
        {
            this.LogMappingError(topic, ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Maps complex control topics using modern parsing and dictionary-based command mapping.
    /// Example: snapdog/zone/1/control/set with payload "play track 5"
    /// </summary>
    public ICommand<Result>? MapControlTopicToCommand(string topic, string payload)
    {
        try
        {
            this.LogMappingCommand(topic, payload);

            // Parse topic using structured parser
            var topicParts = MqttTopicParser.Parse(topic);
            if (topicParts == null || !topicParts.IsControlTopic)
            {
                return null;
            }

            // Parse complex control payloads
            var payloadParts = payload.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (payloadParts.Length == 0)
            {
                return null;
            }

            var command = payloadParts[0].ToLowerInvariant();
            var parameter = payloadParts.Length > 1 ? string.Join(" ", payloadParts[1..]) : string.Empty;

            // Use strategy pattern for command mapping
            return topicParts.EntityType switch
            {
                MqttConstants.EntityTypes.ZONE => MqttCommandMappingStrategy.MapZoneCommand(
                    command,
                    topicParts.EntityId,
                    parameter
                ),
                MqttConstants.EntityTypes.CLIENT => MqttCommandMappingStrategy.MapClientCommand(
                    command,
                    topicParts.EntityId,
                    parameter
                ),
                _ => null,
            };
        }
        catch (Exception ex)
        {
            this.LogMappingError(topic, ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Validates that a topic follows the expected MQTT topic structure.
    /// </summary>
    public bool IsValidMqttTopic(string topic) => MqttTopicParser.IsValid(topic);

    /// <summary>
    /// Gets the entity type (zone/client) from an MQTT topic.
    /// </summary>
    public string? GetEntityType(string topic) => MqttTopicParser.Parse(topic)?.EntityType;

    /// <summary>
    /// Gets the entity ID from an MQTT topic.
    /// </summary>
    public int? GetEntityId(string topic) => MqttTopicParser.Parse(topic)?.EntityId;

    /// <summary>
    /// Gets the command name from an MQTT topic.
    /// </summary>
    public string? GetCommandName(string topic) => MqttTopicParser.Parse(topic)?.Command;
}
