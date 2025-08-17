namespace SnapDog2.Server.Features.Clients.Notifications;

using Cortex.Mediator.Notifications;
using SnapDog2.Core.Attributes;
using SnapDog2.Core.Models;

/// <summary>
/// Notification published when a client's volume status changes.
/// </summary>
[StatusId("CLIENT_VOLUME_STATUS")]
public record ClientVolumeStatusNotification(int ClientIndex, int Volume) : INotification;

/// <summary>
/// Notification published when a client's mute status changes.
/// </summary>
[StatusId("CLIENT_MUTE_STATUS")]
public record ClientMuteStatusNotification(int ClientIndex, bool Muted) : INotification;

/// <summary>
/// Notification published when a client's latency status changes.
/// </summary>
[StatusId("CLIENT_LATENCY_STATUS")]
public record ClientLatencyStatusNotification(int ClientIndex, int LatencyMs) : INotification;

/// <summary>
/// Notification published when a client's zone assignment changes.
/// </summary>
[StatusId("CLIENT_ZONE_STATUS")]
public record ClientZoneStatusNotification(int ClientIndex, int? ZoneIndex) : INotification;

/// <summary>
/// Notification published when a client's connection status changes.
/// </summary>
[StatusId("CLIENT_CONNECTED")]
public record ClientConnectionStatusNotification(int ClientIndex, bool IsConnected) : INotification;

/// <summary>
/// Notification published when a client's complete state changes.
/// </summary>
[StatusId("CLIENT_STATE")]
public record ClientStateNotification(int ClientIndex, ClientState State) : INotification;
