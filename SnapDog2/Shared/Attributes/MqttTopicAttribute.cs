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
namespace SnapDog2.Shared.Attributes;

using System.Reflection;

/// <summary>
/// Attribute to mark command classes with their corresponding MQTT topic pattern.
/// Used for inbound MQTT command processing, similar to HTTP routing attributes.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class MqttTopicAttribute(string topicPattern) : Attribute
{
    /// <summary>
    /// The MQTT topic pattern used for this command.
    /// Supports placeholders like {zoneIndex} and {clientIndex}.
    /// Example: "snapdog/zones/{zoneIndex}/volume/set"
    /// </summary>
    public string TopicPattern { get; } = topicPattern ?? throw new ArgumentNullException(nameof(topicPattern));

    /// <summary>
    /// Optional: Specifies the expected payload format.
    /// </summary>
    public string? PayloadFormat { get; init; }

    /// <summary>
    /// Optional: Indicates if this command supports complex payloads.
    /// </summary>
    public bool SupportsComplexPayload { get; init; } = false;

    /// <summary>
    /// Gets the MQTT topic pattern for a command type.
    /// </summary>
    /// <typeparam name="T">The command type.</typeparam>
    /// <returns>The MQTT topic pattern string.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no MqttTopic attribute is found.</exception>
    public static string GetTopicPattern<T>()
    {
        var attribute = typeof(T).GetCustomAttribute<MqttTopicAttribute>();
        return attribute?.TopicPattern
            ?? throw new InvalidOperationException(
                $"No MqttTopic attribute found on {typeof(T).Name}. "
                    + $"Add [MqttTopic(\"topic/pattern\")] to the class."
            );
    }

    /// <summary>
    /// Gets the MQTT topic pattern for a command type, returning null if not found.
    /// </summary>
    /// <typeparam name="T">The command type.</typeparam>
    /// <returns>The MQTT topic pattern string or null if not found.</returns>
    public static string? TryGetTopicPattern<T>()
    {
        var attribute = typeof(T).GetCustomAttribute<MqttTopicAttribute>();
        return attribute?.TopicPattern;
    }

    /// <summary>
    /// Gets all command types that have MQTT topic attributes.
    /// </summary>
    /// <param name="assembly">Assembly to search in.</param>
    /// <returns>Dictionary mapping topic patterns to command types.</returns>
    public static Dictionary<string, Type> GetAllMqttCommands(Assembly assembly)
    {
        return assembly
            .GetTypes()
            .Where(type => type.GetCustomAttribute<MqttTopicAttribute>() != null)
            .ToDictionary(type => type.GetCustomAttribute<MqttTopicAttribute>()!.TopicPattern, type => type);
    }

    /// <summary>
    /// Checks if a topic matches the pattern, extracting parameters.
    /// Supports configurable base topic by replacing "snapdog" with the actual base topic.
    /// </summary>
    /// <param name="topic">Actual MQTT topic received.</param>
    /// <param name="parameters">Extracted parameters from the topic.</param>
    /// <param name="baseTopic">The configured base topic (e.g., "snapdog", "myapp", etc.)</param>
    /// <returns>True if the topic matches the pattern.</returns>
    public bool TryMatchTopic(string topic, out Dictionary<string, string> parameters, string baseTopic = "snapdog")
    {
        parameters = new Dictionary<string, string>();

        // Replace the hardcoded "snapdog" in the pattern with the actual base topic
        var actualPattern = this.TopicPattern.Replace("snapdog", baseTopic.TrimEnd('/'), StringComparison.OrdinalIgnoreCase);

        var patternParts = actualPattern.Split('/');
        var topicParts = topic.Split('/');

        if (patternParts.Length != topicParts.Length)
        {
            return false;
        }

        for (int i = 0; i < patternParts.Length; i++)
        {
            var patternPart = patternParts[i];
            var topicPart = topicParts[i];

            if (patternPart.StartsWith('{') && patternPart.EndsWith('}'))
            {
                // Extract parameter
                var paramName = patternPart[1..^1];
                parameters[paramName] = topicPart;
            }
            else if (!patternPart.Equals(topicPart, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Checks if a topic matches the pattern, extracting parameters.
    /// Uses default "snapdog" base topic for backward compatibility.
    /// </summary>
    /// <param name="topic">Actual MQTT topic received.</param>
    /// <param name="parameters">Extracted parameters from the topic.</param>
    /// <returns>True if the topic matches the pattern.</returns>
    public bool TryMatchTopic(string topic, out Dictionary<string, string> parameters)
    {
        return this.TryMatchTopic(topic, out parameters, "snapdog");
    }
}
