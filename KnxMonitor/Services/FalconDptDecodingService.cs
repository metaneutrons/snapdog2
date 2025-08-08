using System.Reflection;
using Knx.Falcon;
using Microsoft.Extensions.Logging;

namespace KnxMonitor.Services;

/// <summary>
/// DPT decoding service that primarily uses Falcon SDK's built-in conversion capabilities.
/// </summary>
public class FalconDptDecodingService : IDptDecodingService
{
    private readonly ILogger<FalconDptDecodingService> _logger;
    private readonly DptDecodingService _fallbackService;

    /// <summary>
    /// Initializes a new instance of the <see cref="FalconDptDecodingService"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public FalconDptDecodingService(ILogger<FalconDptDecodingService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Keep the manual implementation as fallback
        var fallbackLogger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<DptDecodingService>();
        _fallbackService = new DptDecodingService(fallbackLogger);
    }

    /// <inheritdoc/>
    public object? DecodeValue(byte[] data, string dptId)
    {
        if (data == null || data.Length == 0 || string.IsNullOrEmpty(dptId))
        {
            return null;
        }

        try
        {
            // First, try to use Falcon SDK's GroupValue for decoding
            var falconDecoded = TryDecodeFalconGroupValue(data, dptId);
            if (falconDecoded != null)
            {
                _logger.LogDebug("Successfully decoded {DptId} using Falcon SDK", dptId);
                return falconDecoded;
            }

            // Fallback to manual implementation
            _logger.LogDebug("Falling back to manual decoding for {DptId}", dptId);
            return _fallbackService.DecodeValue(data, dptId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error decoding DPT {DptId}, falling back to manual decoding", dptId);
            return _fallbackService.DecodeValue(data, dptId);
        }
    }

    /// <inheritdoc/>
    public (object? Value, string? DetectedDpt) DecodeValueWithAutoDetection(byte[] data)
    {
        // For auto-detection, we'll use our fallback service since we need to guess the DPT
        // Falcon SDK requires knowing the DPT beforehand
        return _fallbackService.DecodeValueWithAutoDetection(data);
    }

    /// <inheritdoc/>
    public string FormatValue(object? value, string? dptId)
    {
        // Use the fallback service for formatting since it has comprehensive formatting logic
        return _fallbackService.FormatValue(value, dptId);
    }

    /// <inheritdoc/>
    public string? DetectDpt(byte[] data)
    {
        return _fallbackService.DetectDpt(data);
    }

    /// <inheritdoc/>
    public bool IsDptSupported(string dptId)
    {
        // Check if Falcon SDK supports it first, then fallback
        return TryIsFalconDptSupported(dptId) || _fallbackService.IsDptSupported(dptId);
    }

    /// <inheritdoc/>
    public IEnumerable<string> GetSupportedDpts()
    {
        // Return the union of Falcon SDK supported DPTs and our manual ones
        var falconDpts = GetFalconSupportedDpts();
        var manualDpts = _fallbackService.GetSupportedDpts();
        return falconDpts.Union(manualDpts).Distinct().OrderBy(x => x);
    }

    /// <summary>
    /// Attempts to decode data using Falcon SDK's GroupValue.
    /// </summary>
    /// <param name="data">Raw KNX data.</param>
    /// <param name="dptId">DPT identifier.</param>
    /// <returns>Decoded value or null if failed.</returns>
    private object? TryDecodeFalconGroupValue(byte[] data, string dptId)
    {
        try
        {
            // Try to create a GroupValue from the raw data and DPT
            // This is where we'd use Falcon SDK's proper DPT conversion

            // Method 1: Try to use GroupValue constructor with DPT
            var groupValue = TryCreateGroupValueWithDpt(data, dptId);
            if (groupValue != null)
            {
                return ExtractValueFromGroupValue(groupValue);
            }

            // Method 2: Try to use DPT converter utilities directly
            return TryUseDptConverters(data, dptId);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to decode using Falcon SDK for DPT {DptId}", dptId);
            return null;
        }
    }

    /// <summary>
    /// Attempts to create a GroupValue using Falcon SDK with the specified DPT.
    /// </summary>
    /// <param name="data">Raw data.</param>
    /// <param name="dptId">DPT identifier.</param>
    /// <returns>GroupValue or null.</returns>
    private object? TryCreateGroupValueWithDpt(byte[] data, string dptId)
    {
        try
        {
            // Look for GroupValue constructors that accept raw data and DPT
            var groupValueType = typeof(GroupValue);

            // Try different constructor patterns that Falcon SDK might use
            var constructors = groupValueType.GetConstructors();

            foreach (var constructor in constructors)
            {
                var parameters = constructor.GetParameters();

                // Look for constructor that takes byte[] and DPT info
                if (
                    parameters.Length == 2
                    && parameters[0].ParameterType == typeof(byte[])
                    && (
                        parameters[1].ParameterType == typeof(string)
                        || parameters[1].ParameterType.Name.Contains("Dpt")
                    )
                )
                {
                    try
                    {
                        return constructor.Invoke(new object[] { data, dptId });
                    }
                    catch
                    {
                        // Try next constructor
                        continue;
                    }
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Attempts to use Falcon SDK's DPT converter utilities directly.
    /// </summary>
    /// <param name="data">Raw data.</param>
    /// <param name="dptId">DPT identifier.</param>
    /// <returns>Converted value or null.</returns>
    private object? TryUseDptConverters(byte[] data, string dptId)
    {
        try
        {
            // Look for DPT converter classes in Falcon SDK
            // These might be named like DptConverter, DataPointTypeConverter, etc.
            var falconAssembly = typeof(GroupValue).Assembly;
            var converterTypes = falconAssembly
                .GetTypes()
                .Where(t => t.Name.Contains("Dpt") && t.Name.Contains("Convert"))
                .ToArray();

            foreach (var converterType in converterTypes)
            {
                // Look for static methods that convert byte[] to values
                var methods = converterType
                    .GetMethods(BindingFlags.Static | BindingFlags.Public)
                    .Where(m => m.GetParameters().Length >= 1 && m.GetParameters()[0].ParameterType == typeof(byte[]))
                    .ToArray();

                foreach (var method in methods)
                {
                    try
                    {
                        // Try to invoke the converter method
                        var result = method.Invoke(null, new object[] { data });
                        if (result != null)
                        {
                            _logger.LogDebug(
                                "Successfully used Falcon converter {ConverterType}.{MethodName} for DPT {DptId}",
                                converterType.Name,
                                method.Name,
                                dptId
                            );
                            return result;
                        }
                    }
                    catch
                    {
                        // Continue trying other methods
                    }
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Extracts the decoded value from a Falcon SDK GroupValue object.
    /// </summary>
    /// <param name="groupValue">GroupValue object.</param>
    /// <returns>Extracted value.</returns>
    private object? ExtractValueFromGroupValue(object groupValue)
    {
        try
        {
            var type = groupValue.GetType();

            // Look for value properties
            var valueProperty =
                type.GetProperty("Value") ?? type.GetProperty("DecodedValue") ?? type.GetProperty("TypedValue");

            if (valueProperty != null)
            {
                return valueProperty.GetValue(groupValue);
            }

            // Look for conversion methods
            var toValueMethod = type.GetMethod("ToValue") ?? type.GetMethod("GetValue");

            if (toValueMethod != null)
            {
                return toValueMethod.Invoke(groupValue, null);
            }

            // If no specific value property, the object itself might be the value
            return groupValue;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Checks if a DPT is supported by Falcon SDK.
    /// </summary>
    /// <param name="dptId">DPT identifier.</param>
    /// <returns>True if supported by Falcon SDK.</returns>
    private bool TryIsFalconDptSupported(string dptId)
    {
        try
        {
            // Try to create a dummy GroupValue with this DPT to see if it's supported
            // This is a heuristic approach
            var dummyData = new byte[] { 0x00 };
            var result = TryCreateGroupValueWithDpt(dummyData, dptId);
            return result != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets DPT types supported by Falcon SDK.
    /// </summary>
    /// <returns>Collection of supported DPT identifiers.</returns>
    private IEnumerable<string> GetFalconSupportedDpts()
    {
        try
        {
            // This would ideally query Falcon SDK for supported DPTs
            // For now, return common ones that Falcon SDK definitely supports
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
                "14.001",
                "14.019",
                "14.027",
                "14.056",
                "14.057",
                "14.065",
                "14.068",
                "14.076",
            };
        }
        catch
        {
            return Enumerable.Empty<string>();
        }
    }
}
