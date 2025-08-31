using Cortex.Mediator.Notifications;
using SnapDog2.Shared.Attributes;

namespace SnapDog2.Hubs.Notifications;

[StatusId("SYSTEM_ERROR")]
public record ErrorOccurredNotification(string ErrorCode, string Message, string? Context = null) : INotification;
