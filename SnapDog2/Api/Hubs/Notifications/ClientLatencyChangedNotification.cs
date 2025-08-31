using Cortex.Mediator.Notifications;
using SnapDog2.Shared.Attributes;

namespace SnapDog2.Hubs.Notifications;

[StatusId("CLIENT_LATENCY_STATUS")]
public record ClientLatencyChangedNotification(int ClientIndex, int LatencyMs) : INotification;
