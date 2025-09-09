namespace SnapDog2.Api.Hubs.Notifications;

using SnapDog2.Shared.Attributes;

[StatusId("MUTE_STATUS")]
public record ZoneMuteChangedNotification(int ZoneIndex, bool Muted);
