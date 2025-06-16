using EnvoyConfig.Attributes;

namespace EnvoyConfig.Sample
{
    /// <summary>
    /// Configuration for an individual radio station.
    /// Maps environment variables like SNAPDOG_RADIO_X_* to properties.
    /// </summary>
    public class RadioStation
    {
        /// <summary>
        /// Gets or sets the display name of the radio station.
        /// Maps to: SNAPDOG_RADIO_X_NAME
        /// </summary>
        [Env(Key = "NAME")]
        public string Name { get; set; } = null!;

        /// <summary>
        /// Gets or sets the URL of the radio station stream.
        /// Maps to: SNAPDOG_RADIO_X_URL
        /// </summary>
        [Env(Key = "URL")]
        public string URL { get; set; } = null!;
    }
}
