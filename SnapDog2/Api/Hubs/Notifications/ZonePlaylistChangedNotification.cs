using Cortex.Mediator.Notifications;
using SnapDog2.Shared.Attributes;
using SnapDog2.Shared.Models;

namespace SnapDog2.Hubs.Notifications;

[StatusId("PLAYLIST_STATUS")]
public record ZonePlaylistChangedNotification(int ZoneIndex, PlaylistInfo? Playlist) : INotification;
