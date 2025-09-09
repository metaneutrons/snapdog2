namespace SnapDog2.Api.Hubs.Notifications;

using SnapDog2.Shared.Attributes;

[StatusId("CLIENT_VOLUME_STATUS")]
public record ClientVolumeChangedNotification(int ClientIndex, int Volume);
