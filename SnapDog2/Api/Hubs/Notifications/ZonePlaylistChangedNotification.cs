namespace SnapDog2.Api.Hubs.Notifications;

using Cortex.Mediator.Notifications;
using SnapDog2.Shared.Attributes;
using SnapDog2.Shared.Models;

[StatusId("PLAYLIST_STATUS")]
public record ZonePlaylistChangedNotification(int ZoneIndex, PlaylistInfo? Playlist) : INotification;
