using Cortex.Mediator.Notifications;
using SnapDog2.Shared.Attributes;

namespace SnapDog2.Hubs.Notifications;

[StatusId("TRACK_REPEAT_STATUS")]
public record ZoneRepeatModeChangedNotification(int ZoneIndex, bool TrackRepeat, bool PlaylistRepeat) : INotification;
