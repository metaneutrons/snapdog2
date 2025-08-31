using Cortex.Mediator.Notifications;
using SnapDog2.Shared.Attributes;

namespace SnapDog2.Hubs.Notifications;

[StatusId("MUTE_STATUS")]
public record ZoneMuteChangedNotification(int ZoneIndex, bool Muted) : INotification;
