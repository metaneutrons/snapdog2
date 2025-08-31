using Cortex.Mediator.Notifications;
using SnapDog2.Shared.Attributes;
using SnapDog2.Shared.Models;

namespace SnapDog2.Hubs.Notifications;

[StatusId("TRACK_METADATA")]
public record ZoneTrackMetadataChangedNotification(int ZoneIndex, TrackInfo Track) : INotification;
