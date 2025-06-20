using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using EnvoyConfig.Conversion; // For ITypeConverter
using EnvoyConfig.Logging;   // For IEnvLogSink (assumed namespace)

namespace SnapDog2.Core.Configuration;

/// <summary>
/// Converts a <see cref="KnxAddress"/> to and from JSON string (for System.Text.Json)
/// and from string for EnvoyConfig.
/// </summary>
public class KnxAddressConverter : JsonConverter<KnxAddress>, ITypeConverter // Implement ITypeConverter
{
    /// <inheritdoc />
    public override KnxAddress Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"Expected string for KnxAddress, got {reader.TokenType}.");
        }

        var addressString = reader.GetString();
        if (string.IsNullOrWhiteSpace(addressString))
        {
            // For System.Text.Json, null might be handled by JsonConverter<T?> or options
            // For direct parsing, KnxAddress.Parse might handle it or throw.
            // If KnxAddress.Parse throws on null/empty, this check is fine.
            // Consider if KnxAddress? should return null here if addressString is null/empty.
            throw new JsonException("KnxAddress string cannot be null or empty for JSON deserialization if target is non-nullable KnxAddress.");
        }

        try
        {
            return KnxAddress.Parse(addressString);
        }
        catch (FormatException ex)
        {
            throw new JsonException($"Invalid KnxAddress format: {addressString}", ex);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            throw new JsonException($"KnxAddress component out of range: {addressString}", ex);
        }
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, KnxAddress value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }

    // Implementation for EnvoyConfig.Conversion.ITypeConverter
    public object? Convert(string? value, Type targetType, IEnvLogSink? logSink) // Signature updated
    {
        // logSink is available here if logging is needed. For now, it's unused.

        if (value == null)
        {
            // If targetType is KnxAddress? (nullable), return null.
            // If targetType is KnxAddress (non-nullable), this might be an issue,
            // depending on EnvoyConfig's expectations for null input for non-nullable types.
            // Typically, for nullable reference types or Nullable<value type>, null is valid.
            if (targetType == typeof(KnxAddress?) || Nullable.GetUnderlyingType(targetType) != null)
            {
                return null;
            }
            // Or throw new ArgumentNullException(nameof(value), "Input string cannot be null for non-nullable KnxAddress.");
            // Let KnxAddress.Parse handle it if it's more robust for empty strings etc.
        }

        if (string.IsNullOrWhiteSpace(value))
        {
             // Similar to null, if target is KnxAddress?, return null.
            if (targetType == typeof(KnxAddress?) || Nullable.GetUnderlyingType(targetType) != null)
            {
                return null;
            }
            // For non-nullable KnxAddress, an empty string might be invalid.
            // KnxAddress.Parse should ideally throw for invalid formats.
            // Or throw new ArgumentException("Input string cannot be empty or whitespace.", nameof(value));
        }

        try
        {
            // KnxAddress.Parse should handle various string formats and throw if invalid.
            return KnxAddress.Parse(value!); // value is not null or whitespace here due to checks or subsequent Parse logic
        }
        catch (Exception ex) // Catching generic Exception to be safe, could be FormatException, ArgumentOutOfRangeException etc.
        {
            // EnvoyConfig might have its own error handling/logging.
            // Re-throwing or returning null might be options.
            // Consider if EnvoyConfig expects a specific exception type.
            throw new InvalidOperationException($"Failed to convert string '{value}' to KnxAddress: {ex.Message}", ex);
        }
    }
}
