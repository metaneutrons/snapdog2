using SnapDog2.Core.Attributes;

namespace SnapDog2.Core.Notifications;

/// <summary>
/// Notification for control status changes.
/// </summary>
[StatusId("CONTROL_STATUS")]
public class ControlStatusNotification
{
    /// <summary>
    /// Gets or sets the zone index.
    /// </summary>
    public int ZoneIndex { get; set; }

    /// <summary>
    /// Gets or sets the control command that was executed.
    /// </summary>
    public string Command { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the execution status.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when this status was generated.
    /// </summary>
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}
