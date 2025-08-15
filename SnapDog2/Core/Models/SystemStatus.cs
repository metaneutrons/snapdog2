namespace SnapDog2.Core.Models;

/// <summary>
/// Represents the current system status.
/// </summary>
public record SystemStatus
{
    /// <summary>
    /// Gets whether the system is online.
    /// </summary>
    public required bool IsOnline { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the status was recorded.
    /// </summary>
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
}
