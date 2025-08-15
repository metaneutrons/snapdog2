namespace SnapDog2.Server.Features.Subsonic.Notifications;

using Cortex.Mediator.Notifications;

/// <summary>
/// Base class for all Subsonic-related notifications.
/// </summary>
public abstract record SubsonicNotification : INotification
{
    /// <summary>
    /// Timestamp when the notification was created.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Server URL for context.
    /// </summary>
    public string ServerUrl { get; init; } = string.Empty;
}

#region Connection Events

/// <summary>
/// Notification sent when Subsonic connection is established.
/// </summary>
public record SubsonicConnectionEstablishedNotification(string ServerUrl, string Username) : SubsonicNotification
{
    public new string ServerUrl { get; init; } = ServerUrl;
}

/// <summary>
/// Notification sent when Subsonic connection is lost.
/// </summary>
public record SubsonicConnectionLostNotification(string ServerUrl, string Reason) : SubsonicNotification
{
    public new string ServerUrl { get; init; } = ServerUrl;
}

/// <summary>
/// Notification sent when Subsonic connection test fails.
/// </summary>
public record SubsonicConnectionTestFailedNotification(string ServerUrl, string ErrorMessage) : SubsonicNotification
{
    public new string ServerUrl { get; init; } = ServerUrl;
}

/// <summary>
/// Notification sent when Subsonic service is initializing.
/// </summary>
public record SubsonicInitializationStartedNotification(string ServerUrl) : SubsonicNotification
{
    public new string ServerUrl { get; init; } = ServerUrl;
}

#endregion

#region Playlist Events

/// <summary>
/// Notification sent when playlists are retrieved from Subsonic.
/// </summary>
public record SubsonicPlaylistsRetrievedNotification(string ServerUrl, int PlaylistCount, TimeSpan RetrievalTime)
    : SubsonicNotification
{
    public new string ServerUrl { get; init; } = ServerUrl;
}

/// <summary>
/// Notification sent when a specific playlist is accessed.
/// </summary>
public record SubsonicPlaylistAccessedNotification(
    string ServerUrl,
    string PlaylistIndex,
    string PlaylistName,
    int TrackCount
) : SubsonicNotification
{
    public new string ServerUrl { get; init; } = ServerUrl;
}

/// <summary>
/// Notification sent when playlist retrieval fails.
/// </summary>
public record SubsonicPlaylistRetrievalFailedNotification(string ServerUrl, string ErrorMessage) : SubsonicNotification
{
    public new string ServerUrl { get; init; } = ServerUrl;
}

/// <summary>
/// Notification sent when a specific playlist retrieval fails.
/// </summary>
public record SubsonicPlaylistAccessFailedNotification(string ServerUrl, string PlaylistIndex, string ErrorMessage)
    : SubsonicNotification
{
    public new string ServerUrl { get; init; } = ServerUrl;
}

#endregion

#region Stream Events

/// <summary>
/// Notification sent when a stream URL is requested.
/// </summary>
public record SubsonicStreamRequestedNotification(string ServerUrl, string TrackId, string? TrackTitle = null)
    : SubsonicNotification
{
    public new string ServerUrl { get; init; } = ServerUrl;
}

/// <summary>
/// Notification sent when stream URL retrieval fails.
/// </summary>
public record SubsonicStreamRequestFailedNotification(string ServerUrl, string TrackId, string ErrorMessage)
    : SubsonicNotification
{
    public new string ServerUrl { get; init; } = ServerUrl;
}

#endregion

#region Service Events

/// <summary>
/// Notification sent when Subsonic service is disposed.
/// </summary>
public record SubsonicServiceDisposedNotification(string ServerUrl) : SubsonicNotification
{
    public new string ServerUrl { get; init; } = ServerUrl;
}

/// <summary>
/// Notification sent when Subsonic service encounters an error.
/// </summary>
public record SubsonicServiceErrorNotification(string ServerUrl, string Operation, string ErrorMessage)
    : SubsonicNotification
{
    public new string ServerUrl { get; init; } = ServerUrl;
}

#endregion
