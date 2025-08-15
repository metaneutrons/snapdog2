namespace SnapDog2.Core.Configuration;

using System.ComponentModel.DataAnnotations;
using EnvoyConfig.Attributes;

/// <summary>
/// Configuration for an individual audio zone.
/// Maps environment variables like SNAPDOG_ZONE_X_* to properties.
/// </summary>
public class ZoneConfig
{
    /// <summary>
    /// Display name of the zone.
    /// Maps to: SNAPDOG_ZONE_X_NAME
    /// </summary>
    [Env(Key = "NAME")]
    [Required]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Snapcast sink path for this zone.
    /// Maps to: SNAPDOG_ZONE_X_SINK
    /// </summary>
    [Env(Key = "SINK")]
    [Required]
    public string Sink { get; set; } = null!;

    /// <summary>
    /// MQTT configuration for this zone.
    /// Maps environment variables with prefix: SNAPDOG_ZONE_X_MQTT_*
    /// </summary>
    [Env(NestedPrefix = "MQTT_")]
    public ZoneMqttConfig Mqtt { get; set; } = new();

    /// <summary>
    /// KNX configuration for this zone.
    /// Maps environment variables with prefix: SNAPDOG_ZONE_X_KNX_*
    /// </summary>
    [Env(NestedPrefix = "KNX_")]
    public ZoneKnxConfig Knx { get; set; } = new();
}
