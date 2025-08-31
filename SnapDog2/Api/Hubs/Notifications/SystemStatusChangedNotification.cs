using Cortex.Mediator.Notifications;
using SnapDog2.Shared.Attributes;
using SnapDog2.Shared.Models;

namespace SnapDog2.Hubs.Notifications;

[StatusId("SYSTEM_STATUS")]
public record SystemStatusChangedNotification(SystemStatus Status) : INotification;
