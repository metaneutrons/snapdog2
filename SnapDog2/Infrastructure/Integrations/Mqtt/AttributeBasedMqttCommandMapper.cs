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

using System.Reflection;
using System.Text.Json;
using Cortex.Mediator.Commands;
using SnapDog2.Shared.Attributes;
using SnapDog2.Shared.Configuration;
using SnapDog2.Shared.Enums;
using SnapDog2.Shared.Models;

/// <summary>
/// Attribute-based MQTT command mapper that uses MqttTopicAttribute decorations
/// to automatically discover and route MQTT topics to commands, similar to API routing.
/// </summary>
public partial class AttributeBasedMqttCommandMapper(
    ILogger<AttributeBasedMqttCommandMapper> logger,
    MqttConfig mqttConfig
)
{
    private readonly ILogger<AttributeBasedMqttCommandMapper> _logger = logger;
    private readonly MqttConfig _mqttConfig = mqttConfig;
    private readonly Dictionary<string, (Type CommandType, MqttTopicAttribute Attribute)> _topicMappings = new();
    private bool _initialized;

    [LoggerMessage(EventId = 115150, Level = LogLevel.Debug, Message = "Initializing MQTT command mappings from attributes"
)]
    private partial void LogInitializingMappings();

    [LoggerMessage(EventId = 115151, Level = LogLevel.Debug, Message = "Found MQTT command: {CommandType} -> {TopicPattern}"
)]
    private partial void LogFoundCommand(string commandType, string topicPattern);

    [LoggerMessage(EventId = 115152, Level = LogLevel.Debug, Message = "Mapping MQTT topic: {Topic} -> {Payload}"
)]
    private partial void LogMappingTopic(string topic, string payload);

    [LoggerMessage(EventId = 115153, Level = LogLevel.Warning, Message = "No matching command found for MQTT topic: {Topic}"
)]
    private partial void LogNoMatchingCommand(string topic);

    [LoggerMessage(EventId = 115154, Level = LogLevel.Error, Message = "Error creating command for topic {Topic}: {Error}"
)]
    private partial void LogCommandCreationError(string topic, string error);

    /// <summary>
    /// Initializes the mapper by scanning for commands with MqttTopicAttribute.
    /// </summary>
    public void Initialize()
    {
        if (this._initialized)
        {
            return;
        }

        this.LogInitializingMappings();

        // Scan all assemblies for commands with MqttTopicAttribute
        var assemblies = new[]
        {
            Assembly.GetExecutingAssembly(),
            Assembly.GetAssembly(typeof(ICommand<>))!,
            Assembly.GetAssembly(typeof(Result))!,
        };

        foreach (var assembly in assemblies)
        {
            var mqttCommands = MqttTopicAttribute.GetAllMqttCommands(assembly);
            foreach (var (topicPattern, commandType) in mqttCommands)
            {
                var attribute = commandType.GetCustomAttribute<MqttTopicAttribute>()!;
                this._topicMappings[topicPattern] = (commandType, attribute);
                this.LogFoundCommand(commandType.Name, topicPattern);
            }
        }

        this._initialized = true;
    }

    /// <summary>
    /// Maps an MQTT topic and payload to a command using attribute-based routing.
    /// </summary>
    /// <param name="topic">The MQTT topic received.</param>
    /// <param name="payload">The MQTT payload.</param>
    /// <returns>The mapped command or null if no match found.</returns>
    public ICommand<Result>? MapTopicToCommand(string topic, string payload)
    {
        if (!this._initialized)
        {
            this.Initialize();
        }

        this.LogMappingTopic(topic, payload);

        try
        {
            // Try to find a matching topic pattern
            foreach (var (_, (commandType, attribute)) in this._topicMappings)
            {
                if (attribute.TryMatchTopic(topic, out var parameters, this._mqttConfig.MqttBaseTopic))
                {
                    return this.CreateCommandInstance(commandType, parameters, payload, attribute);
                }
            }

            this.LogNoMatchingCommand(topic);
            return null;
        }
        catch (Exception ex)
        {
            this.LogCommandCreationError(topic, ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Creates a command instance using reflection and parameter binding.
    /// </summary>
    private ICommand<Result>? CreateCommandInstance(
        Type commandType,
        Dictionary<string, string> parameters,
        string payload,
        MqttTopicAttribute attribute
    )
    {
        try
        {
            // Create command instance
            var command = Activator.CreateInstance(commandType);
            if (command is not ICommand<Result> typedCommand)
            {
                return null;
            }

            // Set properties from topic parameters
            foreach (var (paramName, paramValue) in parameters)
            {
                var property = commandType.GetProperty(GetPropertyName(paramName));
                if (property != null && property.CanWrite)
                {
                    var convertedValue = ConvertParameter(paramValue, property.PropertyType);
                    property.SetValue(command, convertedValue);
                }
            }

            // Set payload-based properties
            SetPayloadProperties(command, payload, attribute);

            // Set command source
            var sourceProperty = commandType.GetProperty("Source");
            sourceProperty?.SetValue(command, CommandSource.Mqtt);

            return typedCommand;
        }
        catch (Exception ex)
        {
            this.LogCommandInstanceCreationError(commandType.Name, ex);
            return null;
        }
    }

    /// <summary>
    /// Sets properties from the MQTT payload.
    /// </summary>
    private static void SetPayloadProperties(object command, string payload, MqttTopicAttribute attribute)
    {
        var commandType = command.GetType();

        if (attribute.SupportsComplexPayload)
        {
            // Try to deserialize JSON payload
            try
            {
                var payloadObject = JsonSerializer.Deserialize<Dictionary<string, object>>(payload);
                if (payloadObject != null)
                {
                    foreach (var (key, value) in payloadObject)
                    {
                        var property = commandType.GetProperty(GetPropertyName(key));
                        if (property != null && property.CanWrite)
                        {
                            var convertedValue = ConvertParameter(value.ToString()!, property.PropertyType);
                            property.SetValue(command, convertedValue);
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // Fall back to simple payload handling
                SetSimplePayloadProperty(command, payload);
            }
        }
        else
        {
            SetSimplePayloadProperty(command, payload);
        }
    }

    /// <summary>
    /// Sets a simple payload property (usually "Value" or similar).
    /// </summary>
    private static void SetSimplePayloadProperty(object command, string payload)
    {
        var commandType = command.GetType();

        // Try common property names for payload values
        var payloadProperties = new[] { "Volume", "Value", "Level", "State", "Enabled", "Index" };

        foreach (var propName in payloadProperties)
        {
            var property = commandType.GetProperty(propName);
            if (property != null && property.CanWrite)
            {
                var convertedValue = ConvertParameter(payload, property.PropertyType);
                if (convertedValue != null)
                {
                    property.SetValue(command, convertedValue);
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Converts a parameter name from MQTT format to C# property name.
    /// </summary>
    private static string GetPropertyName(string parameterName)
    {
        return parameterName switch
        {
            "zoneIndex" => "ZoneIndex",
            "clientIndex" => "ClientIndex",
            "trackIndex" => "TrackIndex",
            "playlistIndex" => "PlaylistIndex",
            _ => char.ToUpper(parameterName[0]) + parameterName[1..],
        };
    }

    /// <summary>
    /// Converts a string parameter to the target type.
    /// </summary>
    private static object? ConvertParameter(string value, Type targetType)
    {
        if (targetType == typeof(string))
        {
            return value;
        }

        if (targetType == typeof(int) || targetType == typeof(int?))
        {
            return int.TryParse(value, out var intValue) ? intValue : null;
        }

        if (targetType == typeof(bool) || targetType == typeof(bool?))
        {
            return bool.TryParse(value, out var boolValue) ? boolValue : null;
        }

        if (targetType == typeof(double) || targetType == typeof(double?))
        {
            return double.TryParse(value, out var doubleValue) ? doubleValue : null;
        }

        if (targetType.IsEnum)
        {
            return Enum.TryParse(targetType, value, true, out var enumValue) ? enumValue : null;
        }

        return value;
    }

    /// <summary>
    /// Gets all registered MQTT topic patterns.
    /// </summary>
    public IEnumerable<string> GetRegisteredTopicPatterns()
    {
        if (!this._initialized)
        {
            this.Initialize();
        }

        return this._topicMappings.Keys;
    }

    [LoggerMessage(EventId = 115155, Level = LogLevel.Error, Message = "Failed to create command instance for type {CommandType}"
)]
    private partial void LogCommandInstanceCreationError(string commandType, Exception ex);
}
