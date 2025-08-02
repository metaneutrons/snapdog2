namespace SnapDog2.Core.Models;

using System;
using System.Collections.Generic;

/// <summary>
/// Represents MQTT topic configuration for a zone.
/// </summary>
public record ZoneMqttTopics
{
    /// <summary>
    /// Gets the base topic path.
    /// </summary>
    public required string BaseTopic { get; init; }

    /// <summary>
    /// Gets the control command topics (subscribe).
    /// </summary>
    public required ZoneControlTopics Control { get; init; }

    /// <summary>
    /// Gets the status publishing topics (publish).
    /// </summary>
    public required ZoneStatusTopics Status { get; init; }
}

/// <summary>
/// Represents zone control topics for subscribing to commands.
/// </summary>
public record ZoneControlTopics
{
    public required string ControlSet { get; init; }
    public required string TrackSet { get; init; }
    public required string TrackRepeatSet { get; init; }
    public required string PlaylistSet { get; init; }
    public required string PlaylistRepeatSet { get; init; }
    public required string PlaylistShuffleSet { get; init; }
    public required string VolumeSet { get; init; }
    public required string MuteSet { get; init; }
}

/// <summary>
/// Represents zone status topics for publishing state.
/// </summary>
public record ZoneStatusTopics
{
    public required string Control { get; init; }
    public required string Track { get; init; }
    public required string TrackInfo { get; init; }
    public required string TrackRepeat { get; init; }
    public required string Playlist { get; init; }
    public required string PlaylistInfo { get; init; }
    public required string PlaylistRepeat { get; init; }
    public required string PlaylistShuffle { get; init; }
    public required string Volume { get; init; }
    public required string Mute { get; init; }
    public required string State { get; init; }
}

/// <summary>
/// Represents MQTT topic configuration for a client.
/// </summary>
public record ClientMqttTopics
{
    /// <summary>
    /// Gets the base topic path.
    /// </summary>
    public required string BaseTopic { get; init; }

    /// <summary>
    /// Gets the control command topics (subscribe).
    /// </summary>
    public required ClientControlTopics Control { get; init; }

    /// <summary>
    /// Gets the status publishing topics (publish).
    /// </summary>
    public required ClientStatusTopics Status { get; init; }
}

/// <summary>
/// Represents client control topics for subscribing to commands.
/// </summary>
public record ClientControlTopics
{
    public required string VolumeSet { get; init; }
    public required string MuteSet { get; init; }
    public required string LatencySet { get; init; }
    public required string ZoneSet { get; init; }
}

/// <summary>
/// Represents client status topics for publishing state.
/// </summary>
public record ClientStatusTopics
{
    public required string Connected { get; init; }
    public required string Volume { get; init; }
    public required string Mute { get; init; }
    public required string Latency { get; init; }
    public required string Zone { get; init; }
    public required string State { get; init; }
}
