using Cortex.Mediator.Notifications;
using SnapDog2.Shared.Attributes;

namespace SnapDog2.Hubs.Notifications;

[StatusId("CLIENT_MUTE_STATUS")]
public record ClientMuteChangedNotification(int ClientIndex, bool Muted) : INotification;
