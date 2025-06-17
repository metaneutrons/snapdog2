namespace SnapDog2.Core.Events;

/// <summary>
/// Domain event triggered when a client's or zone's volume changes.
/// Contains information about the volume change and the entity affected.
/// </summary>
public sealed record VolumeChangedEvent : DomainEvent
{
    /// <summary>
    /// Gets the type of entity whose volume was changed.
    /// </summary>
    public required VolumeEntityType EntityType { get; init; }

    /// <summary>
    /// Gets the unique identifier of the entity (client ID or zone ID).
    /// </summary>
    public required string EntityId { get; init; }

    /// <summary>
    /// Gets the name of the entity.
    /// </summary>
    public required string EntityName { get; init; }

    /// <summary>
    /// Gets the previous volume level (0-100).
    /// </summary>
    public required int PreviousVolume { get; init; }

    /// <summary>
    /// Gets the new volume level (0-100).
    /// </summary>
    public required int NewVolume { get; init; }

    /// <summary>
    /// Gets the previous mute status.
    /// </summary>
    public bool PreviousMuteStatus { get; init; }

    /// <summary>
    /// Gets the new mute status.
    /// </summary>
    public bool NewMuteStatus { get; init; }

    /// <summary>
    /// Gets the zone ID associated with the volume change (for clients).
    /// </summary>
    public string? ZoneId { get; init; }

    /// <summary>
    /// Gets the user or system that initiated the volume change.
    /// </summary>
    public string? ChangedBy { get; init; }

    /// <summary>
    /// Gets the reason for the volume change.
    /// </summary>
    public string? ChangeReason { get; init; }

    /// <summary>
    /// Gets a value indicating whether the change was automatic (e.g., system initiated).
    /// </summary>
    public bool IsAutomaticChange { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="VolumeChangedEvent"/> record.
    /// </summary>
    public VolumeChangedEvent() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="VolumeChangedEvent"/> record with a correlation ID.
    /// </summary>
    /// <param name="correlationId">The correlation ID to associate with this event.</param>
    public VolumeChangedEvent(string? correlationId)
        : base(correlationId) { }

    /// <summary>
    /// Creates a new volume changed event for a client.
    /// </summary>
    /// <param name="clientId">The unique identifier of the client.</param>
    /// <param name="clientName">The name of the client.</param>
    /// <param name="previousVolume">The previous volume level.</param>
    /// <param name="newVolume">The new volume level.</param>
    /// <param name="previousMuteStatus">The previous mute status.</param>
    /// <param name="newMuteStatus">The new mute status.</param>
    /// <param name="zoneId">The zone ID associated with the client.</param>
    /// <param name="changedBy">The user or system that initiated the change.</param>
    /// <param name="changeReason">The reason for the volume change.</param>
    /// <param name="isAutomaticChange">Whether the change was automatic.</param>
    /// <param name="correlationId">The correlation ID to associate with this event.</param>
    /// <returns>A new <see cref="VolumeChangedEvent"/> instance for a client.</returns>
    public static VolumeChangedEvent CreateForClient(
        string clientId,
        string clientName,
        int previousVolume,
        int newVolume,
        bool previousMuteStatus = false,
        bool newMuteStatus = false,
        string? zoneId = null,
        string? changedBy = null,
        string? changeReason = null,
        bool isAutomaticChange = false,
        string? correlationId = null
    )
    {
        return new VolumeChangedEvent(correlationId)
        {
            EntityType = VolumeEntityType.Client,
            EntityId = clientId,
            EntityName = clientName,
            PreviousVolume = previousVolume,
            NewVolume = newVolume,
            PreviousMuteStatus = previousMuteStatus,
            NewMuteStatus = newMuteStatus,
            ZoneId = zoneId,
            ChangedBy = changedBy,
            ChangeReason = changeReason,
            IsAutomaticChange = isAutomaticChange,
        };
    }

    /// <summary>
    /// Creates a new volume changed event for a zone.
    /// </summary>
    /// <param name="zoneId">The unique identifier of the zone.</param>
    /// <param name="zoneName">The name of the zone.</param>
    /// <param name="previousVolume">The previous default volume level.</param>
    /// <param name="newVolume">The new default volume level.</param>
    /// <param name="changedBy">The user or system that initiated the change.</param>
    /// <param name="changeReason">The reason for the volume change.</param>
    /// <param name="isAutomaticChange">Whether the change was automatic.</param>
    /// <param name="correlationId">The correlation ID to associate with this event.</param>
    /// <returns>A new <see cref="VolumeChangedEvent"/> instance for a zone.</returns>
    public static VolumeChangedEvent CreateForZone(
        string zoneId,
        string zoneName,
        int previousVolume,
        int newVolume,
        string? changedBy = null,
        string? changeReason = null,
        bool isAutomaticChange = false,
        string? correlationId = null
    )
    {
        return new VolumeChangedEvent(correlationId)
        {
            EntityType = VolumeEntityType.Zone,
            EntityId = zoneId,
            EntityName = zoneName,
            PreviousVolume = previousVolume,
            NewVolume = newVolume,
            PreviousMuteStatus = false,
            NewMuteStatus = false,
            ZoneId = zoneId,
            ChangedBy = changedBy,
            ChangeReason = changeReason,
            IsAutomaticChange = isAutomaticChange,
        };
    }

    /// <summary>
    /// Gets the volume change amount (positive for increase, negative for decrease).
    /// </summary>
    public int VolumeChange => NewVolume - PreviousVolume;

    /// <summary>
    /// Gets a value indicating whether the volume was increased.
    /// </summary>
    public bool IsVolumeIncrease => VolumeChange > 0;

    /// <summary>
    /// Gets a value indicating whether the volume was decreased.
    /// </summary>
    public bool IsVolumeDecrease => VolumeChange < 0;

    /// <summary>
    /// Gets a value indicating whether the mute status changed.
    /// </summary>
    public bool IsMuteStatusChanged => PreviousMuteStatus != NewMuteStatus;

    /// <summary>
    /// Gets a value indicating whether the entity was muted.
    /// </summary>
    public bool WasMuted => !PreviousMuteStatus && NewMuteStatus;

    /// <summary>
    /// Gets a value indicating whether the entity was unmuted.
    /// </summary>
    public bool WasUnmuted => PreviousMuteStatus && !NewMuteStatus;

    /// <summary>
    /// Gets a value indicating whether this is a client volume change.
    /// </summary>
    public bool IsClientVolumeChange => EntityType == VolumeEntityType.Client;

    /// <summary>
    /// Gets a value indicating whether this is a zone volume change.
    /// </summary>
    public bool IsZoneVolumeChange => EntityType == VolumeEntityType.Zone;
}

/// <summary>
/// Represents the type of entity whose volume was changed.
/// </summary>
public enum VolumeEntityType
{
    /// <summary>
    /// A Snapcast client's volume was changed.
    /// </summary>
    Client,

    /// <summary>
    /// A zone's default volume was changed.
    /// </summary>
    Zone,
}
