namespace SnapDog2.Tests.Blueprint;

using System.Reflection;
using SnapDog2.Core.Attributes;

/// <summary>
/// Simple validator to demonstrate MQTT topic attribute concept.
/// This is a proof-of-concept for the attribute-based MQTT topic system.
/// </summary>
public static class MqttAttributeValidator
{
    /// <summary>
    /// Gets all commands that have MQTT topic attributes.
    /// </summary>
    /// <param name="assemblies">Assemblies to scan for commands.</param>
    /// <returns>Dictionary mapping command IDs to their topic patterns.</returns>
    public static Dictionary<string, string> GetMqttCommands(params Assembly[] assemblies)
    {
        var result = new Dictionary<string, string>();

        foreach (var assembly in assemblies)
        {
            var mqttCommands = MqttTopicAttribute.GetAllMqttCommands(assembly);
            foreach (var (topicPattern, commandType) in mqttCommands)
            {
                var commandIdAttr = commandType.GetCustomAttribute<CommandIdAttribute>();
                if (commandIdAttr != null)
                {
                    result[commandIdAttr.Id] = topicPattern;
                }
            }
        }

        return result;
    }
}
