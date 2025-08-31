namespace SnapDog2.Api.Hubs.Notifications;

using Cortex.Mediator.Notifications;
using SnapDog2.Shared.Attributes;

[StatusId("VOLUME_STATUS")]
public record ZoneVolumeChangedNotification(int ZoneIndex, int Volume) : INotification;
