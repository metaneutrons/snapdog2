namespace SnapDog2.Api.Hubs.Notifications;

using Cortex.Mediator.Notifications;
using SnapDog2.Shared.Attributes;
using SnapDog2.Shared.Models;

[StatusId("SYSTEM_STATUS")]
public record SystemStatusChangedNotification(SystemStatus Status) : INotification;
