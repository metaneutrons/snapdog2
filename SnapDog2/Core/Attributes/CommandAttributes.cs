namespace SnapDog2.Core.Attributes;

using System.Reflection;

/// <summary>
/// Attribute to mark notification classes with their corresponding status ID from the blueprint.
/// Used for outbound status events to external systems (MQTT, KNX).
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class StatusIdAttribute : Attribute
{
    /// <summary>
    /// The status identifier used in external systems.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Optional reference to the blueprint document for traceability.
    /// </summary>
    public string? BlueprintReference { get; }

    public StatusIdAttribute(string id, string? blueprintReference = null)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        BlueprintReference = blueprintReference;
    }

    /// <summary>
    /// Gets the status ID for a notification type.
    /// </summary>
    /// <typeparam name="T">The notification type.</typeparam>
    /// <returns>The status ID string.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no StatusId attribute is found.</exception>
    public static string GetStatusId<T>()
    {
        var attribute = typeof(T).GetCustomAttribute<StatusIdAttribute>();
        return attribute?.Id
            ?? throw new InvalidOperationException(
                $"No StatusId attribute found on {typeof(T).Name}. " + $"Add [StatusId(\"STATUS_NAME\")] to the class."
            );
    }

    /// <summary>
    /// Gets the status ID for a notification type, returning null if not found.
    /// </summary>
    /// <typeparam name="T">The notification type.</typeparam>
    /// <returns>The status ID string or null if not found.</returns>
    public static string? TryGetStatusId<T>()
    {
        var attribute = typeof(T).GetCustomAttribute<StatusIdAttribute>();
        return attribute?.Id;
    }
}

/// <summary>
/// Attribute to mark command classes with their corresponding command ID from the blueprint.
/// Used for inbound command processing from external systems (MQTT, KNX).
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class CommandIdAttribute : Attribute
{
    /// <summary>
    /// The command identifier used in external systems.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Optional reference to the blueprint document for traceability.
    /// </summary>
    public string? BlueprintReference { get; }

    public CommandIdAttribute(string id, string? blueprintReference = null)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        BlueprintReference = blueprintReference;
    }

    /// <summary>
    /// Gets the command ID for a command type.
    /// </summary>
    /// <typeparam name="T">The command type.</typeparam>
    /// <returns>The command ID string.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no CommandId attribute is found.</exception>
    public static string GetCommandId<T>()
    {
        var attribute = typeof(T).GetCustomAttribute<CommandIdAttribute>();
        return attribute?.Id
            ?? throw new InvalidOperationException(
                $"No CommandId attribute found on {typeof(T).Name}. "
                    + $"Add [CommandId(\"COMMAND_NAME\", \"Blueprint-Reference\")] to the class."
            );
    }

    /// <summary>
    /// Gets the command ID for a command type, returning null if not found.
    /// </summary>
    /// <typeparam name="T">The command type.</typeparam>
    /// <returns>The command ID string or null if not found.</returns>
    public static string? TryGetCommandId<T>()
    {
        var attribute = typeof(T).GetCustomAttribute<CommandIdAttribute>();
        return attribute?.Id;
    }
}
