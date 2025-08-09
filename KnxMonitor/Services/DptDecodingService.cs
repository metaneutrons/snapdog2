using Microsoft.Extensions.Logging;

namespace KnxMonitor.Services;

/// <summary>
/// Service for decoding KNX Data Point Types (DPT) using Falcon SDK.
/// </summary>
public partial class DptDecodingService : IDptDecodingService
{
    private readonly ILogger<DptDecodingService> _logger;

    /// <summary>
    /// Supported DPT mappings with their characteristics.
    /// </summary>
    private static readonly Dictionary<string, DptInfo> SupportedDpts = new()
    {
        // DPT 1 - Boolean (1-bit)
        ["1.001"] = new("1.001", "Switch", 1, typeof(bool), "On/Off"),
        ["1.002"] = new("1.002", "Boolean", 1, typeof(bool), "True/False"),
        ["1.003"] = new("1.003", "Enable", 1, typeof(bool), "Enable/Disable"),
        ["1.008"] = new("1.008", "Up/Down", 1, typeof(bool), "Up/Down"),
        ["1.009"] = new("1.009", "Open/Close", 1, typeof(bool), "Open/Close"),

        // DPT 5 - 8-bit Unsigned Value
        ["5.001"] = new("5.001", "Scaling", 1, typeof(byte), "0-100%"),
        ["5.003"] = new("5.003", "Angle", 1, typeof(byte), "0-360°"),
        ["5.004"] = new("5.004", "Percent_U8", 1, typeof(byte), "0-255"),

        // DPT 6 - 8-bit Signed Value
        ["6.001"] = new("6.001", "Percent_V8", 1, typeof(sbyte), "-128 to 127"),
        ["6.010"] = new("6.010", "Counter", 1, typeof(sbyte), "Counter pulses"),

        // DPT 7 - 2-byte Unsigned Value
        ["7.001"] = new("7.001", "Value_2_Ucount", 2, typeof(ushort), "0-65535"),
        ["7.002"] = new("7.002", "TimePeriodMsec", 2, typeof(ushort), "Time in ms"),
        ["7.003"] = new("7.003", "TimePeriod10Msec", 2, typeof(ushort), "Time in 10ms"),

        // DPT 8 - 2-byte Signed Value
        ["8.001"] = new("8.001", "Value_2_Count", 2, typeof(short), "-32768 to 32767"),
        ["8.002"] = new("8.002", "DeltaTimeMsec", 2, typeof(short), "Delta time in ms"),

        // DPT 9 - 2-byte Float Value
        ["9.001"] = new("9.001", "Temp", 2, typeof(float), "Temperature °C"),
        ["9.002"] = new("9.002", "Temp_Diff", 2, typeof(float), "Temperature difference K"),
        ["9.003"] = new("9.003", "Temp_Gradient", 2, typeof(float), "Temperature gradient K/h"),
        ["9.004"] = new("9.004", "Lux", 2, typeof(float), "Illuminance lux"),
        ["9.005"] = new("9.005", "Wsp", 2, typeof(float), "Wind speed m/s"),
        ["9.006"] = new("9.006", "Pressure", 2, typeof(float), "Pressure Pa"),
        ["9.007"] = new("9.007", "Humidity", 2, typeof(float), "Humidity %"),
        ["9.008"] = new("9.008", "AirQuality", 2, typeof(float), "Air quality ppm"),

        // DPT 12 - 4-byte Unsigned Value
        ["12.001"] = new("12.001", "Value_4_Ucount", 4, typeof(uint), "0 to 4294967295"),

        // DPT 13 - 4-byte Signed Value
        ["13.001"] = new("13.001", "Value_4_Count", 4, typeof(int), "-2147483648 to 2147483647"),

        // DPT 14 - 4-byte Float Value
        ["14.000"] = new("14.000", "Value_Acceleration", 4, typeof(float), "Acceleration m/s²"),
        ["14.001"] = new("14.001", "Value_Angular_Acceleration", 4, typeof(float), "Angular acceleration rad/s²"),
        ["14.019"] = new("14.019", "Value_Electric_Current", 4, typeof(float), "Electric current A"),
        ["14.027"] = new("14.027", "Value_Frequency", 4, typeof(float), "Frequency Hz"),
        ["14.056"] = new("14.056", "Value_Power", 4, typeof(float), "Power W"),
        ["14.057"] = new("14.057", "Value_Power_Factor", 4, typeof(float), "Power factor"),
        ["14.065"] = new("14.065", "Value_Speed", 4, typeof(float), "Speed m/s"),
        ["14.068"] = new("14.068", "Value_Temp", 4, typeof(float), "Temperature °C"),
        ["14.076"] = new("14.076", "Value_Voltage", 4, typeof(float), "Voltage V"),
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="DptDecodingService"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public DptDecodingService(ILogger<DptDecodingService> logger)
    {
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public object? DecodeValue(byte[] data, string dptId)
    {
        if (data == null || data.Length == 0)
        {
            return null;
        }

        if (string.IsNullOrEmpty(dptId))
        {
            return null;
        }

        try
        {
            // Normalize DPT ID (handle both "1.001" and "1001" formats)
            var normalizedDptId = NormalizeDptId(dptId);

            if (!SupportedDpts.TryGetValue(normalizedDptId, out var dptInfo))
            {
                this.LogUnsupportedDpt(dptId);
                return null;
            }

            // Validate data length
            if (data.Length != dptInfo.ExpectedLength)
            {
                this.LogInvalidDataLengthForDpt(dptId, dptInfo.ExpectedLength, data.Length);
                return null;
            }

            return normalizedDptId switch
            {
                // DPT 1.xxx - Boolean values
                var dpt when dpt.StartsWith("1.") => DecodeDpt1(data),

                // DPT 5.xxx - 8-bit unsigned values
                var dpt when dpt.StartsWith("5.") => DecodeDpt5(data, normalizedDptId),

                // DPT 6.xxx - 8-bit signed values
                var dpt when dpt.StartsWith("6.") => DecodeDpt6(data),

                // DPT 7.xxx - 2-byte unsigned values
                var dpt when dpt.StartsWith("7.") => DecodeDpt7(data),

                // DPT 8.xxx - 2-byte signed values
                var dpt when dpt.StartsWith("8.") => DecodeDpt8(data),

                // DPT 9.xxx - 2-byte float values
                var dpt when dpt.StartsWith("9.") => DecodeDpt9(data),

                // DPT 12.xxx - 4-byte unsigned values
                var dpt when dpt.StartsWith("12.") => DecodeDpt12(data),

                // DPT 13.xxx - 4-byte signed values
                var dpt when dpt.StartsWith("13.") => DecodeDpt13(data),

                // DPT 14.xxx - 4-byte float values
                var dpt when dpt.StartsWith("14.") => DecodeDpt14(data),

                _ => null,
            };
        }
        catch (Exception ex)
        {
            this.LogErrorDecodingDpt(ex, dptId, Convert.ToHexString(data));
            return null;
        }
    }

    /// <inheritdoc/>
    public (object? Value, string? DetectedDpt) DecodeValueWithAutoDetection(byte[] data)
    {
        if (data == null || data.Length == 0)
        {
            return (null, null);
        }

        var detectedDpt = this.DetectDpt(data);
        if (detectedDpt == null)
        {
            return (null, null);
        }

        var value = this.DecodeValue(data, detectedDpt);
        return (value, detectedDpt);
    }

    /// <inheritdoc/>
    public string FormatValue(object? value, string? dptId)
    {
        if (value == null)
        {
            return "null";
        }

        var normalizedDptId = !string.IsNullOrEmpty(dptId) ? NormalizeDptId(dptId) : null;

        return normalizedDptId switch
        {
            // Boolean values
            var dpt when dpt?.StartsWith("1.") == true => FormatBooleanValue(value, dpt),

            // Temperature values
            "9.001" or "14.068" => $"{value:F1}°C",
            "9.002" => $"{value:F1}K",

            // Percentage values
            "5.001" => $"{value}%",
            "9.007" => $"{value:F1}%",

            // Illuminance
            "9.004" => $"{value:F0} lux",

            // Pressure
            "9.006" => $"{value:F0} Pa",

            // Wind speed
            "9.005" => $"{value:F1} m/s",

            // Air quality
            "9.008" => $"{value:F0} ppm",

            // Electrical values
            "14.019" => $"{value:F2} A",
            "14.027" => $"{value:F1} Hz",
            "14.056" => $"{value:F1} W",
            "14.076" => $"{value:F1} V",

            // Angle
            "5.003" => $"{value}°",

            // Default formatting
            _ => FormatDefaultValue(value),
        };
    }

    /// <inheritdoc/>
    public string? DetectDpt(byte[] data)
    {
        if (data == null || data.Length == 0)
        {
            return null;
        }

        return data.Length switch
        {
            1 => DetectDpt1Byte(data[0]),
            2 => DetectDpt2Byte(data),
            4 => DetectDpt4Byte(data),
            _ => null,
        };
    }

    /// <inheritdoc/>
    public bool IsDptSupported(string dptId)
    {
        if (string.IsNullOrEmpty(dptId))
        {
            return false;
        }

        var normalizedDptId = NormalizeDptId(dptId);
        return SupportedDpts.ContainsKey(normalizedDptId);
    }

    /// <inheritdoc/>
    public IEnumerable<string> GetSupportedDpts()
    {
        return SupportedDpts.Keys.OrderBy(x => x);
    }

    #region Private Methods

    private static string NormalizeDptId(string dptId)
    {
        if (string.IsNullOrEmpty(dptId))
        {
            return string.Empty;
        }

        // Handle formats like "1001" -> "1.001"
        if (dptId.Length == 4 && char.IsDigit(dptId[0]) && !dptId.Contains('.'))
        {
            return $"{dptId[0]}.{dptId.Substring(1)}";
        }

        return dptId;
    }

    private static bool DecodeDpt1(byte[] data)
    {
        // DPT 1.xxx - Boolean (1-bit value in 1 byte)
        // The actual bit is in the least significant bit
        return (data[0] & 0x01) != 0;
    }

    private static byte DecodeDpt5(byte[] data, string dptId)
    {
        // DPT 5.xxx - 8-bit unsigned value
        var rawValue = data[0];

        return dptId switch
        {
            "5.001" => (byte)Math.Round(rawValue * 100.0 / 255.0), // Scale to 0-100%
            _ => rawValue,
        };
    }

    private static sbyte DecodeDpt6(byte[] data)
    {
        // DPT 6.xxx - 8-bit signed value
        return (sbyte)data[0];
    }

    private static ushort DecodeDpt7(byte[] data)
    {
        // DPT 7.xxx - 2-byte unsigned value (big-endian)
        return (ushort)((data[0] << 8) | data[1]);
    }

    private static short DecodeDpt8(byte[] data)
    {
        // DPT 8.xxx - 2-byte signed value (big-endian)
        return (short)((data[0] << 8) | data[1]);
    }

    private static float DecodeDpt9(byte[] data)
    {
        // DPT 9.xxx - 2-byte float value (KNX 16-bit float format)
        // Format: SEEEEMMMMMMMMMMM (S=sign, E=exponent, M=mantissa)
        var value = (data[0] << 8) | data[1];

        // Extract components
        var mantissa = value & 0x07FF; // 11 bits (bits 0-10)
        var exponent = (value >> 11) & 0x0F; // 4 bits (bits 11-14)
        var sign = (value >> 15) & 0x01; // 1 bit (bit 15)

        // Convert mantissa to signed value if sign bit is set
        if (sign == 1)
        {
            // For negative values, the mantissa is in two's complement format
            // but only for the 11-bit mantissa part
            mantissa = mantissa - 2048; // 2^11 = 2048
        }

        // Calculate final value: mantissa * 2^exponent * 0.01
        return (float)(mantissa * Math.Pow(2, exponent) * 0.01);
    }

    private static uint DecodeDpt12(byte[] data)
    {
        // DPT 12.xxx - 4-byte unsigned value (big-endian)
        return (uint)((data[0] << 24) | (data[1] << 16) | (data[2] << 8) | data[3]);
    }

    private static int DecodeDpt13(byte[] data)
    {
        // DPT 13.xxx - 4-byte signed value (big-endian)
        return (data[0] << 24) | (data[1] << 16) | (data[2] << 8) | data[3];
    }

    private static float DecodeDpt14(byte[] data)
    {
        // DPT 14.xxx - 4-byte IEEE 754 float (big-endian)
        var bytes = new byte[4];
        Array.Copy(data, bytes, 4);

        // Convert from big-endian to little-endian if needed
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }

        return BitConverter.ToSingle(bytes, 0);
    }

    private static string? DetectDpt1Byte(byte value)
    {
        // For 1-byte values, try to detect the most likely DPT
        return value switch
        {
            0x00 or 0x01 => "1.001", // Boolean switch
            <= 100 => "5.001", // Scaling percentage
            _ => "5.004", // General 8-bit unsigned
        };
    }

    private static string? DetectDpt2Byte(byte[] data)
    {
        // Try to decode as DPT 9 float first
        var floatValue = DecodeDpt9(data);

        // Check if it's a reasonable temperature
        if (floatValue >= -50 && floatValue <= 100)
        {
            return "9.001"; // Temperature
        }

        // Check if it's a reasonable percentage
        if (floatValue >= 0 && floatValue <= 100)
        {
            return "9.007"; // Humidity percentage
        }

        // Check if it's a reasonable illuminance value
        if (floatValue >= 0 && floatValue <= 100000)
        {
            return "9.004"; // Illuminance
        }

        // If float doesn't make sense, try as unsigned integer
        var intValue = DecodeDpt7(data);
        if (intValue <= 65535)
        {
            return "7.001"; // 2-byte unsigned
        }

        return "9.001"; // Default to temperature
    }

    private static string? DetectDpt4Byte(byte[] data)
    {
        // Try as IEEE 754 float first
        var floatValue = DecodeDpt14(data);

        if (!float.IsNaN(floatValue) && !float.IsInfinity(floatValue) && Math.Abs(floatValue) < 1000000)
        {
            // Reasonable float value
            if (floatValue >= -50 && floatValue <= 100)
            {
                return "14.068"; // Temperature
            }
            return "14.000"; // General 4-byte float
        }

        // Try as signed integer
        var intValue = DecodeDpt13(data);
        if (Math.Abs(intValue) < 1000000)
        {
            return "13.001"; // 4-byte signed
        }

        return "12.001"; // Default to 4-byte unsigned
    }

    private static string FormatBooleanValue(object value, string dptId)
    {
        if (value is not bool boolValue)
        {
            return value.ToString() ?? "unknown";
        }

        return dptId switch
        {
            "1.001" => boolValue ? "On" : "Off",
            "1.002" => boolValue ? "True" : "False",
            "1.003" => boolValue ? "Enable" : "Disable",
            "1.008" => boolValue ? "Up" : "Down",
            "1.009" => boolValue ? "Open" : "Close",
            _ => boolValue ? "true" : "false",
        };
    }

    private static string FormatDefaultValue(object value)
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
            _ => value.ToString() ?? "unknown",
        };
    }

    #endregion

    /// <summary>
    /// Information about a supported DPT.
    /// </summary>
    private record DptInfo(string Id, string Name, int ExpectedLength, Type ValueType, string Description);
}
