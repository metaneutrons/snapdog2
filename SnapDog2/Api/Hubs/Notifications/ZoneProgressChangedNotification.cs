namespace SnapDog2.Api.Hubs.Notifications;

using SnapDog2.Shared.Attributes;

[StatusId("TRACK_PROGRESS_STATUS")]
public record ZoneProgressChangedNotification(int ZoneIndex, long Position, float Progress);
