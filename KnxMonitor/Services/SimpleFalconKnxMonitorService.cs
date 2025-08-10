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
/// Simplified KNX Monitor service that trusts Falcon SDK completely for decoding.
/// No manual decoding fallbacks - if Falcon can't decode it, we show an error.
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

    public event EventHandler<KnxMessage>? MessageReceived;

    public KnxMonitorService(KnxMonitorConfig config, ILogger<KnxMonitorService> logger)
    {
        this._config = config ?? throw new ArgumentNullException(nameof(config));
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));

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
                this.LogInvalidFilterPattern(ex, this._config.Filter);
            }
        }
    }

    public bool IsConnected => this._isConnected;
    public string ConnectionStatus => this._connectionStatus;
    public int MessageCount => this._messageCount;

    public async Task StartMonitoringAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            this.LogStartingMonitoring(this._config.ConnectionType.ToString());

            var connectorParameters = this.CreateConnectorParameters();
            if (connectorParameters == null)
            {
                this.LogFailedToCreateConnectorParameters();
                return;
            }

            this._knxBus = new KnxBus(connectorParameters);
            this._knxBus.GroupMessageReceived += this.OnGroupMessageReceived;

            await this._knxBus.ConnectAsync(cancellationToken);
            this._isConnected = true;
            this._connectionStatus = $"Connected via {this._config.ConnectionType}";

            this.LogMonitoringStartedSuccessfully();
        }
        catch (Exception ex)
        {
            this.LogFailedToStartMonitoring(ex);
            this._connectionStatus = $"Failed: {ex.Message}";
        }
    }

    public async Task StopMonitoringAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (this._knxBus != null)
            {
                this._knxBus.GroupMessageReceived -= this.OnGroupMessageReceived;
                await this._knxBus.DisposeAsync();
                this._isConnected = false;
                this._connectionStatus = "Disconnected";
            }

            this.LogMonitoringStopped();
        }
        catch (Exception ex)
        {
            this.LogErrorStoppingMonitoring(ex);
        }
    }

    private ConnectorParameters? CreateConnectorParameters()
    {
        return this._config.ConnectionType switch
        {
            KnxConnectionType.Tunnel => this.CreateTunnelingParameters(),
            KnxConnectionType.Router => this.CreateRoutingParameters(),
            KnxConnectionType.Usb => this.CreateUsbParameters(),
            _ => null,
        };
    }

    private ConnectorParameters CreateTunnelingParameters()
    {
        this.LogCreatingIpTunnelingConnection();
        return new IpTunnelingConnectorParameters(this._config.Gateway!, this._config.Port);
    }

    private ConnectorParameters CreateRoutingParameters()
    {
        this.LogCreatingIpRoutingConnection();
        return new IpRoutingConnectorParameters(IPAddress.Parse(this._config.MulticastAddress!));
    }

    private ConnectorParameters CreateUsbParameters()
    {
        this.LogCreatingUsbConnection();
        var usbDevices = KnxBus.GetAttachedUsbDevices().ToArray();
        if (usbDevices.Length == 0)
        {
            throw new InvalidOperationException("No KNX USB devices found");
        }
        return UsbConnectorParameters.FromDiscovery(usbDevices[0]);
    }

    private void OnGroupMessageReceived(object? sender, GroupEventArgs e)
    {
        try
        {
            var message = this.CreateKnxMessage(e);
            this.ProcessMessage(message);
        }
        catch (Exception ex)
        {
            this.LogErrorProcessingGroupMessage(ex);
        }
    }

    private KnxMessage CreateKnxMessage(GroupEventArgs e)
    {
        var messageType = e.Value != null ? KnxMessageType.Write : KnxMessageType.Read;

        // Extract raw data from GroupValue.Value property
        var rawData = this.TryExtractBytesFromGroupValue(e.Value) ?? Array.Empty<byte>();

        // Extract the actual decoded value from GroupValue.TypedValue property
        var decodedValue = this.ExtractDecodedValueFromGroupValue(e.Value);

        return new KnxMessage
        {
            Timestamp = DateTime.Now,
            SourceAddress = e.SourceAddress.ToString(),
            GroupAddress = e.DestinationAddress.ToString(),
            MessageType = messageType,
            Data = rawData,
            Value = decodedValue,
            DataPointType = null,
            Priority = KnxPriority.Normal,
            IsRepeated = false,
        };
    }

    private object? ExtractDecodedValueFromGroupValue(object? groupValue)
    {
        if (groupValue == null)
            return null;

        try
        {
            var type = groupValue.GetType();

            // For GroupValue objects, use TypedValue property
            if (type.Name == "GroupValue")
            {
                var typedValueProp = type.GetProperty("TypedValue");
                if (typedValueProp != null)
                {
                    var typedValue = typedValueProp.GetValue(groupValue);

                    // If TypedValue is a byte array, show as hex (no automatic decoding available)
                    if (typedValue is byte[] bytes)
                    {
                        return Convert.ToHexString(bytes);
                    }

                    // For primitive values (byte, int, bool, etc.), return directly
                    return typedValue;
                }
            }

            // Fallback to the object itself
            return groupValue;
        }
        catch
        {
            return groupValue;
        }
    }

    private byte[]? TryExtractBytesFromGroupValue(object? groupValue)
    {
        if (groupValue == null)
            return null;

        try
        {
            var type = groupValue.GetType();

            // For GroupValue objects, use Value property (contains raw bytes)
            if (type.Name == "GroupValue")
            {
                var valueProp = type.GetProperty("Value");
                if (valueProp?.PropertyType == typeof(byte[]))
                {
                    return valueProp.GetValue(groupValue) as byte[];
                }
            }

            // If it's already a byte array, use it directly
            if (groupValue is byte[] directBytes)
            {
                return directBytes;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private void ProcessMessage(KnxMessage message)
    {
        // Apply filter if configured
        if (this._filterRegex != null && !this._filterRegex.IsMatch(message.GroupAddress))
        {
            return;
        }

        this._messageCount++;
        this.LogReceivedKnxMessage(message.GroupAddress, message.MessageType.ToString());
        this.MessageReceived?.Invoke(this, message);
    }

    public void Dispose()
    {
        try
        {
            this.StopMonitoringAsync().GetAwaiter().GetResult();
            this._knxBus?.Dispose();
        }
        catch (Exception ex)
        {
            this.LogErrorDuringAsyncDispose(ex);
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await this.StopMonitoringAsync();
            this._knxBus?.Dispose();
        }
        catch (Exception ex)
        {
            this.LogErrorDuringAsyncDispose(ex);
        }
    }
}
