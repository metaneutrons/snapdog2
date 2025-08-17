namespace SnapDog2.Infrastructure.Integrations.Snapcast;

/// <summary>
/// Constants for Snapcast service internal operations.
/// These are used only for mapping StatusChangedNotification to Snapcast operations.
/// </summary>
internal static class SnapcastConstants
{
    /// <summary>
    /// Status types used in StatusChangedNotification handling.
    /// These match the existing StatusIds from the blueprint.
    /// </summary>
    public static class StatusTypes
    {
        public const string VOLUME = "VOLUME";
        public const string MUTE = "MUTE";
    }

    /// <summary>
    /// Target ID prefixes for identifying entity types in status notifications.
    /// </summary>
    public static class TargetPrefixes
    {
        public const string CLIENT = "client_";
        public const string GROUP = "group_";
    }
}
