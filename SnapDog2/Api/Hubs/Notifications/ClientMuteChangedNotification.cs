namespace SnapDog2.Api.Hubs.Notifications;

using SnapDog2.Shared.Attributes;

[StatusId("CLIENT_MUTE_STATUS")]
public record ClientMuteChangedNotification(int ClientIndex, bool Muted);
