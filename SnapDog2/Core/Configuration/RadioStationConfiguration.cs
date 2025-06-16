using EnvoyConfig.Attributes;

namespace SnapDog2.Core.Configuration;

/// <summary>
/// Configuration for radio stations.
/// Enhanced with EnvoyConfig attributes for environment variable mapping.
/// Maps environment variables like SNAPDOG_RADIO_X_* to properties.
///
/// Examples:
/// - SNAPDOG_RADIO_1_NAME → Name
/// - SNAPDOG_RADIO_1_URL → Url
/// - SNAPDOG_RADIO_1_DESCRIPTION → Description
/// - SNAPDOG_RADIO_1_ENABLED → Enabled
/// </summary>
public class RadioStationConfiguration
{
    /// <summary>
    /// Gets or sets the display name of the radio station.
    /// Maps to: SNAPDOG_RADIO_X_NAME
    /// </summary>
    [Env(Key = "NAME", Default = "")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the URL of the radio station stream.
    /// Maps to: SNAPDOG_RADIO_X_URL
    /// </summary>
    [Env(Key = "URL", Default = "")]
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the radio station description.
    /// Maps to: SNAPDOG_RADIO_X_DESCRIPTION
    /// </summary>
    [Env(Key = "DESCRIPTION", Default = "")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the station is enabled.
    /// Maps to: SNAPDOG_RADIO_X_ENABLED
    /// </summary>
    [Env(Key = "ENABLED", Default = true)]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the radio station genre.
    /// Maps to: SNAPDOG_RADIO_X_GENRE
    /// </summary>
    [Env(Key = "GENRE", Default = "")]
    public string Genre { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the radio station country/region.
    /// Maps to: SNAPDOG_RADIO_X_COUNTRY
    /// </summary>
    [Env(Key = "COUNTRY", Default = "")]
    public string Country { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the radio station language.
    /// Maps to: SNAPDOG_RADIO_X_LANGUAGE
    /// </summary>
    [Env(Key = "LANGUAGE", Default = "")]
    public string Language { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the bitrate of the stream in kbps.
    /// Maps to: SNAPDOG_RADIO_X_BITRATE
    /// </summary>
    [Env(Key = "BITRATE", Default = 128)]
    public int Bitrate { get; set; } = 128;

    /// <summary>
    /// Gets or sets the stream format (mp3, aac, etc.).
    /// Maps to: SNAPDOG_RADIO_X_FORMAT
    /// </summary>
    [Env(Key = "FORMAT", Default = "mp3")]
    public string Format { get; set; } = "mp3";

    /// <summary>
    /// Gets or sets the station logo/icon URL.
    /// Maps to: SNAPDOG_RADIO_X_LOGO_URL
    /// </summary>
    [Env(Key = "LOGO_URL", Default = "")]
    public string LogoUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the station website URL.
    /// Maps to: SNAPDOG_RADIO_X_WEBSITE
    /// </summary>
    [Env(Key = "WEBSITE", Default = "")]
    public string Website { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional tags for the station (comma-separated).
    /// Maps to: SNAPDOG_RADIO_X_TAGS
    /// </summary>
    [Env(Key = "TAGS", Default = "")]
    public string Tags { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the station priority for display ordering (higher = more priority).
    /// Maps to: SNAPDOG_RADIO_X_PRIORITY
    /// </summary>
    [Env(Key = "PRIORITY", Default = 1)]
    public int Priority { get; set; } = 1;

    /// <summary>
    /// Gets or sets whether the station requires authentication.
    /// Maps to: SNAPDOG_RADIO_X_REQUIRES_AUTH
    /// </summary>
    [Env(Key = "REQUIRES_AUTH", Default = false)]
    public bool RequiresAuth { get; set; } = false;

    /// <summary>
    /// Gets or sets the username for authentication (if required).
    /// Maps to: SNAPDOG_RADIO_X_USERNAME
    /// </summary>
    [Env(Key = "USERNAME", Default = "")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password for authentication (if required).
    /// Maps to: SNAPDOG_RADIO_X_PASSWORD
    /// </summary>
    [Env(Key = "PASSWORD", Default = "")]
    public string Password { get; set; } = string.Empty;
}
