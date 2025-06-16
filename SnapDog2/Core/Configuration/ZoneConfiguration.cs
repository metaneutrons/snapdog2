using EnvoyConfig.Attributes;

namespace SnapDog2.Core.Configuration;

/// <summary>
/// Zone configuration for SnapDog2 multi-room audio system.
/// Maps environment variables with SNAPDOG_ZONE_X_ prefix pattern.
///
/// Examples:
/// - SNAPDOG_ZONE_1_NAME → Zones[0].Name
/// - SNAPDOG_ZONE_1_DESCRIPTION → Zones[0].Description
/// - SNAPDOG_ZONE_2_ENABLED → Zones[1].Enabled
/// - SNAPDOG_ZONE_1_MQTT_TOPIC → Zones[0].MqttTopic
/// </summary>
public class ZoneConfiguration
{
    /// <summary>
    /// Gets or sets the zone identifier (unique within the system).
    /// Maps to: SNAPDOG_ZONE_X_ID
    /// </summary>
    [Env(Key = "ID", Default = 1)]
    public int Id { get; set; } = 1;

    /// <summary>
    /// Gets or sets the display name of the zone.
    /// Maps to: SNAPDOG_ZONE_X_NAME
    /// </summary>
    [Env(Key = "NAME", Default = "")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the zone description.
    /// Maps to: SNAPDOG_ZONE_X_DESCRIPTION
    /// </summary>
    [Env(Key = "DESCRIPTION", Default = "")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the zone is enabled.
    /// Maps to: SNAPDOG_ZONE_X_ENABLED
    /// </summary>
    [Env(Key = "ENABLED", Default = true)]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the zone icon identifier.
    /// Maps to: SNAPDOG_ZONE_X_ICON
    /// </summary>
    [Env(Key = "ICON", Default = "speaker")]
    public string Icon { get; set; } = "speaker";

    /// <summary>
    /// Gets or sets the zone color (hex code).
    /// Maps to: SNAPDOG_ZONE_X_COLOR
    /// </summary>
    [Env(Key = "COLOR", Default = "#007bff")]
    public string Color { get; set; } = "#007bff";

    /// <summary>
    /// Gets or sets the default volume level for this zone (0-100).
    /// Maps to: SNAPDOG_ZONE_X_DEFAULT_VOLUME
    /// </summary>
    [Env(Key = "DEFAULT_VOLUME", Default = 50)]
    public int DefaultVolume { get; set; } = 50;

    /// <summary>
    /// Gets or sets the maximum volume level for this zone (0-100).
    /// Maps to: SNAPDOG_ZONE_X_MAX_VOLUME
    /// </summary>
    [Env(Key = "MAX_VOLUME", Default = 100)]
    public int MaxVolume { get; set; } = 100;

    /// <summary>
    /// Gets or sets the minimum volume level for this zone (0-100).
    /// Maps to: SNAPDOG_ZONE_X_MIN_VOLUME
    /// </summary>
    [Env(Key = "MIN_VOLUME", Default = 0)]
    public int MinVolume { get; set; } = 0;

    /// <summary>
    /// Gets or sets whether the zone starts muted by default.
    /// Maps to: SNAPDOG_ZONE_X_DEFAULT_MUTED
    /// </summary>
    [Env(Key = "DEFAULT_MUTED", Default = false)]
    public bool DefaultMuted { get; set; } = false;

    /// <summary>
    /// Gets or sets the audio delay/latency for this zone in milliseconds.
    /// Maps to: SNAPDOG_ZONE_X_LATENCY
    /// </summary>
    [Env(Key = "LATENCY", Default = 0)]
    public int LatencyMs { get; set; } = 0;

    /// <summary>
    /// Gets or sets the MQTT topic prefix for this zone.
    /// Maps to: SNAPDOG_ZONE_X_MQTT_TOPIC
    /// </summary>
    [Env(Key = "MQTT_TOPIC", Default = "")]
    public string MqttTopic { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the physical location/room of this zone.
    /// Maps to: SNAPDOG_ZONE_X_LOCATION
    /// </summary>
    [Env(Key = "LOCATION", Default = "")]
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the zone priority for automatic assignments (higher = more priority).
    /// Maps to: SNAPDOG_ZONE_X_PRIORITY
    /// </summary>
    [Env(Key = "PRIORITY", Default = 1)]
    public int Priority { get; set; } = 1;

    /// <summary>
    /// Gets or sets additional zone tags (comma-separated).
    /// Maps to: SNAPDOG_ZONE_X_TAGS
    /// </summary>
    [Env(Key = "TAGS", Default = "")]
    public string Tags { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this zone supports stereo playback.
    /// Maps to: SNAPDOG_ZONE_X_STEREO_ENABLED
    /// </summary>
    [Env(Key = "STEREO_ENABLED", Default = true)]
    public bool StereoEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the audio quality setting for this zone.
    /// Maps to: SNAPDOG_ZONE_X_AUDIO_QUALITY (low, medium, high, lossless)
    /// </summary>
    [Env(Key = "AUDIO_QUALITY", Default = "high")]
    public string AudioQuality { get; set; } = "high";

    /// <summary>
    /// Gets or sets whether zone grouping is allowed.
    /// Maps to: SNAPDOG_ZONE_X_GROUPING_ENABLED
    /// </summary>
    [Env(Key = "GROUPING_ENABLED", Default = true)]
    public bool GroupingEnabled { get; set; } = true;
}
