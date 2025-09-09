namespace SnapDog2.Api.Hubs.Notifications;

using SnapDog2.Shared.Attributes;

[StatusId("TRACK_REPEAT_STATUS")]
public record ZoneRepeatModeChangedNotification(int ZoneIndex, bool TrackRepeat, bool PlaylistRepeat);
