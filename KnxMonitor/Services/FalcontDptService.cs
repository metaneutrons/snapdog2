using System.Reflection;
using Knx.Falcon;
using Microsoft.Extensions.Logging;

namespace KnxMonitor.Services;

/// <summary>
/// DPT service that leverages Falcon SDK's built-in decoding capabilities first.
/// </summary>
public class FalconDptService : IDptDecodingService
{
    private readonly ILogger<FalconDptService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FalconDptService"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public FalconDptService(ILogger<FalconDptService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public object? DecodeValue(byte[] data, string dptId)
    {
        if (data == null || data.Length == 0)
        {
            return null;
        }

        try
        {
            // Try to create a GroupValue using Falcon SDK
            var groupValue = CreateFalconGroupValue(data, dptId);
            if (groupValue != null)
            {
                var decodedValue = ExtractFalconDecodedValue(groupValue);
                if (decodedValue != null)
                {
                    _logger.LogDebug("Successfully decoded {DptId} using Falcon SDK: {Value}", dptId, decodedValue);
                    return decodedValue;
                }
            }

            // If Falcon SDK can't handle it, we have a problem - log it
            _logger.LogWarning(
                "Falcon SDK couldn't decode DPT {DptId} with data {Data} - this shouldn't happen for standard DPTs",
                dptId,
                Convert.ToHexString(data)
            );

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error using Falcon SDK to decode DPT {DptId}", dptId);
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

        // Try common DPT types and see which one Falcon SDK can decode
        var candidateDpts = GetCandidateDpts(data);

        foreach (var dptId in candidateDpts)
        {
            try
            {
                var decodedValue = DecodeValue(data, dptId);
                if (decodedValue != null)
                {
                    _logger.LogDebug("Auto-detected DPT {DptId} for data {Data}", dptId, Convert.ToHexString(data));
                    return (decodedValue, dptId);
                }
            }
            catch
            {
                // Try next candidate
                continue;
            }
        }

        _logger.LogDebug("Could not auto-detect DPT for data {Data}", Convert.ToHexString(data));
        return (null, null);
    }

    /// <inheritdoc/>
    public string FormatValue(object? value, string? dptId)
    {
        if (value == null)
        {
            return "null";
        }

        // Format based on DPT context and value type
        return (dptId, value) switch
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

            // Default formatting based on type
            (_, bool b) => b ? "true" : "false",
            (_, byte b) => $"{b}",
            (_, sbyte sb) => $"{sb}",
            (_, short s) => $"{s}",
            (_, ushort us) => $"{us}",
            (_, int i) => $"{i}",
            (_, uint ui) => $"{ui}",
            (_, float f) => $"{f:F2}",
            (_, double d) => $"{d:F2}",
            (_, string str) => str,
            _ => value.ToString() ?? "unknown",
        };
    }

    /// <inheritdoc/>
    public string? DetectDpt(byte[] data)
    {
        var (_, detectedDpt) = DecodeValueWithAutoDetection(data);
        return detectedDpt;
    }

    /// <inheritdoc/>
    public bool IsDptSupported(string dptId)
    {
        if (string.IsNullOrEmpty(dptId))
        {
            return false;
        }

        try
        {
            // Test with dummy data to see if Falcon SDK can handle this DPT
            var testData = GetTestDataForDpt(dptId);
            var result = DecodeValue(testData, dptId);
            return result != null;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public IEnumerable<string> GetSupportedDpts()
    {
        // Return DPTs that we know Falcon SDK supports
        // This is based on the KNX specification and Falcon SDK documentation
        return new[]
        {
            // DPT 1 - Boolean
            "1.001",
            "1.002",
            "1.003",
            "1.008",
            "1.009",
            "1.010",
            "1.011",
            "1.012",
            // DPT 2 - 1-bit controlled
            "2.001",
            "2.002",
            "2.003",
            "2.004",
            "2.005",
            "2.006",
            // DPT 3 - 3-bit controlled
            "3.007",
            "3.008",
            // DPT 4 - Character
            "4.001",
            "4.002",
            // DPT 5 - 8-bit unsigned
            "5.001",
            "5.003",
            "5.004",
            "5.005",
            "5.006",
            "5.010",
            // DPT 6 - 8-bit signed
            "6.001",
            "6.010",
            "6.020",
            // DPT 7 - 2-byte unsigned
            "7.001",
            "7.002",
            "7.003",
            "7.004",
            "7.005",
            "7.006",
            "7.007",
            // DPT 8 - 2-byte signed
            "8.001",
            "8.002",
            "8.003",
            "8.004",
            "8.005",
            "8.006",
            "8.007",
            // DPT 9 - 2-byte float
            "9.001",
            "9.002",
            "9.003",
            "9.004",
            "9.005",
            "9.006",
            "9.007",
            "9.008",
            "9.009",
            "9.010",
            "9.011",
            "9.020",
            "9.021",
            "9.022",
            "9.023",
            "9.024",
            // DPT 10 - Time
            "10.001",
            // DPT 11 - Date
            "11.001",
            // DPT 12 - 4-byte unsigned
            "12.001",
            "12.100",
            "12.101",
            "12.102",
            // DPT 13 - 4-byte signed
            "13.001",
            "13.002",
            "13.010",
            "13.011",
            "13.012",
            "13.013",
            "13.014",
            "13.015",
            // DPT 14 - 4-byte float
            "14.000",
            "14.001",
            "14.002",
            "14.003",
            "14.004",
            "14.005",
            "14.006",
            "14.007",
            "14.019",
            "14.027",
            "14.028",
            "14.029",
            "14.030",
            "14.031",
            "14.032",
            "14.033",
            "14.056",
            "14.057",
            "14.058",
            "14.059",
            "14.060",
            "14.061",
            "14.062",
            "14.063",
            "14.064",
            "14.065",
            "14.066",
            "14.067",
            "14.068",
            "14.069",
            "14.070",
            "14.071",
            "14.072",
            "14.073",
            "14.074",
            "14.075",
            "14.076",
            "14.077",
            "14.078",
            "14.079",
            // DPT 16 - String
            "16.000",
            "16.001",
            // DPT 17 - Scene number
            "17.001",
            // DPT 18 - Scene control
            "18.001",
            // DPT 19 - Date/Time
            "19.001",
            // DPT 20 - 1-byte enum
            "20.102",
            "20.103",
            "20.104",
            "20.105",
            "20.106",
            "20.107",
            "20.108",
        }.OrderBy(x => x);
    }

    /// <summary>
    /// Creates a Falcon SDK GroupValue from raw data and DPT ID.
    /// This is where the magic happens - we let Falcon SDK do the work!
    /// </summary>
    /// <param name="data">Raw KNX data.</param>
    /// <param name="dptId">DPT identifier.</param>
    /// <returns>Falcon SDK GroupValue or null.</returns>
    private object? CreateFalconGroupValue(byte[] data, string dptId)
    {
        try
        {
            // Method 1: Try GroupValue constructor with DPT string
            var groupValueType = typeof(GroupValue);
            var constructors = groupValueType.GetConstructors();

            foreach (var constructor in constructors)
            {
                var parameters = constructor.GetParameters();

                // Look for constructor that takes byte[] and string (DPT)
                if (
                    parameters.Length == 2
                    && parameters[0].ParameterType == typeof(byte[])
                    && parameters[1].ParameterType == typeof(string)
                )
                {
                    try
                    {
                        _logger.LogDebug("Trying GroupValue constructor with byte[] and string for DPT {DptId}", dptId);
                        return constructor.Invoke(new object[] { data, dptId });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "GroupValue constructor failed for DPT {DptId}", dptId);
                    }
                }
            }

            // Method 2: Try static factory methods
            var factoryMethods = groupValueType
                .GetMethods(BindingFlags.Static | BindingFlags.Public)
                .Where(m => m.Name.Contains("Create") || m.Name.Contains("From"))
                .ToArray();

            foreach (var method in factoryMethods)
            {
                var parameters = method.GetParameters();
                if (parameters.Length >= 2 && parameters[0].ParameterType == typeof(byte[]))
                {
                    try
                    {
                        _logger.LogDebug("Trying factory method {MethodName} for DPT {DptId}", method.Name, dptId);
                        return method.Invoke(null, new object[] { data, dptId });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Factory method {MethodName} failed for DPT {DptId}", method.Name, dptId);
                    }
                }
            }

            // Method 3: Try to find DPT-specific classes
            var falconAssembly = typeof(GroupValue).Assembly;
            var dptSpecificType = FindDptSpecificType(falconAssembly, dptId);
            if (dptSpecificType != null)
            {
                try
                {
                    _logger.LogDebug(
                        "Trying DPT-specific type {TypeName} for DPT {DptId}",
                        dptSpecificType.Name,
                        dptId
                    );
                    return CreateFromDptSpecificType(dptSpecificType, data);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(
                        ex,
                        "DPT-specific type {TypeName} failed for DPT {DptId}",
                        dptSpecificType.Name,
                        dptId
                    );
                }
            }

            _logger.LogWarning(
                "Could not create Falcon GroupValue for DPT {DptId} - no suitable constructor/factory found",
                dptId
            );
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Falcon GroupValue for DPT {DptId}", dptId);
            return null;
        }
    }

    /// <summary>
    /// Extracts the decoded value from a Falcon SDK GroupValue.
    /// </summary>
    /// <param name="groupValue">Falcon SDK GroupValue.</param>
    /// <returns>Decoded value.</returns>
    private object? ExtractFalconDecodedValue(object groupValue)
    {
        try
        {
            var type = groupValue.GetType();

            // Try common value property names
            var valueProperties = new[] { "Value", "DecodedValue", "TypedValue", "ConvertedValue" };

            foreach (var propName in valueProperties)
            {
                var prop = type.GetProperty(propName);
                if (prop != null)
                {
                    try
                    {
                        var value = prop.GetValue(groupValue);
                        if (value != null)
                        {
                            _logger.LogDebug(
                                "Extracted value from property {PropertyName}: {Value} ({ValueType})",
                                propName,
                                value,
                                value.GetType().Name
                            );
                            return value;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Error reading property {PropertyName}", propName);
                    }
                }
            }

            // Try conversion methods
            var valueMethods = new[] { "ToValue", "GetValue", "Convert", "AsValue" };

            foreach (var methodName in valueMethods)
            {
                var method = type.GetMethod(methodName, Type.EmptyTypes);
                if (method != null)
                {
                    try
                    {
                        var value = method.Invoke(groupValue, null);
                        if (value != null)
                        {
                            _logger.LogDebug(
                                "Extracted value from method {MethodName}: {Value} ({ValueType})",
                                methodName,
                                value,
                                value.GetType().Name
                            );
                            return value;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Error calling method {MethodName}", methodName);
                    }
                }
            }

            // If no specific value property/method, the object itself might be the value
            _logger.LogDebug(
                "Using GroupValue object itself as value: {Value} ({ValueType})",
                groupValue,
                groupValue.GetType().Name
            );
            return groupValue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting value from Falcon GroupValue");
            return null;
        }
    }

    /// <summary>
    /// Gets candidate DPT types based on data length and patterns.
    /// </summary>
    /// <param name="data">Raw data.</param>
    /// <returns>Ordered list of candidate DPT types.</returns>
    private IEnumerable<string> GetCandidateDpts(byte[] data)
    {
        return data.Length switch
        {
            1 => GetOneByteCandidates(data[0]),
            2 => GetTwoByteCandidates(data),
            4 => GetFourByteCandidates(data),
            _ => new[] { "16.000" }, // String fallback
        };
    }

    private IEnumerable<string> GetOneByteCandidates(byte value)
    {
        return value switch
        {
            0 or 1 => new[] { "1.001", "1.002", "1.003", "5.001", "5.004" },
            <= 100 => new[] { "5.001", "5.004", "6.001" },
            _ => new[] { "5.004", "6.001" },
        };
    }

    private IEnumerable<string> GetTwoByteCandidates(byte[] data)
    {
        // Prioritize DPT 9 (2-byte float) as it's most common for 2-byte values
        return new[] { "9.001", "9.004", "9.007", "7.001", "8.001" };
    }

    private IEnumerable<string> GetFourByteCandidates(byte[] data)
    {
        // Prioritize DPT 14 (4-byte float) as it's most common for 4-byte values
        return new[] { "14.068", "14.000", "14.019", "14.076", "12.001", "13.001" };
    }

    private byte[] GetTestDataForDpt(string dptId)
    {
        // Return appropriate test data based on DPT
        return dptId.Split('.')[0] switch
        {
            "1" => new byte[] { 0x01 },
            "5" or "6" => new byte[] { 0x80 },
            "7" or "8" or "9" => new byte[] { 0x08, 0x00 },
            "12" or "13" or "14" => new byte[] { 0x40, 0x00, 0x00, 0x00 },
            _ => new byte[] { 0x00 },
        };
    }

    private Type? FindDptSpecificType(Assembly assembly, string dptId)
    {
        try
        {
            var dptNumber = dptId.Split('.')[0];
            var possibleNames = new[]
            {
                $"Dpt{dptNumber}Value",
                $"DPT{dptNumber}Value",
                $"Dpt{dptNumber}",
                $"DPT{dptNumber}",
                $"DataPointType{dptNumber}",
            };

            foreach (var name in possibleNames)
            {
                var type = assembly
                    .GetTypes()
                    .FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private object? CreateFromDptSpecificType(Type dptType, byte[] data)
    {
        // Try constructors that take byte[]
        var constructors = dptType.GetConstructors();

        foreach (var constructor in constructors)
        {
            var parameters = constructor.GetParameters();
            if (parameters.Length == 1 && parameters[0].ParameterType == typeof(byte[]))
            {
                try
                {
                    return constructor.Invoke(new object[] { data });
                }
                catch
                {
                    continue;
                }
            }
        }

        // Try static factory methods
        var factoryMethods = dptType
            .GetMethods(BindingFlags.Static | BindingFlags.Public)
            .Where(m => m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(byte[]))
            .ToArray();

        foreach (var method in factoryMethods)
        {
            try
            {
                return method.Invoke(null, new object[] { data });
            }
            catch
            {
                continue;
            }
        }

        return null;
    }
}
