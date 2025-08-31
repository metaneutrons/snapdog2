namespace SnapDog2.Api.Hubs.Notifications;

using Cortex.Mediator.Notifications;
using SnapDog2.Shared.Attributes;

[StatusId("PLAYBACK_STATE")]
public record ZonePlaybackChangedNotification(int ZoneIndex, string PlaybackState) : INotification;
