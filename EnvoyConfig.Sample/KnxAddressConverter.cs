using System;
using EnvoyConfig.Conversion;
using EnvoyConfig.Logging;

namespace EnvoyConfig.Sample
{
    /// <summary>
    /// Custom type converter for <see cref="KnxAddress"/> to integrate with EnvoyConfig.
    /// Converts between string environment variable values and <see cref="KnxAddress"/> instances.
    /// </summary>
    public class KnxAddressConverter : ITypeConverter
    {
        /// <summary>
        /// Converts a string value from an environment variable to a <see cref="KnxAddress"/>.
        /// </summary>
        /// <param name="value">The string value to convert.</param>
        /// <param name="targetType">The target type (should be <see cref="KnxAddress"/> or <see cref="Nullable{KnxAddress}"/>).</param>
        /// <param name="logger">Optional logger for warnings and errors.</param>
        /// <returns>A <see cref="KnxAddress"/> instance if conversion is successful; otherwise, null.</returns>
        public object? Convert(string? value, Type targetType, IEnvLogSink? logger)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                // Handle nullable KnxAddress
                if (targetType == typeof(KnxAddress?))
                    return null;

                // For non-nullable, this will be handled by EnvoyConfig's validation
                return null;
            }

            if (KnxAddress.TryParse(value, out var result))
                return result;

            // Log the conversion error if logger is available
            logger?.Log(
                EnvLogLevel.Error,
                $"Failed to parse KNX address '{value}'. Expected format: 'Main/Middle/Sub' (e.g., '2/1/1')."
            );

            // Return null for invalid values - EnvoyConfig will handle the error appropriately
            return null;
        }
    }
}
