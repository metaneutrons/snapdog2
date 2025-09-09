namespace SnapDog2.Api.Hubs.Notifications;

using SnapDog2.Shared.Attributes;

[StatusId("VOLUME_STATUS")]
public record ZoneVolumeChangedNotification(int ZoneIndex, int Volume);
