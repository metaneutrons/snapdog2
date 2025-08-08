using KnxMonitor.Services;

namespace KnxMonitor.Models;

/// <summary>
/// Represents a KNX bus message for display purposes.
/// </summary>
public class KnxMessage
{
    private static IDptDecodingService? _dptDecodingService;

    /// <summary>
    /// Sets the DPT decoding service for all KnxMessage instances.
    /// This should be called during application startup.
    /// </summary>
    /// <param name="dptDecodingService">The DPT decoding service.</param>
    public static void SetDptDecodingService(IDptDecodingService dptDecodingService)
    {
        _dptDecodingService = dptDecodingService;
    }

    /// <summary>
    /// Gets or sets the timestamp when the message was received.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the source address of the message.
    /// </summary>
    public string SourceAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the destination group address.
    /// </summary>
    public string GroupAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the message type (Read, Write, Response).
    /// </summary>
    public KnxMessageType MessageType { get; set; }

    /// <summary>
    /// Gets or sets the raw data payload.
    /// </summary>
    public byte[] Data { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Gets or sets the interpreted value (if available).
    /// This should be set from the Falcon SDK's decoded value.
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// Gets or sets the data point type (if known).
    /// </summary>
    public string? DataPointType { get; set; }

    /// <summary>
    /// Gets or sets the message priority.
    /// </summary>
    public KnxPriority Priority { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this message was repeated.
    /// </summary>
    public bool IsRepeated { get; set; }

    /// <summary>
    /// Gets the data as a hexadecimal string.
    /// </summary>
    public string DataHex => Convert.ToHexString(Data);

    /// <summary>
    /// Gets a formatted display value with proper DPT decoding.
    /// </summary>
    public string DisplayValue
    {
        get
        {
            // If we have a DPT decoding service, use it
            if (_dptDecodingService != null)
            {
                // If we have a known DPT, use it for decoding
                if (!string.IsNullOrEmpty(DataPointType))
                {
                    var decodedValue = _dptDecodingService.DecodeValue(Data, DataPointType);
                    if (decodedValue != null)
                    {
                        return _dptDecodingService.FormatValue(decodedValue, DataPointType);
                    }
                }

                // Try auto-detection if no DPT is known
                var (autoDecodedValue, detectedDpt) = _dptDecodingService.DecodeValueWithAutoDetection(Data);
                if (autoDecodedValue != null)
                {
                    return _dptDecodingService.FormatValue(autoDecodedValue, detectedDpt);
                }
            }

            // Fallback: if we have a pre-decoded value from Falcon SDK, format it
            if (Value != null)
            {
                return FormatFalconValue(Value, DataPointType);
            }

            // Final fallback: show raw data
            if (Data.Length == 0)
            {
                return "Empty";
            }

            return $"Raw: {DataHex}";
        }
    }

    /// <summary>
    /// Gets the likely datapoint type based on data analysis.
    /// </summary>
    public string GuessedDPT
    {
        get
        {
            if (!string.IsNullOrEmpty(DataPointType))
                return DataPointType;

            // Use DPT decoding service for detection if available
            if (_dptDecodingService != null)
            {
                var detectedDpt = _dptDecodingService.DetectDpt(Data);
                if (!string.IsNullOrEmpty(detectedDpt))
                {
                    return $"DPT {detectedDpt}";
                }
            }

            // Fallback to simple length-based guessing
            return Data.Length switch
            {
                0 => "DPT 1 (1-bit)",
                1 => "DPT 1/5/6 (1-byte)",
                2 => "DPT 7/8/9 (2-byte)",
                4 => "DPT 12/13/14 (4-byte)",
                _ => "Unknown",
            };
        }
    }

    /// <summary>
    /// Gets the decoded value using the DPT decoding service.
    /// </summary>
    public object? DecodedValue
    {
        get
        {
            if (_dptDecodingService == null)
            {
                return Value; // Return Falcon SDK value if no decoding service
            }

            // Try with known DPT first
            if (!string.IsNullOrEmpty(DataPointType))
            {
                var decoded = _dptDecodingService.DecodeValue(Data, DataPointType);
                if (decoded != null)
                {
                    return decoded;
                }
            }

            // Try auto-detection
            var (autoDecoded, _) = _dptDecodingService.DecodeValueWithAutoDetection(Data);
            return autoDecoded ?? Value;
        }
    }

    private static string FormatFalconValue(object value, string? dataPointType)
    {
        return value switch
        {
            bool b => b ? "true" : "false",
            byte by => $"{by}",
            sbyte sb => $"{sb}",
            short s => $"{s}",
            ushort us => $"{us}",
            int i => $"{i}",
            uint ui => $"{ui}",
            float f => $"{f:F2}",
            double d => $"{d:F2}",
            string str => str,
            byte[] bytes => Convert.ToHexString(bytes),
            _ => value.ToString() ?? "Unknown",
        };
    }
}

/// <summary>
/// KNX message types.
/// </summary>
public enum KnxMessageType
{
    /// <summary>
    /// Read request.
    /// </summary>
    Read,

    /// <summary>
    /// Write request.
    /// </summary>
    Write,

    /// <summary>
    /// Response to read request.
    /// </summary>
    Response,
}

/// <summary>
/// KNX message priorities.
/// </summary>
public enum KnxPriority
{
    /// <summary>
    /// System priority.
    /// </summary>
    System,

    /// <summary>
    /// Normal priority.
    /// </summary>
    Normal,

    /// <summary>
    /// Urgent priority.
    /// </summary>
    Urgent,

    /// <summary>
    /// Low priority.
    /// </summary>
    Low,
}
