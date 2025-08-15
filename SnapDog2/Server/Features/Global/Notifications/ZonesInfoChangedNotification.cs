namespace SnapDog2.Server.Features.Global.Notifications;

using System.Collections.Generic;
using Cortex.Mediator.Notifications;
using SnapDog2.Core.Attributes;

/// <summary>
/// Notification published when the system zones information changes.
/// </summary>
[StatusId("ZONES_INFO")]
public record ZonesInfoChangedNotification(IReadOnlyList<int> ZoneIndices) : INotification;
