namespace SnapDog2.Api.Hubs.Notifications;

using SnapDog2.Shared.Attributes;

[StatusId("CLIENT_ZONE_STATUS")]
public record ClientZoneChangedNotification(int ClientIndex, int? ZoneIndex);
