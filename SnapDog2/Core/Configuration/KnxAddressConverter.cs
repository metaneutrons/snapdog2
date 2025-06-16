using System;
using EnvoyConfig.Conversion;
using EnvoyConfig.Logging;

namespace SnapDog2.Core.Configuration;

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
            {
                return null;
            }

            // For non-nullable, this will be handled by EnvoyConfig's validation
            return null;
        }

        if (KnxAddress.TryParse(value, out var result))
        {
            return result;
        }

        // Log the conversion error if logger is available
        logger?.Log(
            EnvLogLevel.Error,
            $"Failed to parse KNX address '{value}'. Expected format: 'Main/Middle/Sub' (e.g., '2/1/1')."
        );

        // Return null for invalid values - EnvoyConfig will handle the error appropriately
        return null;
    }

    /// <summary>
    /// Converts a string value from configuration to a <see cref="KnxAddress"/>.
    /// Legacy method for backward compatibility.
    /// </summary>
    /// <param name="value">The string value to convert.</param>
    /// <returns>A <see cref="KnxAddress"/> instance if conversion is successful; otherwise, null.</returns>
    public static KnxAddress? ConvertFromString(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (KnxAddress.TryParse(value, out var result))
        {
            return result;
        }

        return null;
    }

    /// <summary>
    /// Converts a <see cref="KnxAddress"/> to its string representation.
    /// </summary>
    /// <param name="address">The address to convert.</param>
    /// <returns>The string representation of the address.</returns>
    public static string ConvertToString(KnxAddress? address)
    {
        return address?.ToString() ?? string.Empty;
    }

    /// <summary>
    /// Validates if a string can be converted to a valid <see cref="KnxAddress"/>.
    /// </summary>
    /// <param name="value">The string value to validate.</param>
    /// <param name="errorMessage">The error message if validation fails.</param>
    /// <returns>True if the value is valid; otherwise, false.</returns>
    public static bool IsValid(string? value, out string errorMessage)
    {
        errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(value))
        {
            return true; // Null/empty is valid for nullable KnxAddress
        }

        if (KnxAddress.TryParse(value, out _))
        {
            return true;
        }

        errorMessage = $"Invalid KNX address format: '{value}'. Expected format: 'Main/Middle/Sub' (e.g., '2/1/1').";
        return false;
    }
}
