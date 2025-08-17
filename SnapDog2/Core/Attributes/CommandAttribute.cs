namespace SnapDog2.Core.Attributes;

using System.Reflection;

/// <summary>
/// Attribute to mark command classes with their corresponding command ID.
/// Used for inbound command processing from external systems (MQTT, KNX).
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class CommandIdAttribute : Attribute
{
    /// <summary>
    /// The command identifier used in external systems.
    /// </summary>
    public string Id { get; }

    public CommandIdAttribute(string id)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
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
                    + $"Add [CommandId(\"COMMAND_NAME\")] to the class."
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
