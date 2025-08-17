namespace SnapDog2.Core.Attributes;

using System.Reflection;

/// <summary>
/// Attribute to mark notification classes with their corresponding status ID.
/// Used for outbound status events to external systems (MQTT, KNX).
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class StatusIdAttribute : Attribute
{
    /// <summary>
    /// The status identifier used in external systems.
    /// </summary>
    public string Id { get; }

    public StatusIdAttribute(string id)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
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
