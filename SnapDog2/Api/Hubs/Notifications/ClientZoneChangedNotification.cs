using Cortex.Mediator.Notifications;
using SnapDog2.Shared.Attributes;

namespace SnapDog2.Hubs.Notifications;

[StatusId("CLIENT_ZONE_STATUS")]
public record ClientZoneChangedNotification(int ClientIndex, int? ZoneIndex) : INotification;
