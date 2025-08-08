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

    /// <summary>
    /// Initializes a new instance of the <see cref="KnxMonitorService"/> class.
    /// </summary>
    /// <param name="config">Monitor configuration.</param>
    /// <param name="logger">Logger instance.</param>
    public KnxMonitorService(KnxMonitorConfig config, ILogger<KnxMonitorService> logger)
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
    public async Task StartMonitoringAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _connectionStatus = "Connecting...";
            _logger.LogInformation("Starting KNX monitoring with {ConnectionType} connection", _config.ConnectionType);

            // Create connector parameters
            var connectorParameters = CreateConnectorParameters();
            if (connectorParameters == null)
            {
                _connectionStatus = "Failed to create connector parameters";
                return;
            }

            // Create and configure KNX bus
            _knxBus = new KnxBus(connectorParameters);

            // Subscribe to events
            _knxBus.GroupMessageReceived += OnGroupMessageReceived;

            // Connect to bus with timeout support
            await _knxBus.ConnectAsync(cancellationToken);

            _isConnected = true;
            _connectionStatus = $"Connected to {GetConnectionDescription()}";

            _logger.LogInformation("KNX monitoring started successfully");
        }
        catch (Exception ex)
        {
            _isConnected = false;
            _connectionStatus = $"Connection failed: {ex.Message}";
            _logger.LogError(ex, "Failed to start KNX monitoring");
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
                _knxBus.GroupMessageReceived -= OnGroupMessageReceived;

                // Dispose the bus (this will disconnect)
                await _knxBus.DisposeAsync();

                _isConnected = false;
                _connectionStatus = "Disconnected";

                _logger.LogInformation("KNX monitoring stopped");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping KNX monitoring");
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
            // Stop monitoring synchronously if not already stopped
            if (_isConnected)
            {
                StopMonitoringAsync().GetAwaiter().GetResult();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing KNX monitor service");
        }
        finally
        {
            // Dispose the bus if it exists
            _knxBus?.Dispose();
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

    /// <summary>
    /// Creates IP tunneling connector parameters.
    /// </summary>
    /// <returns>Tunneling connector parameters.</returns>
    private ConnectorParameters CreateTunnelingParameters()
    {
        if (string.IsNullOrEmpty(_config.Gateway))
        {
            throw new InvalidOperationException("Gateway address is required for tunneling connection");
        }

        _logger.LogDebug("Creating IP tunneling connection to {Gateway}:{Port}", _config.Gateway, _config.Port);
        return new IpTunnelingConnectorParameters(_config.Gateway, _config.Port);
    }

    /// <summary>
    /// Creates IP routing connector parameters.
    /// </summary>
    /// <returns>Routing connector parameters.</returns>
    private ConnectorParameters CreateRoutingParameters()
    {
        var multicastAddress = _config.MulticastAddress;

        // Try to parse as IP address first
        if (IPAddress.TryParse(multicastAddress, out var ipAddress))
        {
            _logger.LogDebug(
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
        _logger.LogDebug("Creating USB connection to device: {Device}", device);
        return UsbConnectorParameters.FromDiscovery(device);
    }

    /// <summary>
    /// Gets a description of the current connection.
    /// </summary>
    /// <returns>Connection description.</returns>
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

    /// <summary>
    /// Handles group message received events.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Event arguments.</param>
    private void OnGroupMessageReceived(object? sender, GroupEventArgs e)
    {
        var messageType = DetermineMessageType(e);
        var message = CreateKnxMessage(e, messageType);
        ProcessMessage(message);
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
        // Extract data from the value object
        var data = ExtractDataFromValue(e.Value);

        return new KnxMessage
        {
            Timestamp = DateTime.Now,
            SourceAddress = e.SourceAddress.ToString(),
            GroupAddress = e.DestinationAddress.ToString(),
            MessageType = messageType,
            Data = data,
            Value = TryInterpretValue(data),
            Priority = KnxPriority.Normal, // Default priority since it's not available in the event
            IsRepeated = false, // Default since it's not available in the event
        };
    }

    /// <summary>
    /// Extracts byte data from a KNX value object.
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
                _ => System.Text.Encoding.UTF8.GetBytes(value.ToString() ?? ""),
            };
        }
        catch
        {
            return Array.Empty<byte>();
        }
    }

    /// <summary>
    /// Tries to interpret the raw data as a meaningful value.
    /// </summary>
    /// <param name="data">Raw data.</param>
    /// <returns>Interpreted value or null.</returns>
    private static object? TryInterpretValue(byte[]? data)
    {
        if (data == null || data.Length == 0)
        {
            return null;
        }

        try
        {
            // Common KNX data types
            return data.Length switch
            {
                1 when data[0] <= 1 => data[0] == 1, // Boolean (DPT 1.001)
                1 => data[0], // 8-bit value (DPT 5.001)
                2 => BitConverter.ToUInt16(data.Reverse().ToArray(), 0), // 16-bit value
                4 => BitConverter.ToSingle(data.Reverse().ToArray(), 0), // Float (DPT 9.001)
                _ => null,
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Processes a received message.
    /// </summary>
    /// <param name="message">Message to process.</param>
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
                "KNX message: {MessageType} {GroupAddress} = {Value}",
                message.MessageType,
                message.GroupAddress,
                message.DisplayValue
            );
        }

        // Raise event
        MessageReceived?.Invoke(this, message);
    }
}
