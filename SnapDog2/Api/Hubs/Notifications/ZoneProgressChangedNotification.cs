using Cortex.Mediator.Notifications;
using SnapDog2.Shared.Attributes;

namespace SnapDog2.Hubs.Notifications;

[StatusId("TRACK_PROGRESS_STATUS")]
public record ZoneProgressChangedNotification(int ZoneIndex, long Position, float Progress) : INotification;
