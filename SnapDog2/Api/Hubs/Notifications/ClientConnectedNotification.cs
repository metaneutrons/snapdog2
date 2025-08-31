namespace SnapDog2.Api.Hubs.Notifications;

using Cortex.Mediator.Notifications;
using SnapDog2.Shared.Attributes;

[StatusId("CLIENT_CONNECTED")]
public record ClientConnectedNotification(int ClientIndex, bool Connected) : INotification;
