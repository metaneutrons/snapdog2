using System.Text;

namespace SnapDog2.Infrastructure.Services.Models;

/// <summary>
/// Utility class for converting values to and from KNX Data Point Types (DPT).
/// Provides conversion methods for common DPT types used in building automation.
/// </summary>
public static class KnxDptConverter
{
    /// <summary>
    /// Converts a boolean value to DPT 1.001 (Switch) format.
    /// </summary>
    /// <param name="value">The boolean value to convert</param>
    /// <returns>KNX byte array for DPT 1.001</returns>
    public static byte[] BooleanToDpt1001(bool value)
    {
        return new byte[] { (byte)(value ? 1 : 0) };
    }

    /// <summary>
    /// Converts DPT 1.001 (Switch) byte array to boolean.
    /// </summary>
    /// <param name="data">The KNX byte array</param>
    /// <returns>The boolean value</returns>
    public static bool Dpt1001ToBoolean(byte[] data)
    {
        if (data == null || data.Length == 0)
            return false;
        
        return (data[0] & 0x01) != 0;
    }

    /// <summary>
    /// Converts a percentage value (0-100) to DPT 5.001 (Scaling) format.
    /// </summary>
    /// <param name="percent">The percentage value (0-100)</param>
    /// <returns>KNX byte array for DPT 5.001</returns>
    public static byte[] PercentToDpt5001(int percent)
    {
        if (percent < 0) percent = 0;
        if (percent > 100) percent = 100;
        
        var value = (byte)Math.Round(percent * 2.55); // Convert 0-100 to 0-255
        return new byte[] { value };
    }

    /// <summary>
    /// Converts DPT 5.001 (Scaling) byte array to percentage.
    /// </summary>
    /// <param name="data">The KNX byte array</param>
    /// <returns>The percentage value (0-100)</returns>
    public static int Dpt5001ToPercent(byte[] data)
    {
        if (data == null || data.Length == 0)
            return 0;
        
        return (int)Math.Round(data[0] / 2.55); // Convert 0-255 to 0-100
    }

    /// <summary>
    /// Converts a 16-bit integer to DPT 7.001 (2-byte unsigned) format.
    /// </summary>
    /// <param name="value">The integer value (0-65535)</param>
    /// <returns>KNX byte array for DPT 7.001</returns>
    public static byte[] UInt16ToDpt7001(ushort value)
    {
        return new byte[] 
        { 
            (byte)(value >> 8),   // High byte
            (byte)(value & 0xFF)  // Low byte
        };
    }

    /// <summary>
    /// Converts DPT 7.001 (2-byte unsigned) byte array to 16-bit integer.
    /// </summary>
    /// <param name="data">The KNX byte array</param>
    /// <returns>The integer value</returns>
    public static ushort Dpt7001ToUInt16(byte[] data)
    {
        if (data == null || data.Length < 2)
            return 0;
        
        return (ushort)((data[0] << 8) | data[1]);
    }

    /// <summary>
    /// Converts a floating-point value to DPT 9.001 (2-byte float) format.
    /// </summary>
    /// <param name="value">The float value</param>
    /// <returns>KNX byte array for DPT 9.001</returns>
    public static byte[] FloatToDpt9001(float value)
    {
        // DPT 9.001 format: MEEEEMMM MMMMMMMM
        // M = mantissa (11 bits, signed), E = exponent (4 bits, signed)
        
        if (value == 0.0f)
            return new byte[] { 0, 0 };
        
        var sign = value < 0 ? 1 : 0;
        var absValue = Math.Abs(value);
        
        // Find appropriate exponent
        var exponent = 0;
        var mantissa = absValue;
        
        while (mantissa > 2047.0f && exponent < 15)
        {
            mantissa /= 2.0f;
            exponent++;
        }
        
        while (mantissa < 1024.0f && exponent > -15)
        {
            mantissa *= 2.0f;
            exponent--;
        }
        
        var intMantissa = (int)Math.Round(mantissa);
        if (sign == 1)
            intMantissa = -intMantissa;
        
        // Ensure mantissa fits in 11 bits
        intMantissa &= 0x7FF;
        if (sign == 1)
            intMantissa |= 0x800; // Set sign bit
        
        // Ensure exponent fits in 4 bits
        exponent = Math.Max(-8, Math.Min(7, exponent));
        var expBits = exponent & 0x0F;
        
        var result = (ushort)((expBits << 11) | intMantissa);
        return new byte[] { (byte)(result >> 8), (byte)(result & 0xFF) };
    }

    /// <summary>
    /// Converts DPT 9.001 (2-byte float) byte array to floating-point value.
    /// </summary>
    /// <param name="data">The KNX byte array</param>
    /// <returns>The float value</returns>
    public static float Dpt9001ToFloat(byte[] data)
    {
        if (data == null || data.Length < 2)
            return 0.0f;
        
        var value = (ushort)((data[0] << 8) | data[1]);
        
        var exponent = (value >> 11) & 0x0F;
        var mantissa = value & 0x7FF;
        
        // Handle sign extension for exponent (4-bit signed)
        if (exponent > 7)
            exponent -= 16;
        
        // Handle sign extension for mantissa (11-bit signed)
        if ((mantissa & 0x400) != 0)
            mantissa -= 2048;
        
        return (float)(mantissa * Math.Pow(2, exponent));
    }

    /// <summary>
    /// Converts a string to DPT 16.001 (ASCII string) format.
    /// </summary>
    /// <param name="text">The string to convert (max 14 characters)</param>
    /// <returns>KNX byte array for DPT 16.001</returns>
    public static byte[] StringToDpt16001(string text)
    {
        if (string.IsNullOrEmpty(text))
            return new byte[14]; // All zeros
        
        var bytes = Encoding.ASCII.GetBytes(text);
        var result = new byte[14];
        
        // Copy up to 14 bytes, pad with zeros
        Array.Copy(bytes, result, Math.Min(bytes.Length, 14));
        
        return result;
    }

    /// <summary>
    /// Converts DPT 16.001 (ASCII string) byte array to string.
    /// </summary>
    /// <param name="data">The KNX byte array</param>
    /// <returns>The string value</returns>
    public static string Dpt16001ToString(byte[] data)
    {
        if (data == null || data.Length == 0)
            return string.Empty;
        
        // Find the null terminator or use full length
        var length = Array.IndexOf(data, (byte)0);
        if (length == -1)
            length = Math.Min(data.Length, 14);
        
        return Encoding.ASCII.GetString(data, 0, length);
    }

    /// <summary>
    /// Converts a date and time to DPT 19.001 (Date Time) format.
    /// </summary>
    /// <param name="dateTime">The DateTime to convert</param>
    /// <returns>KNX byte array for DPT 19.001</returns>
    public static byte[] DateTimeToDpt19001(DateTime dateTime)
    {
        var result = new byte[8];
        
        // Year (8 bits, year - 1900)
        result[0] = (byte)(dateTime.Year - 1900);
        
        // Month (4 bits) and Day (5 bits, high 3 bits)
        result[1] = (byte)((dateTime.Month << 4) | ((dateTime.Day >> 1) & 0x0F));
        
        // Day (low 1 bit) and Day of week (3 bits) and Hour (4 bits, high bit)
        var dayOfWeek = (int)dateTime.DayOfWeek; // 0 = Sunday
        result[2] = (byte)(((dateTime.Day & 0x01) << 7) | (dayOfWeek << 4) | ((dateTime.Hour >> 4) & 0x01));
        
        // Hour (low 4 bits) and Minute (4 bits, high 4 bits)
        result[3] = (byte)(((dateTime.Hour & 0x0F) << 4) | ((dateTime.Minute >> 2) & 0x0F));
        
        // Minute (low 2 bits) and Second (6 bits)
        result[4] = (byte)(((dateTime.Minute & 0x03) << 6) | (dateTime.Second & 0x3F));
        
        // Centiseconds, fault bits, working day, etc. (set to default values)
        result[5] = 0; // Centiseconds
        result[6] = 0; // Quality bits
        result[7] = 0; // CLQ (Clock Quality)
        
        return result;
    }

    /// <summary>
    /// Converts DPT 19.001 (Date Time) byte array to DateTime.
    /// </summary>
    /// <param name="data">The KNX byte array</param>
    /// <returns>The DateTime value</returns>
    public static DateTime Dpt19001ToDateTime(byte[] data)
    {
        if (data == null || data.Length < 8)
            return DateTime.MinValue;
        
        try
        {
            var year = 1900 + data[0];
            var month = (data[1] >> 4) & 0x0F;
            var day = ((data[1] & 0x0F) << 1) | ((data[2] >> 7) & 0x01);
            var hour = ((data[2] & 0x01) << 4) | ((data[3] >> 4) & 0x0F);
            var minute = ((data[3] & 0x0F) << 2) | ((data[4] >> 6) & 0x03);
            var second = data[4] & 0x3F;
            
            return new DateTime(year, month, day, hour, minute, second);
        }
        catch
        {
            return DateTime.MinValue;
        }
    }

    /// <summary>
    /// Gets a human-readable description of a DPT type.
    /// </summary>
    /// <param name="dptType">The DPT type string (e.g., "1.001", "5.001")</param>
    /// <returns>Description of the DPT type</returns>
    public static string GetDptDescription(string dptType)
    {
        return dptType switch
        {
            "1.001" => "Switch (Boolean)",
            "1.002" => "Boolean",
            "1.003" => "Enable",
            "1.008" => "Up/Down",
            "1.009" => "Open/Close",
            "5.001" => "Scaling (0-100%)",
            "5.003" => "Angle (0-360°)",
            "5.004" => "Percent U8 (0-255)",
            "7.001" => "Pulses (2-byte unsigned)",
            "7.002" => "Time Period (milliseconds)",
            "7.003" => "Time Period (10ms)",
            "7.004" => "Time Period (100ms)",
            "7.005" => "Time Period (seconds)",
            "7.006" => "Time Period (minutes)",
            "7.007" => "Time Period (hours)",
            "9.001" => "Temperature (°C)",
            "9.002" => "Temperature Difference (K)",
            "9.003" => "Kelvin/hour",
            "9.004" => "Lux (Illumination)",
            "9.005" => "Wind Speed (m/s)",
            "9.006" => "Pressure (Pa)",
            "9.007" => "Humidity (%)",
            "9.008" => "Air Quality (ppm)",
            "16.001" => "Character String (ASCII)",
            "19.001" => "Date Time",
            _ => $"Unknown DPT {dptType}"
        };
    }
}