using System.Reflection;
using Microsoft.Extensions.Logging;

namespace KnxMonitor.Services;

/// <summary>
/// The CORRECT Falcon-first DPT service that works with live KNX events.
/// This service is designed to extract and format values that Falcon SDK already decoded!
/// </summary>
public class CorrectFalconFirstService : IDptDecodingService
{
    private readonly ILogger<CorrectFalconFirstService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CorrectFalconFirstService"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public CorrectFalconFirstService(ILogger<CorrectFalconFirstService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Extracts DPT information from a Falcon SDK object (from live KNX events).
    /// This is the RIGHT way - extract from what Falcon SDK already provides!
    /// </summary>
    /// <param name="falconObject">Object from GroupEventArgs.Value.</param>
    /// <returns>Extracted DPT information.</returns>
    public FalconExtractedInfo ExtractFromFalconObject(object? falconObject)
    {
        if (falconObject == null)
        {
            return new FalconExtractedInfo(null, null, "No value (read request)");
        }

        try
        {
            var objectType = falconObject.GetType();
            _logger.LogDebug(
                "🔍 Extracting from Falcon object: {TypeName} ({Namespace})",
                objectType.Name,
                objectType.Namespace
            );

            // Case 1: Handle byte arrays (common case from Falcon SDK)
            if (falconObject is byte[] byteArray)
            {
                _logger.LogDebug("🎯 Falcon SDK provided byte array: {Data}", Convert.ToHexString(byteArray));
                return HandleByteArray(byteArray);
            }

            // Case 2: Primitive .NET types (Falcon SDK already converted!)
            if (IsPrimitiveType(objectType))
            {
                var dptId = GuessDptFromPrimitive(falconObject, objectType);
                _logger.LogDebug(
                    "✅ Falcon SDK provided primitive: {Value} ({Type}) -> DPT {DptId}",
                    falconObject,
                    objectType.Name,
                    dptId
                );
                return new FalconExtractedInfo(falconObject, dptId, $"Falcon primitive: {objectType.Name}");
            }

            // Case 3: Falcon SDK GroupValue or similar objects
            if (IsFalconSdkObject(objectType))
            {
                return ExtractFromFalconSdkObject(falconObject, objectType);
            }

            // Case 4: Unknown object - use reflection
            return ExtractUsingReflection(falconObject, objectType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting from Falcon object {TypeName}", falconObject.GetType().Name);
            return new FalconExtractedInfo(falconObject, null, $"Extraction error: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles byte arrays from Falcon SDK by decoding them based on length and content.
    /// </summary>
    /// <param name="byteArray">Byte array from Falcon SDK.</param>
    /// <returns>Extracted information with decoded value.</returns>
    private FalconExtractedInfo HandleByteArray(byte[] byteArray)
    {
        if (byteArray.Length == 0)
        {
            return new FalconExtractedInfo("Empty", null, "Empty byte array");
        }

        try
        {
            // Decode based on length and content patterns
            var (decodedValue, dptId) = DecodeByteArray(byteArray);

            _logger.LogDebug(
                "📦 Decoded byte array {Data} -> {Value} (DPT {DptId})",
                Convert.ToHexString(byteArray),
                decodedValue,
                dptId
            );

            return new FalconExtractedInfo(decodedValue, dptId, $"Decoded from {byteArray.Length}-byte array");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error decoding byte array {Data}", Convert.ToHexString(byteArray));
            return new FalconExtractedInfo(Convert.ToHexString(byteArray), null, "Raw hex (decode failed)");
        }
    }

    /// <summary>
    /// Decodes a byte array based on KNX DPT patterns.
    /// </summary>
    /// <param name="data">Byte array to decode.</param>
    /// <returns>Decoded value and detected DPT.</returns>
    private (object DecodedValue, string DptId) DecodeByteArray(byte[] data)
    {
        return data.Length switch
        {
            1 => DecodeOneByte(data[0]),
            2 => DecodeTwoBytes(data),
            4 => DecodeFourBytes(data),
            _ => (Convert.ToHexString(data), "16.001"), // String fallback
        };
    }

    /// <summary>
    /// Decodes a single byte value.
    /// </summary>
    /// <param name="value">Byte value.</param>
    /// <returns>Decoded value and DPT.</returns>
    private (object DecodedValue, string DptId) DecodeOneByte(byte value)
    {
        return value switch
        {
            0 => (false, "1.001"), // Boolean Off
            1 => (true, "1.001"), // Boolean On
            <= 100 => (value, "5.001"), // Scaling percentage
            _ => (value, "5.004"), // General 8-bit unsigned
        };
    }

    /// <summary>
    /// Decodes a two-byte value.
    /// </summary>
    /// <param name="data">Two-byte data.</param>
    /// <returns>Decoded value and DPT.</returns>
    private (object DecodedValue, string DptId) DecodeTwoBytes(byte[] data)
    {
        // Try KNX 2-byte float (DPT 9.xxx) first
        var floatValue = DecodeKnx2ByteFloat(data);

        // Check if it's a reasonable temperature
        if (floatValue >= -50 && floatValue <= 100)
        {
            return (floatValue, "9.001"); // Temperature
        }

        // Check if it's a reasonable percentage
        if (floatValue >= 0 && floatValue <= 100)
        {
            return (floatValue, "9.007"); // Humidity
        }

        // Check if it's a reasonable illuminance value
        if (floatValue >= 0 && floatValue <= 100000)
        {
            return (floatValue, "9.004"); // Illuminance
        }

        // If float doesn't make sense, try as unsigned integer
        var intValue = (ushort)((data[0] << 8) | data[1]);
        return (intValue, "7.001"); // 2-byte unsigned
    }

    /// <summary>
    /// Decodes a four-byte value.
    /// </summary>
    /// <param name="data">Four-byte data.</param>
    /// <returns>Decoded value and DPT.</returns>
    private (object DecodedValue, string DptId) DecodeFourBytes(byte[] data)
    {
        // Try as IEEE 754 float first
        var floatBytes = new byte[4];
        Array.Copy(data, floatBytes, 4);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(floatBytes);

        var floatValue = BitConverter.ToSingle(floatBytes, 0);

        if (!float.IsNaN(floatValue) && !float.IsInfinity(floatValue) && Math.Abs(floatValue) < 1000000)
        {
            // Reasonable float value - determine specific DPT
            if (floatValue >= -50 && floatValue <= 100)
            {
                return (floatValue, "14.068"); // Temperature
            }
            if (floatValue >= 0 && floatValue <= 1000)
            {
                return (floatValue, "14.076"); // Voltage
            }
            return (floatValue, "14.000"); // General 4-byte float
        }

        // Try as signed integer
        var intValue = (data[0] << 24) | (data[1] << 16) | (data[2] << 8) | data[3];
        return (intValue, "13.001"); // 4-byte signed
    }

    /// <summary>
    /// Decodes KNX 2-byte float format.
    /// </summary>
    /// <param name="data">2-byte data.</param>
    /// <returns>Decoded float value.</returns>
    private float DecodeKnx2ByteFloat(byte[] data)
    {
        if (data.Length != 2)
            return 0;

        var value = (data[0] << 8) | data[1];
        var mantissa = value & 0x07FF; // 11 bits
        var exponent = (value >> 11) & 0x0F; // 4 bits
        var sign = (value >> 15) & 0x01; // 1 bit

        // Handle sign for mantissa
        if (sign == 1)
        {
            mantissa = mantissa - 2048; // Two's complement for 11-bit value
        }

        // Calculate final value: mantissa * 2^exponent * 0.01
        return (float)(mantissa * Math.Pow(2, exponent) * 0.01);
    }

    /// <summary>
    /// Formats a value that was already decoded by Falcon SDK.
    /// </summary>
    /// <param name="falconDecodedValue">Value already decoded by Falcon SDK.</param>
    /// <param name="dptId">DPT identifier (if known).</param>
    /// <returns>Formatted display string.</returns>
    public string FormatFalconDecodedValue(object? falconDecodedValue, string? dptId)
    {
        if (falconDecodedValue == null)
        {
            return "null";
        }

        // Format based on DPT context and value type
        return (dptId, falconDecodedValue) switch
        {
            // Boolean DPTs with context-specific formatting
            ("1.001", bool b) => b ? "On" : "Off",
            ("1.002", bool b) => b ? "True" : "False",
            ("1.003", bool b) => b ? "Enable" : "Disable",
            ("1.008", bool b) => b ? "Up" : "Down",
            ("1.009", bool b) => b ? "Open" : "Close",
            (var dpt, bool b) when dpt?.StartsWith("1.") == true => b ? "true" : "false",

            // Temperature DPTs
            ("9.001", float f) => $"{f:F1}°C",
            ("9.001", double d) => $"{d:F1}°C",
            ("14.068", float f) => $"{f:F1}°C",
            ("14.068", double d) => $"{d:F1}°C",

            // Percentage DPTs
            ("5.001", byte b) => $"{b}%",
            ("9.007", float f) => $"{f:F1}%",
            ("9.007", double d) => $"{d:F1}%",

            // Illuminance
            ("9.004", float f) => $"{f:F0} lux",
            ("9.004", double d) => $"{d:F0} lux",

            // Electrical values
            ("14.019", float f) => $"{f:F2} A",
            ("14.027", float f) => $"{f:F1} Hz",
            ("14.056", float f) => $"{f:F1} W",
            ("14.076", float f) => $"{f:F1} V",

            // Pressure
            ("9.006", float f) => $"{f:F0} Pa",
            ("9.006", double d) => $"{d:F0} Pa",

            // Wind speed
            ("9.005", float f) => $"{f:F1} m/s",
            ("9.005", double d) => $"{d:F1} m/s",

            // Angle
            ("5.003", byte b) => $"{b}°",

            // Default formatting based on type (when DPT is unknown)
            (_, bool b) => b ? "true" : "false",
            (_, byte b) => $"{b}",
            (_, sbyte sb) => $"{sb}",
            (_, short s) => $"{s}",
            (_, ushort us) => $"{us}",
            (_, int i) => $"{i}",
            (_, uint ui) => $"{ui}",
            (_, float f) when f >= -50 && f <= 100 => $"{f:F1}°C", // Likely temperature
            (_, float f) when f >= 0 && f <= 100000 => $"{f:F0} lux", // Likely illuminance
            (_, float f) => $"{f:F2}",
            (_, double d) => $"{d:F2}",
            (_, string str) => str,
            _ => falconDecodedValue.ToString() ?? "unknown",
        };
    }

    #region IDptDecodingService Implementation (for compatibility)

    /// <inheritdoc/>
    public object? DecodeValue(byte[] data, string dptId)
    {
        // This method is not the primary use case for this service
        // The main purpose is to extract from live Falcon SDK objects
        _logger.LogWarning(
            "DecodeValue called on CorrectFalconFirstService - this service is designed for live event extraction"
        );
        return null;
    }

    /// <inheritdoc/>
    public (object? Value, string? DetectedDpt) DecodeValueWithAutoDetection(byte[] data)
    {
        _logger.LogWarning(
            "DecodeValueWithAutoDetection called on CorrectFalconFirstService - this service is designed for live event extraction"
        );
        return (null, null);
    }

    /// <inheritdoc/>
    public string FormatValue(object? value, string? dptId)
    {
        return FormatFalconDecodedValue(value, dptId);
    }

    /// <inheritdoc/>
    public string? DetectDpt(byte[] data)
    {
        _logger.LogWarning(
            "DetectDpt called on CorrectFalconFirstService - this service is designed for live event extraction"
        );
        return null;
    }

    /// <inheritdoc/>
    public bool IsDptSupported(string dptId)
    {
        // We support whatever Falcon SDK supports (which is most DPTs)
        return !string.IsNullOrEmpty(dptId);
    }

    /// <inheritdoc/>
    public IEnumerable<string> GetSupportedDpts()
    {
        // Return comprehensive list of DPTs that Falcon SDK typically supports
        return new[]
        {
            "1.001",
            "1.002",
            "1.003",
            "1.008",
            "1.009",
            "5.001",
            "5.003",
            "5.004",
            "6.001",
            "6.010",
            "7.001",
            "7.002",
            "7.003",
            "8.001",
            "8.002",
            "9.001",
            "9.002",
            "9.003",
            "9.004",
            "9.005",
            "9.006",
            "9.007",
            "9.008",
            "12.001",
            "13.001",
            "14.000",
            "14.019",
            "14.027",
            "14.056",
            "14.068",
            "14.076",
            "16.000",
            "16.001",
        };
    }

    #endregion

    #region Private Helper Methods

    private static bool IsPrimitiveType(Type type)
    {
        return type.IsPrimitive || type == typeof(string) || type == typeof(decimal);
    }

    private static bool IsFalconSdkObject(Type type)
    {
        return type.Name.Contains("GroupValue")
            || type.Name.Contains("Dpt")
            || type.Namespace?.StartsWith("Knx.Falcon") == true;
    }

    private string? GuessDptFromPrimitive(object value, Type type)
    {
        return (type, value) switch
        {
            (Type t, bool) when t == typeof(bool) => "1.001",
            (Type t, byte b) when t == typeof(byte) && b <= 1 => "1.001",
            (Type t, byte b) when t == typeof(byte) && b <= 100 => "5.001",
            (Type t, byte) when t == typeof(byte) => "5.004",
            (Type t, sbyte) when t == typeof(sbyte) => "6.001",
            (Type t, short) when t == typeof(short) => "8.001",
            (Type t, ushort) when t == typeof(ushort) => "7.001",
            (Type t, int) when t == typeof(int) => "13.001",
            (Type t, uint) when t == typeof(uint) => "12.001",
            (Type t, float f) when t == typeof(float) && f >= -50 && f <= 100 => "9.001",
            (Type t, float f) when t == typeof(float) && f >= 0 && f <= 100000 => "9.004",
            (Type t, float) when t == typeof(float) => "14.000",
            (Type t, double d) when t == typeof(double) && d >= -50 && d <= 100 => "9.001",
            (Type t, double) when t == typeof(double) => "14.000",
            (Type t, string) when t == typeof(string) => "16.001",
            _ => null,
        };
    }

    private FalconExtractedInfo ExtractFromFalconSdkObject(object falconObject, Type objectType)
    {
        try
        {
            var dptId = ExtractDptFromObject(falconObject, objectType);
            var decodedValue = ExtractValueFromObject(falconObject, objectType);

            _logger.LogDebug("✅ Extracted from Falcon SDK object: DPT {DptId}, Value {Value}", dptId, decodedValue);

            return new FalconExtractedInfo(
                decodedValue ?? falconObject,
                dptId,
                $"Falcon SDK object: {objectType.Name}"
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error extracting from Falcon SDK object {TypeName}", objectType.Name);
            return new FalconExtractedInfo(falconObject, null, $"SDK object extraction error: {ex.Message}");
        }
    }

    private FalconExtractedInfo ExtractUsingReflection(object obj, Type objectType)
    {
        try
        {
            var properties = objectType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            _logger.LogDebug(
                "🔍 Reflecting on {TypeName} with {PropertyCount} properties",
                objectType.Name,
                properties.Length
            );

            string? dptId = null;
            object? decodedValue = null;
            var propertyInfo = new List<string>();

            foreach (var prop in properties.Take(10)) // Limit to avoid spam
            {
                try
                {
                    var propValue = prop.GetValue(obj);
                    propertyInfo.Add($"{prop.Name}={propValue}");

                    // Look for DPT information
                    if (
                        prop.Name.Contains("Dpt", StringComparison.OrdinalIgnoreCase)
                        || prop.Name.Contains("DataPoint", StringComparison.OrdinalIgnoreCase)
                        || prop.Name.Contains("Type", StringComparison.OrdinalIgnoreCase)
                    )
                    {
                        dptId ??= propValue?.ToString();
                    }

                    // Look for decoded value
                    if (
                        prop.Name.Equals("Value", StringComparison.OrdinalIgnoreCase)
                        || prop.Name.Equals("DecodedValue", StringComparison.OrdinalIgnoreCase)
                        || prop.Name.Equals("TypedValue", StringComparison.OrdinalIgnoreCase)
                    )
                    {
                        decodedValue ??= propValue;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error reading property {PropertyName}", prop.Name);
                    propertyInfo.Add($"{prop.Name}=ERROR");
                }
            }

            decodedValue ??= obj;
            var info = $"Reflected {objectType.Name}: {string.Join(", ", propertyInfo.Take(3))}";

            _logger.LogDebug("🔍 Reflection result: DPT {DptId}, Value {Value}", dptId, decodedValue);

            return new FalconExtractedInfo(decodedValue, dptId, info);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during reflection on {TypeName}", objectType.Name);
            return new FalconExtractedInfo(obj, null, $"Reflection error: {ex.Message}");
        }
    }

    private string? ExtractDptFromObject(object obj, Type type)
    {
        var dptProperties = new[] { "Dpt", "DptId", "DataPointType", "DataPointTypeId", "Type" };

        foreach (var propName in dptProperties)
        {
            try
            {
                var prop = type.GetProperty(propName);
                if (prop != null)
                {
                    var value = prop.GetValue(obj);
                    if (value != null)
                    {
                        return value.ToString();
                    }
                }
            }
            catch { }
        }

        // Extract from type name if possible
        var match = System.Text.RegularExpressions.Regex.Match(type.Name, @"Dpt(\d+)");
        if (match.Success)
        {
            return $"{match.Groups[1].Value}.001";
        }

        return null;
    }

    private object? ExtractValueFromObject(object obj, Type type)
    {
        var valueProperties = new[] { "Value", "DecodedValue", "TypedValue", "ConvertedValue" };

        foreach (var propName in valueProperties)
        {
            try
            {
                var prop = type.GetProperty(propName);
                if (prop != null)
                {
                    var value = prop.GetValue(obj);
                    if (value != null)
                    {
                        return value;
                    }
                }
            }
            catch { }
        }

        // Try conversion methods
        var valueMethods = new[] { "ToValue", "GetValue", "Convert", "AsValue" };
        foreach (var methodName in valueMethods)
        {
            try
            {
                var method = type.GetMethod(methodName, Type.EmptyTypes);
                if (method != null)
                {
                    var value = method.Invoke(obj, null);
                    if (value != null)
                    {
                        return value;
                    }
                }
            }
            catch { }
        }

        return null;
    }

    #endregion

    /// <summary>
    /// Information extracted from a Falcon SDK object.
    /// </summary>
    /// <param name="DecodedValue">Value already decoded by Falcon SDK.</param>
    /// <param name="DptId">DPT identifier (if detected).</param>
    /// <param name="ExtractionInfo">Information about how the data was extracted.</param>
    public record FalconExtractedInfo(object? DecodedValue, string? DptId, string ExtractionInfo);
}
