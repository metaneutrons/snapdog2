using SnapDog2.Core.Attributes;

namespace SnapDog2.Core.Notifications;

/// <summary>
/// Notification for zone name status changes.
/// </summary>
[StatusId("ZONE_NAME_STATUS")]
public class ZoneNameStatusNotification
{
    /// <summary>
    /// Gets or sets the zone index.
    /// </summary>
    public int ZoneIndex { get; set; }

    /// <summary>
    /// Gets or sets the zone name.
    /// </summary>
    public string ZoneName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when this status was generated.
    /// </summary>
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}
