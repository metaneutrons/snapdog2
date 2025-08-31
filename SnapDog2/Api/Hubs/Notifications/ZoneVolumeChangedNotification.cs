using Cortex.Mediator.Notifications;
using SnapDog2.Shared.Attributes;

namespace SnapDog2.Hubs.Notifications;

[StatusId("VOLUME_STATUS")]
public record ZoneVolumeChangedNotification(int ZoneIndex, int Volume) : INotification;
