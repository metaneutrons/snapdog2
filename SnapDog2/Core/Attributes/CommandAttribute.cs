namespace SnapDog2.Core.Attributes;

using System.Reflection;

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
