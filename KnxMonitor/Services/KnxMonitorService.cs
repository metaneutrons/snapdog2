using System.Net;
using System.Text.RegularExpressions;
using Knx.Falcon;
using Knx.Falcon.Configuration;
using Knx.Falcon.Sdk;
using KnxMonitor.Models;
using Microsoft.Extensions.Logging;

namespace KnxMonitor.Services;

/// <summary>
/// Service for monitoring KNX bus activity.
/// </summary>
public partial class KnxMonitorService : IKnxMonitorService, IAsyncDisposable
{
    private readonly KnxMonitorConfig _config;
    private readonly ILogger<KnxMonitorService> _logger;
    private readonly Regex? _filterRegex;

    private KnxBus? _knxBus;
    private bool _isConnected;
    private string _connectionStatus = "Disconnected";
    private int _messageCount;

    // Static logger for static methods
    private static ILogger<KnxMonitorService>? _staticLogger;

    /// <summary>
    /// Initializes a new instance of the <see cref="KnxMonitorService"/> class.
    /// </summary>
    /// <param name="config">Monitor configuration.</param>
    /// <param name="logger">Logger instance.</param>
    public KnxMonitorService(KnxMonitorConfig config, ILogger<KnxMonitorService> logger)
    {
        this._config = config ?? throw new ArgumentNullException(nameof(config));
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Set static logger for static methods
        _staticLogger = this._logger;

        // Compile filter regex if provided
        if (!string.IsNullOrEmpty(this._config.Filter))
        {
            try
            {
                var pattern = this._config.Filter.Replace("*", ".*").Replace("/", "\\/");
                this._filterRegex = new Regex($"^{pattern}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            }
            catch (Exception ex)
            {
                this._logger.LogWarning(ex, "Invalid filter pattern: {Filter}", this._config.Filter);
            }
        }
    }

    /// <inheritdoc/>
    public event EventHandler<KnxMessage>? MessageReceived;

    /// <inheritdoc/>
    public bool IsConnected => this._isConnected;

    /// <inheritdoc/>
    public string ConnectionStatus => this._connectionStatus;

    /// <inheritdoc/>
    public int MessageCount => this._messageCount;

    /// <inheritdoc/>
    public async Task StartMonitoringAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            this._connectionStatus = "Connecting...";
            this._logger.LogInformation(
                "Starting KNX monitoring with {ConnectionType} connection",
                this._config.ConnectionType
            );

            // Create connector parameters
            var connectorParameters = this.CreateConnectorParameters();
            if (connectorParameters == null)
            {
                this._connectionStatus = "Failed to create connector parameters";
                return;
            }

            // Create and configure KNX bus
            this._knxBus = new KnxBus(connectorParameters);

            // Subscribe to events
            this._knxBus.GroupMessageReceived += this.OnGroupMessageReceived;

            // Connect to bus with timeout support
            await this._knxBus.ConnectAsync(cancellationToken);

            this._isConnected = true;
            this._connectionStatus = $"Connected to {this.GetConnectionDescription()}";

            this._logger.LogInformation("KNX monitoring started successfully");
        }
        catch (Exception ex)
        {
            this._isConnected = false;
            this._connectionStatus = $"Connection failed: {ex.Message}";
            this._logger.LogError(ex, "Failed to start KNX monitoring");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task StopMonitoringAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (this._knxBus != null)
            {
                this._connectionStatus = "Disconnecting...";

                // Unsubscribe from events
                this._knxBus.GroupMessageReceived -= this.OnGroupMessageReceived;

                // Dispose the bus (this will disconnect)
                await this._knxBus.DisposeAsync();

                this._isConnected = false;
                this._connectionStatus = "Disconnected";

                this._logger.LogInformation("KNX monitoring stopped");
            }
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error stopping KNX monitoring");
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        try
        {
            await this.StopMonitoringAsync();
            this.Dispose();
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error during async dispose");
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        try
        {
            // Stop monitoring synchronously if not already stopped
            if (this._isConnected)
            {
                this.StopMonitoringAsync().GetAwaiter().GetResult();
            }
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error disposing KNX monitor service");
        }
        finally
        {
            // Dispose the bus if it exists
            this._knxBus?.Dispose();
        }
    }

    /// <summary>
    /// Creates connector parameters based on configuration.
    /// </summary>
    /// <returns>Connector parameters or null if creation failed.</returns>
    private ConnectorParameters? CreateConnectorParameters()
    {
        try
        {
            return this._config.ConnectionType switch
            {
                KnxConnectionType.Tunnel => this.CreateTunnelingParameters(),
                KnxConnectionType.Router => this.CreateRoutingParameters(),
                KnxConnectionType.Usb => this.CreateUsbParameters(),
                _ => throw new ArgumentOutOfRangeException(nameof(this._config.ConnectionType)),
            };
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Failed to create connector parameters");
            return null;
        }
    }

    /// <summary>
    /// Creates IP tunneling connector parameters.
    /// </summary>
    /// <returns>Tunneling connector parameters.</returns>
    private ConnectorParameters CreateTunnelingParameters()
    {
        if (string.IsNullOrEmpty(this._config.Gateway))
        {
            throw new InvalidOperationException("Gateway address is required for tunneling connection");
        }

        this._logger.LogDebug(
            "Creating IP tunneling connection to {Gateway}:{Port}",
            this._config.Gateway,
            this._config.Port
        );
        return new IpTunnelingConnectorParameters(this._config.Gateway, this._config.Port);
    }

    /// <summary>
    /// Creates IP routing connector parameters.
    /// </summary>
    /// <returns>Routing connector parameters.</returns>
    private ConnectorParameters CreateRoutingParameters()
    {
        var multicastAddress = this._config.MulticastAddress;

        // Try to parse as IP address first
        if (IPAddress.TryParse(multicastAddress, out var ipAddress))
        {
            this._logger.LogDebug(
                "Creating IP routing connection to multicast address {MulticastAddress}",
                multicastAddress
            );
            return new IpRoutingConnectorParameters(ipAddress);
        }

        // Resolve hostname to IP address if needed
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

            this._logger.LogDebug(
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

    /// <summary>
    /// Creates USB connector parameters.
    /// </summary>
    /// <returns>USB connector parameters.</returns>
    private ConnectorParameters CreateUsbParameters()
    {
        var usbDevices = KnxBus.GetAttachedUsbDevices().ToArray();
        if (usbDevices.Length == 0)
        {
            throw new InvalidOperationException("No KNX USB devices found");
        }

        var device = usbDevices[0];
        this._logger.LogDebug("Creating USB connection to device: {Device}", device);
        return UsbConnectorParameters.FromDiscovery(device);
    }

    /// <summary>
    /// Gets a description of the current connection.
    /// </summary>
    /// <returns>Connection description.</returns>
    private string GetConnectionDescription()
    {
        return this._config.ConnectionType switch
        {
            KnxConnectionType.Tunnel => $"{this._config.Gateway}:{this._config.Port} (IP Tunneling)",
            KnxConnectionType.Router => $"{this._config.MulticastAddress}:{this._config.Port} (IP Routing)",
            KnxConnectionType.Usb => "USB Device",
            _ => "Unknown",
        };
    }

    /// <summary>
    /// Handles group message received events.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Event arguments.</param>
    private void OnGroupMessageReceived(object? sender, GroupEventArgs e)
    {
        var messageType = DetermineMessageType(e);
        var message = CreateKnxMessage(e, messageType);
        this.ProcessMessage(message);
    }

    /// <summary>
    /// Determines the message type from the group message event.
    /// </summary>
    /// <param name="e">Event arguments.</param>
    /// <returns>Message type.</returns>
    private static KnxMessageType DetermineMessageType(GroupEventArgs e)
    {
        // Check the message type based on the event properties
        if (e.Value != null)
        {
            return KnxMessageType.Write;
        }

        // For read requests, the value is typically null
        return KnxMessageType.Read;
    }

    /// <summary>
    /// Creates a KNX message from group message event arguments.
    /// </summary>
    /// <param name="e">Event arguments.</param>
    /// <param name="messageType">Message type.</param>
    /// <returns>KNX message.</returns>
    private static KnxMessage CreateKnxMessage(GroupEventArgs e, KnxMessageType messageType)
    {
        // Extract data and DPT information from the Falcon SDK value
        var (data, dptId, falconValue) = ExtractValueInformation(e.Value);

        // Debug logging for development
        if (e.Value != null)
        {
            _staticLogger?.LogDebug(
                "KNX Value - Type: {ValueType}, DPT: {DptId}, Data: {Data}",
                e.Value.GetType().Name,
                dptId ?? "Unknown",
                Convert.ToHexString(data)
            );
        }

        return new KnxMessage
        {
            Timestamp = DateTime.Now,
            SourceAddress = e.SourceAddress.ToString(),
            GroupAddress = e.DestinationAddress.ToString(),
            MessageType = messageType,
            Data = data,
            Value = falconValue, // Store the original Falcon SDK decoded value
            DataPointType = dptId, // Store the detected DPT ID
            Priority = KnxPriority.Normal, // Default priority since it's not available in the event
            IsRepeated = false, // Default since it's not available in the event
        };
    }

    /// <summary>
    /// Extracts value information from a Falcon SDK value object.
    /// </summary>
    /// <param name="value">Falcon SDK value object.</param>
    /// <returns>Tuple containing raw data, DPT ID, and the original Falcon value.</returns>
    private static (byte[] Data, string? DptId, object? FalconValue) ExtractValueInformation(object? value)
    {
        if (value == null)
        {
            return (Array.Empty<byte>(), null, null);
        }

        try
        {
            // Check if it's a GroupValue from Falcon SDK
            if (value.GetType().Name == "GroupValue" || value.GetType().Namespace?.StartsWith("Knx.Falcon") == true)
            {
                // Try to extract DPT information using reflection
                var dptId = TryGetDptFromGroupValue(value);
                var data = TryGetDataFromGroupValue(value);
                var decodedValue = TryGetDecodedValueFromGroupValue(value);

                return (data, dptId, decodedValue ?? value);
            }

            // Handle primitive .NET types that Falcon SDK might return directly
            var (primitiveData, detectedDpt) = HandlePrimitiveValue(value);
            return (primitiveData, detectedDpt, value);
        }
        catch (Exception ex)
        {
            _staticLogger?.LogWarning(ex, "Error extracting value information from {ValueType}", value.GetType().Name);

            // Fallback to legacy extraction
            var fallbackData = ExtractDataFromValue(value);
            return (fallbackData, null, value);
        }
    }

    /// <summary>
    /// Tries to extract DPT ID from a Falcon SDK GroupValue object using reflection.
    /// </summary>
    /// <param name="groupValue">GroupValue object.</param>
    /// <returns>DPT ID or null if not found.</returns>
    private static string? TryGetDptFromGroupValue(object groupValue)
    {
        try
        {
            var type = groupValue.GetType();

            // Look for DPT-related properties
            var dptProperty = type.GetProperty("Dpt") ?? type.GetProperty("DptId") ?? type.GetProperty("DataPointType");

            if (dptProperty != null)
            {
                var dptValue = dptProperty.GetValue(groupValue);
                return dptValue?.ToString();
            }

            // Look for type information in the type name itself
            var typeName = type.Name;
            if (typeName.Contains("Dpt") && typeName.Length > 3)
            {
                // Try to extract DPT from type name like "Dpt1Value" or "Dpt9Value"
                var match = System.Text.RegularExpressions.Regex.Match(typeName, @"Dpt(\d+)");
                if (match.Success)
                {
                    return $"{match.Groups[1].Value}.001"; // Default to .001 subtype
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
    /// Tries to extract raw data bytes from a Falcon SDK GroupValue object.
    /// </summary>
    /// <param name="groupValue">GroupValue object.</param>
    /// <returns>Raw data bytes.</returns>
    private static byte[] TryGetDataFromGroupValue(object groupValue)
    {
        try
        {
            var type = groupValue.GetType();

            // Look for data-related properties
            var dataProperty = type.GetProperty("Data") ?? type.GetProperty("RawData") ?? type.GetProperty("Bytes");

            if (dataProperty != null)
            {
                var dataValue = dataProperty.GetValue(groupValue);
                if (dataValue is byte[] bytes)
                {
                    return bytes;
                }
            }

            // Look for ToByteArray method
            var toByteArrayMethod = type.GetMethod("ToByteArray") ?? type.GetMethod("GetBytes");

            if (toByteArrayMethod != null)
            {
                var result = toByteArrayMethod.Invoke(groupValue, null);
                if (result is byte[] methodBytes)
                {
                    return methodBytes;
                }
            }

            // Fallback to legacy extraction
            return ExtractDataFromValue(groupValue);
        }
        catch
        {
            return ExtractDataFromValue(groupValue);
        }
    }

    /// <summary>
    /// Tries to extract the decoded value from a Falcon SDK GroupValue object.
    /// </summary>
    /// <param name="groupValue">GroupValue object.</param>
    /// <returns>Decoded value or null.</returns>
    private static object? TryGetDecodedValueFromGroupValue(object groupValue)
    {
        try
        {
            var type = groupValue.GetType();

            // Look for value-related properties
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

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Handles primitive .NET values that might come directly from Falcon SDK.
    /// </summary>
    /// <param name="value">Primitive value.</param>
    /// <returns>Tuple containing data bytes and detected DPT.</returns>
    private static (byte[] Data, string? DptId) HandlePrimitiveValue(object value)
    {
        return value switch
        {
            bool boolValue => (new byte[] { (byte)(boolValue ? 1 : 0) }, "1.001"),
            byte byteValue => (new byte[] { byteValue }, "5.001"),
            sbyte sbyteValue => (new byte[] { (byte)sbyteValue }, "6.001"),
            short shortValue => (BitConverter.GetBytes(shortValue).Reverse().ToArray(), "8.001"),
            ushort ushortValue => (BitConverter.GetBytes(ushortValue).Reverse().ToArray(), "7.001"),
            int intValue => (BitConverter.GetBytes(intValue).Reverse().ToArray(), "13.001"),
            uint uintValue => (BitConverter.GetBytes(uintValue).Reverse().ToArray(), "12.001"),
            float floatValue => (BitConverter.GetBytes(floatValue).Reverse().ToArray(), "14.000"),
            double doubleValue => (BitConverter.GetBytes((float)doubleValue).Reverse().ToArray(), "14.000"),
            _ => (ExtractDataFromValue(value), null),
        };
    }

    /// <summary>
    /// Checks if a string is a valid hex string.
    /// </summary>
    private static bool IsHexString(string str)
    {
        if (string.IsNullOrEmpty(str) || str.Length % 2 != 0)
            return false;

        return str.All(c => char.IsDigit(c) || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f'));
    }

    /// <summary>
    /// Parses a hex string into byte array.
    /// </summary>
    private static byte[] ParseHexString(string hexString)
    {
        try
        {
            return Convert.FromHexString(hexString);
        }
        catch
        {
            return Array.Empty<byte>();
        }
    }

    /// <summary>
    /// Converts unknown value types to bytes.
    /// </summary>
    private static byte[] ConvertToBytes(object value)
    {
        var stringValue = value.ToString() ?? "";

        // Try to parse as hex string first
        if (IsHexString(stringValue))
        {
            return ParseHexString(stringValue);
        }

        // Try to parse as integer
        if (int.TryParse(stringValue, out var intValue))
        {
            if (intValue >= 0 && intValue <= 255)
                return new byte[] { (byte)intValue };
            if (intValue >= 0 && intValue <= 65535)
                return BitConverter.GetBytes((ushort)intValue).Reverse().ToArray(); // Big-endian
            return BitConverter.GetBytes(intValue).Reverse().ToArray(); // Big-endian
        }

        // Fallback to UTF8 encoding
        return System.Text.Encoding.UTF8.GetBytes(stringValue);
    }

    /// <summary>
    /// Extracts byte data from a KNX value object (legacy fallback method).
    /// </summary>
    /// <param name="value">KNX value object.</param>
    /// <returns>Byte array representation.</returns>
    private static byte[] ExtractDataFromValue(object? value)
    {
        if (value == null)
        {
            return Array.Empty<byte>();
        }

        try
        {
            // Handle different value types
            return value switch
            {
                bool boolValue => new byte[] { (byte)(boolValue ? 1 : 0) },
                byte byteValue => new byte[] { byteValue },
                int intValue when intValue >= 0 && intValue <= 255 => new byte[] { (byte)intValue },
                byte[] byteArray => byteArray,
                string hexString when IsHexString(hexString) => ParseHexString(hexString),
                _ => ConvertToBytes(value),
            };
        }
        catch
        {
            return Array.Empty<byte>();
        }
    }

    /// <summary>
    /// Processes a received message.
    /// </summary>
    /// <param name="message">Message to process.</param>
    private void ProcessMessage(KnxMessage message)
    {
        // Apply filter if configured
        if (this._filterRegex != null && !this._filterRegex.IsMatch(message.GroupAddress))
        {
            return;
        }

        // Log message if verbose
        if (this._config.Verbose)
        {
            this._logger.LogDebug(
                "KNX message: {MessageType} {GroupAddress} = {Value}",
                message.MessageType,
                message.GroupAddress,
                message.DisplayValue
            );
        }

        // Increment message counter
        Interlocked.Increment(ref this._messageCount);

        // Raise event
        this.MessageReceived?.Invoke(this, message);
    }
}
