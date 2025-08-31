namespace SnapDog2.Api.Hubs.Notifications;

using Cortex.Mediator.Notifications;
using SnapDog2.Shared.Attributes;
using SnapDog2.Shared.Models;

[StatusId("TRACK_METADATA")]
public record ZoneTrackMetadataChangedNotification(int ZoneIndex, TrackInfo Track) : INotification;
