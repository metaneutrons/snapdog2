namespace SnapDog2.Api.Hubs.Notifications;

using Cortex.Mediator.Notifications;
using SnapDog2.Shared.Attributes;

[StatusId("CLIENT_LATENCY_STATUS")]
public record ClientLatencyChangedNotification(int ClientIndex, int LatencyMs) : INotification;
