using Cortex.Mediator.Notifications;
using SnapDog2.Shared.Attributes;

namespace SnapDog2.Hubs.Notifications;

[StatusId("PLAYBACK_STATE")]
public record ZonePlaybackChangedNotification(int ZoneIndex, string PlaybackState) : INotification;
