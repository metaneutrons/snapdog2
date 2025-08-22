using SnapDog2.Core.Attributes;

namespace SnapDog2.Core.Notifications;

/// <summary>
/// Notification for playlist count status changes.
/// </summary>
[StatusId("PLAYLIST_COUNT_STATUS")]
public class PlaylistCountStatusNotification
{
    /// <summary>
    /// Gets or sets the zone index.
    /// </summary>
    public int ZoneIndex { get; set; }

    /// <summary>
    /// Gets or sets the playlist count.
    /// </summary>
    public int PlaylistCount { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when this status was generated.
    /// </summary>
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}
