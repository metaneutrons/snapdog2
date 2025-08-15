namespace SnapDog2.Core.Enums;

using System.ComponentModel;

/// <summary>
/// Enum representing all possible status event types in the system.
/// This provides compile-time safety and eliminates hardcoded strings.
/// </summary>
public enum StatusEventType
{
    // Client Status Events
    [Description("CLIENT_VOLUME_STATUS")]
    ClientVolumeStatus,

    [Description("CLIENT_MUTE_STATUS")]
    ClientMuteStatus,

    [Description("CLIENT_LATENCY_STATUS")]
    ClientLatencyStatus,

    [Description("CLIENT_CONNECTED")]
    ClientConnected,

    [Description("CLIENT_ZONE_STATUS")]
    ClientZoneStatus,

    [Description("CLIENT_STATE")]
    ClientState,

    // Zone Status Events
    [Description("PLAYBACK_STATE")]
    PlaybackState,

    [Description("VOLUME_STATUS")]
    VolumeStatus,

    [Description("MUTE_STATUS")]
    MuteStatus,

    [Description("TRACK_INDEX")]
    TrackIndex,

    [Description("PLAYLIST_INDEX")]
    PlaylistIndex,

    [Description("TRACK_REPEAT_STATUS")]
    TrackRepeatStatus,

    [Description("PLAYLIST_REPEAT_STATUS")]
    PlaylistRepeatStatus,

    [Description("PLAYLIST_SHUFFLE_STATUS")]
    PlaylistShuffleStatus,

    [Description("ZONE_STATE")]
    ZoneState,

    // Global Status Events
    [Description("VERSION_INFO")]
    VersionInfo,

    [Description("SYSTEM_STATUS")]
    SystemStatus,

    [Description("SERVER_STATS")]
    ServerStats,

    [Description("SYSTEM_ERROR")]
    SystemError,
}

/// <summary>
/// Extension methods for StatusEventType enum.
/// </summary>
public static class StatusEventTypeExtensions
{
    /// <summary>
    /// Gets the string value from the Description attribute.
    /// </summary>
    /// <param name="eventType">The status event type.</param>
    /// <returns>The string representation of the status event type.</returns>
    public static string ToStatusString(this StatusEventType eventType)
    {
        var field = eventType.GetType().GetField(eventType.ToString());
        var attribute = (DescriptionAttribute?)Attribute.GetCustomAttribute(field!, typeof(DescriptionAttribute));
        return attribute?.Description ?? eventType.ToString();
    }

    /// <summary>
    /// Parses a status string to StatusEventType enum.
    /// </summary>
    /// <param name="statusString">The status string to parse.</param>
    /// <returns>The corresponding StatusEventType, or null if not found.</returns>
    public static StatusEventType? FromStatusString(string statusString)
    {
        foreach (StatusEventType eventType in Enum.GetValues<StatusEventType>())
        {
            if (eventType.ToStatusString().Equals(statusString, StringComparison.OrdinalIgnoreCase))
            {
                return eventType;
            }
        }
        return null;
    }
}
