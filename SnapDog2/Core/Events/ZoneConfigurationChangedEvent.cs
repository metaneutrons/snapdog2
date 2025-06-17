using System.Collections.Immutable;

namespace SnapDog2.Core.Events;

/// <summary>
/// Domain event triggered when a zone's configuration is changed.
/// Contains information about the zone and what configuration aspects were modified.
/// </summary>
public sealed record ZoneConfigurationChangedEvent : DomainEvent
{
    /// <summary>
    /// Gets the unique identifier of the zone.
    /// </summary>
    public required string ZoneId { get; init; }

    /// <summary>
    /// Gets the name of the zone.
    /// </summary>
    public required string ZoneName { get; init; }

    /// <summary>
    /// Gets the list of client IDs assigned to the zone after the change.
    /// </summary>
    public ImmutableList<string> ClientIds { get; init; } = ImmutableList<string>.Empty;

    /// <summary>
    /// Gets the list of client IDs that were added to the zone.
    /// </summary>
    public ImmutableList<string> AddedClientIds { get; init; } = ImmutableList<string>.Empty;

    /// <summary>
    /// Gets the list of client IDs that were removed from the zone.
    /// </summary>
    public ImmutableList<string> RemovedClientIds { get; init; } = ImmutableList<string>.Empty;

    /// <summary>
    /// Gets the current stream ID assigned to the zone, if any.
    /// </summary>
    public string? CurrentStreamId { get; init; }

    /// <summary>
    /// Gets the previous stream ID that was assigned to the zone, if any.
    /// </summary>
    public string? PreviousStreamId { get; init; }

    /// <summary>
    /// Gets a value indicating whether the zone's enabled status changed.
    /// </summary>
    public bool IsEnabledChanged { get; init; }

    /// <summary>
    /// Gets the current enabled status of the zone.
    /// </summary>
    public bool IsEnabled { get; init; }

    /// <summary>
    /// Gets a value indicating whether the zone's volume settings changed.
    /// </summary>
    public bool VolumeSettingsChanged { get; init; }

    /// <summary>
    /// Gets the new default volume level, if changed.
    /// </summary>
    public int? NewDefaultVolume { get; init; }

    /// <summary>
    /// Gets the new minimum volume level, if changed.
    /// </summary>
    public int? NewMinVolume { get; init; }

    /// <summary>
    /// Gets the new maximum volume level, if changed.
    /// </summary>
    public int? NewMaxVolume { get; init; }

    /// <summary>
    /// Gets additional configuration properties that were changed.
    /// </summary>
    public ImmutableDictionary<string, object?> ChangedProperties { get; init; } =
        ImmutableDictionary<string, object?>.Empty;

    /// <summary>
    /// Gets the reason for the configuration change.
    /// </summary>
    public string? ChangeReason { get; init; }

    /// <summary>
    /// Gets the user or system that initiated the change.
    /// </summary>
    public string? ChangedBy { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ZoneConfigurationChangedEvent"/> record.
    /// </summary>
    public ZoneConfigurationChangedEvent() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ZoneConfigurationChangedEvent"/> record with a correlation ID.
    /// </summary>
    /// <param name="correlationId">The correlation ID to associate with this event.</param>
    public ZoneConfigurationChangedEvent(string? correlationId)
        : base(correlationId) { }

    /// <summary>
    /// Creates a new zone configuration changed event.
    /// </summary>
    /// <param name="zoneId">The unique identifier of the zone.</param>
    /// <param name="zoneName">The name of the zone.</param>
    /// <param name="clientIds">The list of client IDs assigned to the zone after the change.</param>
    /// <param name="addedClientIds">The list of client IDs that were added to the zone.</param>
    /// <param name="removedClientIds">The list of client IDs that were removed from the zone.</param>
    /// <param name="currentStreamId">The current stream ID assigned to the zone, if any.</param>
    /// <param name="previousStreamId">The previous stream ID that was assigned to the zone, if any.</param>
    /// <param name="isEnabledChanged">Whether the zone's enabled status changed.</param>
    /// <param name="isEnabled">The current enabled status of the zone.</param>
    /// <param name="volumeSettingsChanged">Whether the zone's volume settings changed.</param>
    /// <param name="newDefaultVolume">The new default volume level, if changed.</param>
    /// <param name="newMinVolume">The new minimum volume level, if changed.</param>
    /// <param name="newMaxVolume">The new maximum volume level, if changed.</param>
    /// <param name="changedProperties">Additional configuration properties that were changed.</param>
    /// <param name="changeReason">The reason for the configuration change.</param>
    /// <param name="changedBy">The user or system that initiated the change.</param>
    /// <param name="correlationId">The correlation ID to associate with this event.</param>
    /// <returns>A new <see cref="ZoneConfigurationChangedEvent"/> instance.</returns>
    public static ZoneConfigurationChangedEvent Create(
        string zoneId,
        string zoneName,
        IEnumerable<string>? clientIds = null,
        IEnumerable<string>? addedClientIds = null,
        IEnumerable<string>? removedClientIds = null,
        string? currentStreamId = null,
        string? previousStreamId = null,
        bool isEnabledChanged = false,
        bool isEnabled = true,
        bool volumeSettingsChanged = false,
        int? newDefaultVolume = null,
        int? newMinVolume = null,
        int? newMaxVolume = null,
        IReadOnlyDictionary<string, object?>? changedProperties = null,
        string? changeReason = null,
        string? changedBy = null,
        string? correlationId = null
    )
    {
        return new ZoneConfigurationChangedEvent(correlationId)
        {
            ZoneId = zoneId,
            ZoneName = zoneName,
            ClientIds = clientIds?.ToImmutableList() ?? ImmutableList<string>.Empty,
            AddedClientIds = addedClientIds?.ToImmutableList() ?? ImmutableList<string>.Empty,
            RemovedClientIds = removedClientIds?.ToImmutableList() ?? ImmutableList<string>.Empty,
            CurrentStreamId = currentStreamId,
            PreviousStreamId = previousStreamId,
            IsEnabledChanged = isEnabledChanged,
            IsEnabled = isEnabled,
            VolumeSettingsChanged = volumeSettingsChanged,
            NewDefaultVolume = newDefaultVolume,
            NewMinVolume = newMinVolume,
            NewMaxVolume = newMaxVolume,
            ChangedProperties =
                changedProperties?.ToImmutableDictionary() ?? ImmutableDictionary<string, object?>.Empty,
            ChangeReason = changeReason,
            ChangedBy = changedBy,
        };
    }

    /// <summary>
    /// Gets a value indicating whether clients were added or removed from the zone.
    /// </summary>
    public bool HasClientChanges => AddedClientIds.Count > 0 || RemovedClientIds.Count > 0;

    /// <summary>
    /// Gets a value indicating whether the zone's stream assignment changed.
    /// </summary>
    public bool HasStreamChange => CurrentStreamId != PreviousStreamId;
}
