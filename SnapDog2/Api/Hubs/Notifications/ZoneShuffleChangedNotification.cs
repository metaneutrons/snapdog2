using Cortex.Mediator.Notifications;
using SnapDog2.Shared.Attributes;

namespace SnapDog2.Hubs.Notifications;

[StatusId("PLAYLIST_SHUFFLE_STATUS")]
public record ZoneShuffleChangedNotification(int ZoneIndex, bool Shuffle) : INotification;
