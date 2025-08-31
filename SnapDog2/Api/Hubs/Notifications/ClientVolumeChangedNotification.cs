using Cortex.Mediator.Notifications;
using SnapDog2.Shared.Attributes;

namespace SnapDog2.Hubs.Notifications;

[StatusId("CLIENT_VOLUME_STATUS")]
public record ClientVolumeChangedNotification(int ClientIndex, int Volume) : INotification;
