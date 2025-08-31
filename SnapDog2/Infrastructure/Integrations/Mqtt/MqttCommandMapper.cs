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
namespace SnapDog2.Infrastructure.Integrations.Mqtt;

using Cortex.Mediator.Commands;
using Microsoft.Extensions.Logging;
using SnapDog2.Server.Shared.Factories;
using SnapDog2.Shared.Configuration;
using SnapDog2.Shared.Models;

/// <summary>
/// Modern MQTT command mapper using constants, dictionaries, and type-safe parsing.
/// Eliminates magic strings and provides maintainable command mapping.
/// </summary>
public partial class MqttCommandMapper(ILogger<MqttCommandMapper> logger, MqttConfig mqttConfig)
{
    private readonly ILogger<MqttCommandMapper> _logger = logger;
    private readonly MqttConfig _mqttConfig = mqttConfig;

    [LoggerMessage(
        EventId = 4200,
        Level = Microsoft.Extensions.Logging.LogLevel.Debug,
        Message = "Mapping MQTT command: {Topic} -> {Payload}"
    )]
    private partial void LogMappingCommand(string topic, string payload);

    [LoggerMessage(
        EventId = 4201,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Failed to map MQTT topic: {Topic}"
    )]
    private partial void LogMappingFailed(string topic);

    [LoggerMessage(
        EventId = 4202,
        Level = Microsoft.Extensions.Logging.LogLevel.Error,
        Message = "Error mapping MQTT command for topic {Topic}: {Error}"
    )]
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
    /// Example: snapdog/zones/1/control/set with payload "play track 5"
    /// </summary>
    public ICommand<Result>? MapControlTopicToCommand(string topic, string payload)
    {
        try
        {
            this.LogMappingCommand(topic, payload);

            // Parse topic using structured parser
            var topicParts = MqttTopicParser.Parse(topic, this._mqttConfig.MqttBaseTopic);
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
    public bool IsValidMqttTopic(string topic) => MqttTopicParser.IsValid(topic, this._mqttConfig.MqttBaseTopic);

    /// <summary>
    /// Gets the entity type (zones/client) from an MQTT topic.
    /// </summary>
    public string? GetEntityType(string topic) => MqttTopicParser.Parse(topic, this._mqttConfig.MqttBaseTopic)?.EntityType;

    /// <summary>
    /// Gets the entity ID from an MQTT topic.
    /// </summary>
    public int? GetEntityId(string topic) => MqttTopicParser.Parse(topic, this._mqttConfig.MqttBaseTopic)?.EntityId;

    /// <summary>
    /// Gets the command name from an MQTT topic.
    /// </summary>
    public string? GetCommandName(string topic) => MqttTopicParser.Parse(topic, this._mqttConfig.MqttBaseTopic)?.Command;
}
