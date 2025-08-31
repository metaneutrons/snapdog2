using Cortex.Mediator.Notifications;
using SnapDog2.Shared.Attributes;

namespace SnapDog2.Hubs.Notifications;

[StatusId("CLIENT_CONNECTED")]
public record ClientConnectedNotification(int ClientIndex, bool Connected) : INotification;
