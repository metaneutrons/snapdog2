namespace SnapDog2.Api.Hubs.Notifications;

using Cortex.Mediator.Notifications;
using SnapDog2.Shared.Attributes;

[StatusId("PLAYLIST_SHUFFLE_STATUS")]
public record ZoneShuffleChangedNotification(int ZoneIndex, bool Shuffle) : INotification;
