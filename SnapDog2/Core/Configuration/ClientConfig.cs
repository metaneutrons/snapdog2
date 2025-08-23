namespace SnapDog2.Core.Configuration;

using System.ComponentModel.DataAnnotations;
using EnvoyConfig.Attributes;

/// <summary>
/// Configuration for an individual client device.
/// Maps environment variables like SNAPDOG_CLIENT_X_* to properties.
/// </summary>
public class ClientConfig
{
    /// <summary>
    /// Display name of the client.
    /// Maps to: SNAPDOG_CLIENT_X_NAME
    /// </summary>
    [Env(Key = "NAME")]
    [Required]
    public string Name { get; set; } = null!;

    /// <summary>
    /// MAC address of the client device.
    /// Maps to: SNAPDOG_CLIENT_X_MAC
    /// </summary>
    [Env(Key = "MAC")]
    [RegularExpression(
        @"^([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})$",
        ErrorMessage = "MAC address must be in format XX:XX:XX:XX:XX:XX"
    )]
    public string? Mac { get; set; }

    /// <summary>
    /// Default zone ID for this client (1-based).
    /// Maps to: SNAPDOG_CLIENT_X_DEFAULT_ZONE
    /// </summary>
    [Env(Key = "DEFAULT_ZONE", Default = 1)]
    [Range(1, 100)]
    public int DefaultZone { get; set; } = 1;

    /// <summary>
    /// KNX configuration for this client.
    /// Maps environment variables with prefix: SNAPDOG_CLIENT_X_KNX_*
    /// </summary>
    [Env(NestedPrefix = "KNX_")]
    public ClientKnxConfig Knx { get; set; } = new();
}
