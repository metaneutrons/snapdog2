namespace SnapDog2.Api.Hubs.Notifications;

using Cortex.Mediator.Notifications;
using SnapDog2.Shared.Attributes;

[StatusId("SYSTEM_ERROR")]
public record ErrorOccurredNotification(string ErrorCode, string Message, string? Context = null) : INotification;
