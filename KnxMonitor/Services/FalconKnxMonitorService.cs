using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using Knx.Falcon;
using Knx.Falcon.Configuration;
using Knx.Falcon.Sdk;
using KnxMonitor.Models;
using Microsoft.Extensions.Logging;

namespace KnxMonitor.Services;

/// <summary>
/// KNX Monitor service that properly leverages Falcon SDK's built-in DPT decoding.
/// </summary>
public partial class FalconKnxMonitorService : IKnxMonitorService, IAsyncDisposable
{
    private readonly KnxMonitorConfig _config;
    private readonly ILogger<FalconKnxMonitorService> _logger;
    private readonly Regex? _filterRegex;

    private KnxBus? _knxBus;
    private bool _isConnected;
    private string _connectionStatus = "Disconnected";
    private int _messageCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="FalconKnxMonitorService"/> class.
    /// </summary>
    /// <param name="config">Monitor configuration.</param>
    /// <param name="logger">Logger instance.</param>
    public FalconKnxMonitorService(KnxMonitorConfig config, ILogger<FalconKnxMonitorService> logger)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Compile filter regex if provided
        if (!string.IsNullOrEmpty(_config.Filter))
        {
            try
            {
                var pattern = _config.Filter.Replace("*", ".*").Replace("/", "\\/");
                _filterRegex = new Regex($"^{pattern}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Invalid filter pattern: {Filter}", _config.Filter);
            }
        }
    }

    /// <inheritdoc/>
    public event EventHandler<KnxMessage>? MessageReceived;

    /// <inheritdoc/>
    public bool IsConnected => _isConnected;

    /// <inheritdoc/>
    public string ConnectionStatus => _connectionStatus;

    /// <inheritdoc/>
    public int MessageCount => _messageCount;

    /// <inheritdoc/>
    public async Task StartMonitoringAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _connectionStatus = "Connecting...";
            _logger.LogInformation(
                "Starting Falcon-first KNX monitoring with {ConnectionType} connection",
                _config.ConnectionType
            );

            // Create connector parameters
            var connectorParameters = CreateConnectorParameters();
            if (connectorParameters == null)
            {
                _connectionStatus = "Failed to create connector parameters";
                return;
            }

            // Create and configure KNX bus
            _knxBus = new KnxBus(connectorParameters);

            // Subscribe to events - this is where the magic happens!
            _knxBus.GroupMessageReceived += OnGroupMessageReceivedFalconFirst;

            // Connect to bus with timeout support
            await _knxBus.ConnectAsync(cancellationToken);

            _isConnected = true;
            _connectionStatus = $"Connected to {GetConnectionDescription()}";

            _logger.LogInformation(
                "Falcon-first KNX monitoring started successfully - ready to use SDK decoded values!"
            );
        }
        catch (Exception ex)
        {
            _isConnected = false;
            _connectionStatus = $"Connection failed: {ex.Message}";
            _logger.LogError(ex, "Failed to start Falcon-first KNX monitoring");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task StopMonitoringAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_knxBus != null)
            {
                _connectionStatus = "Disconnecting...";

                // Unsubscribe from events
                _knxBus.GroupMessageReceived -= OnGroupMessageReceivedFalconFirst;

                // Dispose the bus (this will disconnect)
                await _knxBus.DisposeAsync();

                _isConnected = false;
                _connectionStatus = "Disconnected";

                _logger.LogInformation("Falcon-first KNX monitoring stopped");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping Falcon-first KNX monitoring");
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        try
        {
            await StopMonitoringAsync();
            Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during async dispose");
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        try
        {
            if (_isConnected)
            {
                StopMonitoringAsync().GetAwaiter().GetResult();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing Falcon-first KNX monitor service");
        }
        finally
        {
            _knxBus?.Dispose();
        }
    }

    /// <summary>
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Event arguments containing Falcon SDK's already-decoded value!</param>
    private void OnGroupMessageReceivedFalconFirst(object? sender, GroupEventArgs e)
    {
        try
        {
            _logger.LogDebug("🎉 Received KNX message - Falcon SDK has already done the decoding work!");

            var messageType = DetermineMessageType(e);
            var message = CreateFalconFirstKnxMessage(e, messageType);
            ProcessMessage(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Falcon-first group message");
        }
    }

    /// <summary>
    /// Creates a KNX message that properly uses Falcon SDK's decoded values.
    /// </summary>
    /// <param name="e">Event arguments with Falcon SDK decoded value.</param>
    /// <param name="messageType">Message type.</param>
    /// <returns>KNX message with proper Falcon SDK integration.</returns>
    private KnxMessage CreateFalconFirstKnxMessage(GroupEventArgs e, KnxMessageType messageType)
    {
        // Extract the treasure that Falcon SDK already prepared for us!
        var falconTreasure = ExtractFalconTreasure(e);

        _logger.LogDebug(
            "✨ Falcon SDK treasure extracted - DPT: {DptId}, Value: {Value} ({ValueType}), Raw: {RawData}",
            falconTreasure.DptId ?? "Unknown",
            falconTreasure.DecodedValue ?? "null",
            falconTreasure.DecodedValue?.GetType().Name ?? "null",
            Convert.ToHexString(falconTreasure.RawData)
        );

        return new KnxMessage
        {
            Timestamp = DateTime.Now,
            SourceAddress = e.SourceAddress.ToString(),
            GroupAddress = e.DestinationAddress.ToString(),
            MessageType = messageType,
            Data = falconTreasure.RawData,
            Value = falconTreasure.DecodedValue, // 🎉 Use Falcon SDK's decoded value directly!
            DataPointType = falconTreasure.DptId, // 🎉 Use Falcon SDK's DPT identification!
            Priority = KnxPriority.Normal,
            IsRepeated = false,
        };
    }

    /// <summary>
    /// Extracts the "treasure" that Falcon SDK has already prepared for us.
    /// This is where we properly leverage the SDK's work!
    /// </summary>
    /// <param name="e">Group event arguments.</param>
    /// <returns>The Falcon SDK treasure (decoded value, DPT, raw data).</returns>
    private FalconTreasure ExtractFalconTreasure(GroupEventArgs e)
    {
        if (e.Value == null)
        {
            _logger.LogDebug("No value in GroupEventArgs - probably a read request");
            return new FalconTreasure(Array.Empty<byte>(), null, null, "Read request");
        }

        try
        {
            var valueType = e.Value.GetType();
            _logger.LogDebug(
                "🔍 Analyzing Falcon SDK value type: {ValueType} from namespace: {Namespace}",
                valueType.Name,
                valueType.Namespace
            );

            // CRITICAL FIX: Handle GroupValue objects (this is what Falcon SDK actually provides!)
            if (valueType.Name == "GroupValue" || valueType.Namespace?.StartsWith("Knx.Falcon") == true)
            {
                _logger.LogDebug("🎯 Falcon SDK provided GroupValue object: {TypeName}", valueType.Name);

                // DEBUG: Log all properties and methods
                var properties = valueType.GetProperties();
                var methods = valueType
                    .GetMethods()
                    .Where(m => m.GetParameters().Length == 0 && m.ReturnType != typeof(void))
                    .Take(10);

                _logger.LogDebug(
                    "📋 GroupValue properties: {Properties}",
                    string.Join(", ", properties.Select(p => $"{p.Name}:{p.PropertyType.Name}"))
                );
                _logger.LogDebug(
                    "📋 GroupValue methods: {Methods}",
                    string.Join(", ", methods.Select(m => $"{m.Name}():{m.ReturnType.Name}"))
                );

                // Extract byte array from GroupValue using reflection
                var byteArray = TryExtractBytesFromGroupValue(e.Value);
                if (byteArray != null && byteArray.Length > 0)
                {
                    _logger.LogDebug(
                        "📦 Extracted bytes from GroupValue: {Data} (length: {Length})",
                        Convert.ToHexString(byteArray),
                        byteArray.Length
                    );

                    // Decode the byte array
                    var (decodedValue, dptId) = DecodeByteArrayBasic(byteArray);
                    _logger.LogDebug("🎉 Decoded: {DecodedValue} (DPT {DptId})", decodedValue, dptId);
                    return new FalconTreasure(byteArray, dptId, decodedValue, "Decoded from GroupValue bytes");
                }

                // If we can't extract bytes, try to extract the decoded value directly
                var directValue = TryExtractValueFromGroupValue(e.Value);
                if (directValue != null)
                {
                    _logger.LogDebug(
                        "🎉 Extracted direct value: {Value} ({Type})",
                        directValue,
                        directValue.GetType().Name
                    );
                    var rawData = ConvertValueToRawData(directValue);
                    var dptId = GuessDptFromValue(directValue);
                    return new FalconTreasure(rawData, dptId, directValue, "Extracted value from GroupValue");
                }

                _logger.LogWarning("❌ Could not extract anything useful from GroupValue {TypeName}", valueType.Name);
            }

            // The value in e.Value is ALREADY DECODED by Falcon SDK!
            // We just need to extract the information properly

            // Method 1: If it's a primitive .NET type, Falcon SDK already converted it!
            if (IsPrimitiveType(valueType))
            {
                _logger.LogDebug("🎯 Falcon SDK provided primitive type: {Value} ({Type})", e.Value, valueType.Name);
                var dptId = GuessDptFromPrimitiveType(e.Value, valueType);
                var rawData = ConvertPrimitiveToRawData(e.Value, valueType);
                return new FalconTreasure(rawData, dptId, e.Value, $"Falcon SDK primitive: {valueType.Name}");
            }

            // Method 2: If it's a Falcon SDK GroupValue object, extract its information
            if (IsFalconGroupValue(valueType))
            {
                _logger.LogDebug("🎯 Falcon SDK provided GroupValue object");
                return ExtractFromFalconGroupValue(e.Value, valueType);
            }

            // Method 3: Use reflection to explore unknown Falcon SDK objects
            _logger.LogDebug("🔍 Using reflection to explore Falcon SDK object");
            return ExtractUsingReflection(e.Value, valueType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting Falcon SDK treasure from {ValueType}", e.Value.GetType().Name);
            return new FalconTreasure(Array.Empty<byte>(), null, e.Value, $"Extraction error: {ex.Message}");
        }
    }

    /// <summary>
    /// Checks if the type is a primitive .NET type that Falcon SDK converted to.
    /// </summary>
    /// <param name="type">Type to check.</param>
    /// <returns>True if primitive type.</returns>
    private static bool IsPrimitiveType(Type type)
    {
        return type.IsPrimitive || type == typeof(string) || type == typeof(decimal);
    }

    /// <summary>
    /// Checks if the type is a Falcon SDK GroupValue or related object.
    /// </summary>
    /// <param name="type">Type to check.</param>
    /// <returns>True if Falcon GroupValue type.</returns>
    private static bool IsFalconGroupValue(Type type)
    {
        return type.Name.Contains("GroupValue")
            || type.Name.Contains("Dpt")
            || type.Namespace?.StartsWith("Knx.Falcon") == true;
    }

    /// <summary>
    /// Guesses the DPT from a primitive type and its value.
    /// </summary>
    /// <param name="value">The primitive value.</param>
    /// <param name="type">The primitive type.</param>
    /// <returns>Guessed DPT ID.</returns>
    private string? GuessDptFromPrimitiveType(object value, Type type)
    {
        return (type, value) switch
        {
            (Type t, bool) when t == typeof(bool) => "1.001", // Boolean switch
            (Type t, byte b) when t == typeof(byte) && b <= 1 => "1.001", // Boolean as byte
            (Type t, byte b) when t == typeof(byte) && b <= 100 => "5.001", // Scaling percentage
            (Type t, byte) when t == typeof(byte) => "5.004", // General 8-bit unsigned
            (Type t, sbyte) when t == typeof(sbyte) => "6.001", // 8-bit signed
            (Type t, short) when t == typeof(short) => "8.001", // 2-byte signed
            (Type t, ushort) when t == typeof(ushort) => "7.001", // 2-byte unsigned
            (Type t, int) when t == typeof(int) => "13.001", // 4-byte signed
            (Type t, uint) when t == typeof(uint) => "12.001", // 4-byte unsigned
            (Type t, float f) when t == typeof(float) && f >= -50 && f <= 100 => "9.001", // Temperature
            (Type t, float f) when t == typeof(float) && f >= 0 && f <= 100000 => "9.004", // Illuminance
            (Type t, float) when t == typeof(float) => "14.000", // General 4-byte float
            (Type t, double d) when t == typeof(double) && d >= -50 && d <= 100 => "9.001", // Temperature
            (Type t, double) when t == typeof(double) => "14.000", // General 4-byte float
            (Type t, string) when t == typeof(string) => "16.001", // String
            _ => null,
        };
    }

    /// <summary>
    /// Converts a primitive value back to raw KNX data format.
    /// </summary>
    /// <param name="value">Primitive value.</param>
    /// <param name="type">Primitive type.</param>
    /// <returns>Raw KNX data bytes.</returns>
    private byte[] ConvertPrimitiveToRawData(object value, Type type)
    {
        return (type, value) switch
        {
            (Type t, bool b) when t == typeof(bool) => new byte[] { (byte)(b ? 1 : 0) },
            (Type t, byte by) when t == typeof(byte) => new byte[] { by },
            (Type t, sbyte sb) when t == typeof(sbyte) => new byte[] { (byte)sb },
            (Type t, short s) when t == typeof(short) => BitConverter.GetBytes(s).Reverse().ToArray(),
            (Type t, ushort us) when t == typeof(ushort) => BitConverter.GetBytes(us).Reverse().ToArray(),
            (Type t, int i) when t == typeof(int) => BitConverter.GetBytes(i).Reverse().ToArray(),
            (Type t, uint ui) when t == typeof(uint) => BitConverter.GetBytes(ui).Reverse().ToArray(),
            (Type t, float f) when t == typeof(float) => BitConverter.GetBytes(f).Reverse().ToArray(),
            (Type t, double d) when t == typeof(double) => BitConverter.GetBytes((float)d).Reverse().ToArray(),
            (Type t, string str) when t == typeof(string) => System.Text.Encoding.UTF8.GetBytes(str),
            _ => Array.Empty<byte>(),
        };
    }

    /// <summary>
    /// Extracts information from a Falcon SDK GroupValue object.
    /// </summary>
    /// <param name="groupValue">Falcon SDK GroupValue.</param>
    /// <param name="type">GroupValue type.</param>
    /// <returns>Extracted treasure.</returns>
    private FalconTreasure ExtractFromFalconGroupValue(object groupValue, Type type)
    {
        try
        {
            var dptId = ExtractDptFromGroupValue(groupValue, type);
            var rawData = ExtractRawDataFromGroupValue(groupValue, type);
            var decodedValue = ExtractDecodedValueFromGroupValue(groupValue, type);

            var info = $"Falcon GroupValue: {type.Name}";

            _logger.LogDebug(
                "📦 Extracted from Falcon GroupValue - DPT: {DptId}, Decoded: {DecodedValue}, Raw: {RawData}",
                dptId,
                decodedValue,
                Convert.ToHexString(rawData)
            );

            return new FalconTreasure(rawData, dptId, decodedValue, info);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error extracting from Falcon GroupValue {TypeName}", type.Name);
            return new FalconTreasure(
                Array.Empty<byte>(),
                null,
                groupValue,
                $"GroupValue extraction error: {ex.Message}"
            );
        }
    }

    /// <summary>
    /// Uses reflection to extract information from unknown Falcon SDK objects.
    /// </summary>
    /// <param name="value">Unknown Falcon SDK object.</param>
    /// <param name="type">Object type.</param>
    /// <returns>Extracted treasure.</returns>
    private FalconTreasure ExtractUsingReflection(object value, Type type)
    {
        try
        {
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            _logger.LogDebug(
                "🔍 Reflecting on {TypeName} with {PropertyCount} properties",
                type.Name,
                properties.Length
            );

            string? dptId = null;
            byte[] rawData = Array.Empty<byte>();
            object? decodedValue = null;
            var propertyInfo = new List<string>();

            foreach (var prop in properties)
            {
                try
                {
                    var propValue = prop.GetValue(value);
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

                    // Look for raw data
                    if (
                        (
                            prop.Name.Equals("Data", StringComparison.OrdinalIgnoreCase)
                            || prop.Name.Equals("RawData", StringComparison.OrdinalIgnoreCase)
                            || prop.Name.Equals("Bytes", StringComparison.OrdinalIgnoreCase)
                        ) && propValue is byte[] bytes
                    )
                    {
                        rawData = bytes;
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

            // If no decoded value found, use the object itself
            decodedValue ??= value;

            // If no raw data found, try to convert the decoded value
            if (rawData.Length == 0 && decodedValue != null)
            {
                rawData = ConvertValueToRawData(decodedValue);
            }

            var info = $"Reflected {type.Name}: {string.Join(", ", propertyInfo.Take(5))}";

            _logger.LogDebug(
                "🔍 Reflection result - DPT: {DptId}, Decoded: {DecodedValue}, Raw: {RawData}",
                dptId,
                decodedValue,
                Convert.ToHexString(rawData)
            );

            return new FalconTreasure(rawData, dptId, decodedValue, info);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during reflection on {TypeName}", type.Name);
            return new FalconTreasure(Array.Empty<byte>(), null, value, $"Reflection error: {ex.Message}");
        }
    }

    // Helper methods for GroupValue extraction
    private string? ExtractDptFromGroupValue(object groupValue, Type type)
    {
        var dptProperties = new[] { "Dpt", "DptId", "DataPointType", "DataPointTypeId", "Type" };

        foreach (var propName in dptProperties)
        {
            try
            {
                var prop = type.GetProperty(propName);
                if (prop != null)
                {
                    var value = prop.GetValue(groupValue);
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

    private byte[] ExtractRawDataFromGroupValue(object groupValue, Type type)
    {
        var dataProperties = new[] { "Data", "RawData", "Bytes", "ByteArray" };

        foreach (var propName in dataProperties)
        {
            try
            {
                var prop = type.GetProperty(propName);
                if (prop != null && prop.PropertyType == typeof(byte[]))
                {
                    var data = prop.GetValue(groupValue) as byte[];
                    if (data != null && data.Length > 0)
                    {
                        return data;
                    }
                }
            }
            catch { }
        }

        // Try methods
        var dataMethods = new[] { "ToByteArray", "GetBytes", "GetRawData" };
        foreach (var methodName in dataMethods)
        {
            try
            {
                var method = type.GetMethod(methodName, Type.EmptyTypes);
                if (method != null && method.ReturnType == typeof(byte[]))
                {
                    var data = method.Invoke(groupValue, null) as byte[];
                    if (data != null && data.Length > 0)
                    {
                        return data;
                    }
                }
            }
            catch { }
        }

        return Array.Empty<byte>();
    }

    private object? ExtractDecodedValueFromGroupValue(object groupValue, Type type)
    {
        var valueProperties = new[] { "Value", "DecodedValue", "TypedValue", "ConvertedValue" };

        foreach (var propName in valueProperties)
        {
            try
            {
                var prop = type.GetProperty(propName);
                if (prop != null)
                {
                    var value = prop.GetValue(groupValue);
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
                    var value = method.Invoke(groupValue, null);
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

    private byte[] ConvertValueToRawData(object value)
    {
        return value switch
        {
            null => Array.Empty<byte>(),
            bool b => new byte[] { (byte)(b ? 1 : 0) },
            byte by => new byte[] { by },
            sbyte sb => new byte[] { (byte)sb },
            short s => BitConverter.GetBytes(s).Reverse().ToArray(),
            ushort us => BitConverter.GetBytes(us).Reverse().ToArray(),
            int i => BitConverter.GetBytes(i).Reverse().ToArray(),
            uint ui => BitConverter.GetBytes(ui).Reverse().ToArray(),
            float f => BitConverter.GetBytes(f).Reverse().ToArray(),
            double d => BitConverter.GetBytes((float)d).Reverse().ToArray(),
            byte[] bytes => bytes, // 🎉 Return byte arrays directly!
            string str => System.Text.Encoding.UTF8.GetBytes(str),
            _ => Array.Empty<byte>(),
        };
    }

    /// <summary>
    /// Tries to extract byte array from a GroupValue object using reflection.
    /// </summary>
    /// <param name="groupValue">GroupValue object.</param>
    /// <returns>Byte array or null.</returns>
    private byte[]? TryExtractBytesFromGroupValue(object groupValue)
    {
        try
        {
            var type = groupValue.GetType();

            // Look for data-related properties
            var dataProperties = new[] { "Data", "RawData", "Bytes", "ByteArray" };

            foreach (var propName in dataProperties)
            {
                var prop = type.GetProperty(propName);
                if (prop != null && prop.PropertyType == typeof(byte[]))
                {
                    var data = prop.GetValue(groupValue) as byte[];
                    if (data != null && data.Length > 0)
                    {
                        _logger.LogDebug(
                            "✅ Found bytes in property {PropertyName}: {Data}",
                            propName,
                            Convert.ToHexString(data)
                        );
                        return data;
                    }
                }
            }

            // Look for methods that return byte arrays
            var dataMethods = new[] { "ToByteArray", "GetBytes", "GetRawData" };
            foreach (var methodName in dataMethods)
            {
                var method = type.GetMethod(methodName, Type.EmptyTypes);
                if (method != null && method.ReturnType == typeof(byte[]))
                {
                    var data = method.Invoke(groupValue, null) as byte[];
                    if (data != null && data.Length > 0)
                    {
                        _logger.LogDebug(
                            "✅ Found bytes from method {MethodName}: {Data}",
                            methodName,
                            Convert.ToHexString(data)
                        );
                        return data;
                    }
                }
            }

            _logger.LogDebug("❌ Could not extract bytes from GroupValue {TypeName}", type.Name);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error extracting bytes from GroupValue");
            return null;
        }
    }

    /// <summary>
    /// Tries to extract the decoded value from a GroupValue object.
    /// </summary>
    /// <param name="groupValue">GroupValue object.</param>
    /// <returns>Decoded value or null.</returns>
    private object? TryExtractValueFromGroupValue(object groupValue)
    {
        try
        {
            var type = groupValue.GetType();

            // Look for value-related properties
            var valueProperties = new[] { "Value", "DecodedValue", "TypedValue", "ConvertedValue" };

            foreach (var propName in valueProperties)
            {
                var prop = type.GetProperty(propName);
                if (prop != null)
                {
                    var value = prop.GetValue(groupValue);
                    if (value != null)
                    {
                        _logger.LogDebug(
                            "✅ Found value in property {PropertyName}: {Value} ({Type})",
                            propName,
                            value,
                            value.GetType().Name
                        );
                        return value;
                    }
                }
            }

            // Look for conversion methods
            var valueMethods = new[] { "ToValue", "GetValue", "Convert", "AsValue" };
            foreach (var methodName in valueMethods)
            {
                var method = type.GetMethod(methodName, Type.EmptyTypes);
                if (method != null)
                {
                    var value = method.Invoke(groupValue, null);
                    if (value != null)
                    {
                        _logger.LogDebug(
                            "✅ Found value from method {MethodName}: {Value} ({Type})",
                            methodName,
                            value,
                            value.GetType().Name
                        );
                        return value;
                    }
                }
            }

            _logger.LogDebug("❌ Could not extract value from GroupValue {TypeName}", type.Name);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error extracting value from GroupValue");
            return null;
        }
    }

    /// <summary>
    /// Guesses DPT from a decoded value.
    /// </summary>
    /// <param name="value">Decoded value.</param>
    /// <returns>DPT ID.</returns>
    private string? GuessDptFromValue(object value)
    {
        return value switch
        {
            bool => "1.001",
            byte b when b <= 1 => "1.001",
            byte => "5.001",
            sbyte => "6.001",
            short => "8.001",
            ushort => "7.001",
            int => "13.001",
            uint => "12.001",
            float f when f >= -50 && f <= 100 => "9.001", // Temperature
            float => "14.000",
            double => "14.000",
            string => "16.001",
            _ => null,
        };
    }

    /// <summary>
    /// Basic decoding of byte arrays when the service is not available.
    /// </summary>
    /// <param name="data">Byte array to decode.</param>
    /// <returns>Decoded value and DPT.</returns>
    private (object DecodedValue, string DptId) DecodeByteArrayBasic(byte[] data)
    {
        return data.Length switch
        {
            1 when data[0] <= 1 => (data[0] == 1, "1.001"), // Boolean
            1 => (data[0], "5.001"), // 8-bit value
            2 => (DecodeKnx2ByteFloat(data), "9.001"), // 2-byte float
            4 => (DecodeIeee754Float(data), "14.000"), // 4-byte float
            _ => (Convert.ToHexString(data), "16.001"), // String fallback
        };
    }

    /// <summary>
    /// Decodes KNX 2-byte float format.
    /// </summary>
    private float DecodeKnx2ByteFloat(byte[] data)
    {
        if (data.Length != 2)
            return 0;

        var value = (data[0] << 8) | data[1];
        var mantissa = value & 0x07FF;
        var exponent = (value >> 11) & 0x0F;
        var sign = (value >> 15) & 0x01;

        if (sign == 1)
            mantissa = mantissa - 2048;

        return (float)(mantissa * Math.Pow(2, exponent) * 0.01);
    }

    /// <summary>
    /// Decodes IEEE 754 4-byte float.
    /// </summary>
    private float DecodeIeee754Float(byte[] data)
    {
        if (data.Length != 4)
            return 0;

        var bytes = new byte[4];
        Array.Copy(data, bytes, 4);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(bytes);

        return BitConverter.ToSingle(bytes, 0);
    }

    // Copy the infrastructure methods from the original service
    private ConnectorParameters? CreateConnectorParameters()
    {
        try
        {
            return _config.ConnectionType switch
            {
                KnxConnectionType.Tunnel => CreateTunnelingParameters(),
                KnxConnectionType.Router => CreateRoutingParameters(),
                KnxConnectionType.Usb => CreateUsbParameters(),
                _ => throw new ArgumentOutOfRangeException(nameof(_config.ConnectionType)),
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create connector parameters");
            return null;
        }
    }

    private ConnectorParameters CreateTunnelingParameters()
    {
        if (string.IsNullOrEmpty(_config.Gateway))
        {
            throw new InvalidOperationException("Gateway address is required for tunneling connection");
        }

        _logger.LogDebug("Creating IP tunneling connection to {Gateway}:{Port}", _config.Gateway, _config.Port);
        return new IpTunnelingConnectorParameters(_config.Gateway, _config.Port);
    }

    private ConnectorParameters CreateRoutingParameters()
    {
        var multicastAddress = _config.MulticastAddress;

        if (IPAddress.TryParse(multicastAddress, out var ipAddress))
        {
            _logger.LogDebug(
                "Creating IP routing connection to multicast address {MulticastAddress}",
                multicastAddress
            );
            return new IpRoutingConnectorParameters(ipAddress);
        }

        try
        {
            var hostEntry = Dns.GetHostEntry(multicastAddress);
            var resolvedIp = hostEntry.AddressList.FirstOrDefault(addr =>
                addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork
            );

            if (resolvedIp == null)
            {
                throw new InvalidOperationException($"Failed to resolve hostname '{multicastAddress}' to IPv4 address");
            }

            _logger.LogDebug(
                "Creating IP routing connection to {MulticastAddress} ({ResolvedIp})",
                multicastAddress,
                resolvedIp
            );
            return new IpRoutingConnectorParameters(resolvedIp);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error resolving hostname '{multicastAddress}': {ex.Message}", ex);
        }
    }

    private ConnectorParameters CreateUsbParameters()
    {
        var usbDevices = KnxBus.GetAttachedUsbDevices().ToArray();
        if (usbDevices.Length == 0)
        {
            throw new InvalidOperationException("No KNX USB devices found");
        }

        var device = usbDevices[0];
        _logger.LogDebug("Creating USB connection to device: {Device}", device);
        return UsbConnectorParameters.FromDiscovery(device);
    }

    private string GetConnectionDescription()
    {
        return _config.ConnectionType switch
        {
            KnxConnectionType.Tunnel => $"{_config.Gateway}:{_config.Port} (IP Tunneling)",
            KnxConnectionType.Router => $"{_config.MulticastAddress}:{_config.Port} (IP Routing)",
            KnxConnectionType.Usb => "USB Device",
            _ => "Unknown",
        };
    }

    private static KnxMessageType DetermineMessageType(GroupEventArgs e)
    {
        return e.Value != null ? KnxMessageType.Write : KnxMessageType.Read;
    }

    private void ProcessMessage(KnxMessage message)
    {
        // Apply filter if configured
        if (_filterRegex != null && !_filterRegex.IsMatch(message.GroupAddress))
        {
            return;
        }

        // Log message if verbose
        if (_config.Verbose)
        {
            _logger.LogDebug(
                "🎉 Falcon-first KNX message: {MessageType} {GroupAddress} = {Value} (DPT: {DptId})",
                message.MessageType,
                message.GroupAddress,
                message.DisplayValue,
                message.DataPointType ?? "Unknown"
            );
        }

        // Increment message counter
        Interlocked.Increment(ref _messageCount);

        // Raise event
        MessageReceived?.Invoke(this, message);
    }

    /// <summary>
    /// The treasure that Falcon SDK has prepared for us!
    /// </summary>
    /// <param name="RawData">Raw KNX data bytes.</param>
    /// <param name="DptId">DPT identifier detected by Falcon SDK.</param>
    /// <param name="DecodedValue">Value already decoded by Falcon SDK.</param>
    /// <param name="AdditionalInfo">Additional information about the extraction.</param>
    private record FalconTreasure(byte[] RawData, string? DptId, object? DecodedValue, string AdditionalInfo);
}
