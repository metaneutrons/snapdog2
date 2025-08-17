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
    /// <returns>Parsed topic parts or null if invalid.</returns>
    public static MqttTopicParts? Parse(string topic)
    {
        if (string.IsNullOrWhiteSpace(topic))
        {
            return null;
        }

        var parts = topic.Split('/');

        // Basic structure: snapdog/{entity}/{id}/{command}[/set]
        if (parts.Length < 4 || parts.Length > 5)
        {
            return null;
        }

        // Must start with snapdog
        if (!parts[0].Equals(MqttConstants.ROOT_TOPIC, StringComparison.OrdinalIgnoreCase))
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
    /// <returns>True if the topic is valid, false otherwise.</returns>
    public static bool IsValid(string topic) => Parse(topic) != null;
}
