using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SnapDog2.Core.Configuration;

/// <summary>
/// Converts a <see cref="KnxAddress"/> to and from JSON string.
/// </summary>
public class KnxAddressConverter : JsonConverter<KnxAddress>
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
            // Or return default, or throw, depending on desired behavior for empty/null strings
            throw new JsonException("KnxAddress string cannot be null or empty.");
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
}
