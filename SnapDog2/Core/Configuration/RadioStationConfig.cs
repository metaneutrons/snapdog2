namespace SnapDog2.Core.Configuration;

using System.ComponentModel.DataAnnotations;
using EnvoyConfig.Attributes;

/// <summary>
/// Configuration for a radio station.
/// Maps environment variables like SNAPDOG_RADIO_X_* to properties.
/// </summary>
public class RadioStationConfig
{
    /// <summary>
    /// Display name of the radio station.
    /// Maps to: SNAPDOG_RADIO_X_NAME
    /// </summary>
    [Env(Key = "NAME")]
    [Required]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Stream URL of the radio station.
    /// Maps to: SNAPDOG_RADIO_X_URL
    /// </summary>
    [Env(Key = "URL")]
    [Required]
    [Url]
    public string Url { get; set; } = null!;
}
