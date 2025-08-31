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

/// <summary>
/// Represents the parsed components of an MQTT topic.
/// </summary>
public record MqttTopicParts
{
    public required string Root { get; init; }
    public required string EntityType { get; init; }
    public required int EntityId { get; init; }
    public required string Command { get; init; }
    public string? Action { get; init; }
    public bool IsControlTopic => this.Action == MqttConstants.Segments.SET;
}

/// <summary>
/// Parses MQTT topics into structured components.
/// </summary>
public static class MqttTopicParser
{
    /// <summary>
    /// Parses an MQTT topic into its component parts.
    /// </summary>
    /// <param name="topic">The MQTT topic to parse.</param>
    /// <param name="baseTopic">The MQTT base topic</param>
    /// <returns>Parsed topic parts or null if invalid.</returns>
    public static MqttTopicParts? Parse(string topic, string baseTopic)
    {
        if (string.IsNullOrWhiteSpace(topic))
        {
            return null;
        }

        var parts = topic.Split('/');

        // Basic structure: {baseTopic}/{entity}/{index}/{command}[/set]
        if (parts.Length < 4 || parts.Length > 5)
        {
            return null;
        }

        // Must start with configured base topic
        if (!parts[0].Equals(baseTopic.TrimEnd('/'), StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        // Entity type must be zone or client
        var entityType = parts[1].ToLowerInvariant();
        if (entityType != MqttConstants.EntityTypes.ZONE && entityType != MqttConstants.EntityTypes.CLIENT)
        {
            return null;
        }

        // Entity ID must be a positive integer
        if (!int.TryParse(parts[2], out var entityId) || entityId <= 0)
        {
            return null;
        }

        // Command must not be empty
        if (string.IsNullOrWhiteSpace(parts[3]))
        {
            return null;
        }

        // If 5 parts, last part should be "set"
        var action = parts.Length == 5 ? parts[4] : null;
        if (action != null && !action.Equals(MqttConstants.Segments.SET, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return new MqttTopicParts
        {
            Root = parts[0],
            EntityType = entityType,
            EntityId = entityId,
            Command = parts[3].ToLowerInvariant(),
            Action = action?.ToLowerInvariant(),
        };
    }

    /// <summary>
    /// Validates that a topic follows the expected MQTT topic structure.
    /// </summary>
    /// <param name="topic">The topic to validate.</param>
    /// <param name="baseTopic">The configured base topic (default: "snapdog").</param>
    /// <returns>True if the topic is valid, false otherwise.</returns>
    public static bool IsValid(string topic, string baseTopic) => Parse(topic, baseTopic) != null;

    /// <summary>
    /// Parses an MQTT topic using the default "snapdog" base topic for backward compatibility.
    /// </summary>
    /// <param name="topic">The MQTT topic to parse.</param>
    /// <returns>Parsed topic parts or null if invalid.</returns>
    public static MqttTopicParts? Parse(string topic) => Parse(topic, "snapdog");

    /// <summary>
    /// Validates that a topic follows the expected MQTT topic structure using default base topic.
    /// </summary>
    /// <param name="topic">The topic to validate.</param>
    /// <returns>True if the topic is valid, false otherwise.</returns>
    public static bool IsValid(string topic) => Parse(topic) != null;
}
