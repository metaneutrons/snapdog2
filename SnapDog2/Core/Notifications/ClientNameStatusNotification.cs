using SnapDog2.Core.Attributes;

namespace SnapDog2.Core.Notifications;

/// <summary>
/// Notification for client name status changes.
/// </summary>
[StatusId("CLIENT_NAME_STATUS")]
public class ClientNameStatusNotification
{
    /// <summary>
    /// Gets or sets the client index.
    /// </summary>
    public int ClientIndex { get; set; }

    /// <summary>
    /// Gets or sets the client name.
    /// </summary>
    public string ClientName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when this status was generated.
    /// </summary>
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}
