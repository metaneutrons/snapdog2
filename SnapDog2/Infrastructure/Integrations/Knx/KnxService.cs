//
// SnapDog
// The Snapcast-based Smart Home Audio System with MQTT & KNX integration
// Copyright (C) 2025 Fabian Schmieder
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <https://www.gnu.org/licenses/>.
//
using System.Net;
using Knx.Falcon;
using Knx.Falcon.Configuration;
using Knx.Falcon.Sdk;
using Microsoft.Extensions.Options;
using SnapDog2.Domain.Abstractions;
using SnapDog2.Shared.Configuration;
using SnapDog2.Shared.Enums;
using SnapDog2.Shared.Models;

namespace SnapDog2.Infrastructure.Integrations.Knx;

/// <summary>
/// KNX integration service using Falcon SDK for real KNX bus communication.
/// </summary>
public partial class KnxService : IKnxService
{
    private readonly ILogger<KnxService> _logger;
    private readonly SnapDogConfiguration _configuration;
    private readonly IClientService _clientService;
    private KnxBus? _knxBus;
    private bool _isConnected;
    private bool _disposed;

    public KnxService(
        ILogger<KnxService> logger,
        IOptions<SnapDogConfiguration> configuration,
        IClientService clientService)
    {
        _logger = logger;
        _configuration = configuration.Value;
        _clientService = clientService;
    }

    public bool IsConnected => _isConnected;

    public ServiceStatus Status => IsConnected ? ServiceStatus.Running : ServiceStatus.Stopped;

    public async Task<Result> InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_configuration.Services.Knx.Enabled)
            {
                LogKnxDisabled();
                return Result.Success();
            }

            var connectorParameters = CreateConnectorParameters();
            if (connectorParameters == null)
            {
                return Result.Failure("Failed to create KNX connector parameters");
            }

            _knxBus = new KnxBus(connectorParameters);
            _knxBus.GroupMessageReceived += OnGroupMessageReceived;
            await _knxBus.ConnectAsync(cancellationToken);
            _isConnected = true;

            LogKnxConnected(_configuration.Services.Knx.Gateway ?? "localhost", _configuration.Services.Knx.Port, _configuration.Services.Knx.ConnectionType.ToString());
            return Result.Success();
        }
        catch (Exception ex)
        {
            LogKnxConnectionFailed(ex.Message);
            return Result.Failure($"Failed to initialize KNX connection: {ex.Message}");
        }
    }

    public async Task<Result> StopAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_knxBus != null)
            {
                await _knxBus.DisposeAsync();
                _isConnected = false;
            }
            LogKnxDisconnected();
            return Result.Success();
        }
        catch (Exception ex)
        {
            LogKnxDisconnectionFailed(ex.Message);
            return Result.Failure($"Failed to stop KNX connection: {ex.Message}");
        }
    }

    public async Task<Result> SendStatusAsync(string statusId, int targetId, object value, CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
        {
            return Result.Failure("KNX service is not connected");
        }

        try
        {
            var groupAddress = GetGroupAddressForStatus(statusId, targetId);
            if (groupAddress == null)
            {
                return Result.Failure($"No group address configured for status {statusId} target {targetId}");
            }

            var groupValue = value switch
            {
                bool b => new GroupValue(b),
                byte[] bytes => new GroupValue(bytes),
                _ => new GroupValue(Convert.ToBoolean(value))
            };
            await _knxBus!.WriteGroupValueAsync(GroupAddress.Parse(groupAddress), groupValue);
            LogKnxStatusSent(statusId, targetId, value?.ToString() ?? "null");
            return Result.Success();
        }
        catch (Exception ex)
        {
            LogKnxStatusSendFailed(statusId, targetId, ex.Message);
            return Result.Failure($"Failed to send KNX status: {ex.Message}");
        }
    }

    public async Task<Result> WriteGroupValueAsync(string groupAddress, object value, CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
        {
            return Result.Failure("KNX service is not connected");
        }

        try
        {
            var groupValue = value switch
            {
                bool b => new GroupValue(b),
                byte[] bytes => new GroupValue(bytes),
                _ => new GroupValue(Convert.ToBoolean(value))
            };
            await _knxBus!.WriteGroupValueAsync(GroupAddress.Parse(groupAddress), groupValue);
            LogKnxGroupValueWritten(groupAddress, value?.ToString() ?? "null");
            return Result.Success();
        }
        catch (Exception ex)
        {
            LogKnxGroupValueWriteFailed(groupAddress, ex.Message);
            return Result.Failure($"Failed to write KNX group value: {ex.Message}");
        }
    }

    public async Task<Result<object>> ReadGroupValueAsync(string groupAddress, CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
        {
            return Result<object>.Failure("KNX service is not connected");
        }

        try
        {
            var value = await _knxBus!.ReadGroupValueAsync(GroupAddress.Parse(groupAddress));
            LogKnxGroupValueRead(groupAddress);
            return Result<object>.Success(value);
        }
        catch (Exception ex)
        {
            LogKnxGroupValueReadFailed(groupAddress, ex.Message);
            return Result<object>.Failure($"Failed to read KNX group value: {ex.Message}");
        }
    }

    public Task<Result> PublishClientStatusAsync<T>(string clientIndex, string eventType, T payload, CancellationToken cancellationToken = default)
    {
        LogKnxClientStatusPublished(clientIndex, eventType);
        return Task.FromResult(Result.Success());
    }

    public Task<Result> PublishZoneStatusAsync<T>(int zoneIndex, string eventType, T payload, CancellationToken cancellationToken = default)
    {
        LogKnxZoneStatusPublished(zoneIndex, eventType);
        return Task.FromResult(Result.Success());
    }

    public Task<Result> PublishGlobalStatusAsync<T>(string eventType, T payload, CancellationToken cancellationToken = default)
    {
        LogKnxGlobalStatusPublished(eventType);
        return Task.FromResult(Result.Success());
    }

    private ConnectorParameters? CreateConnectorParameters()
    {
        return _configuration.Services.Knx.ConnectionType switch
        {
            KnxConnectionType.Tunnel => new IpTunnelingConnectorParameters(_configuration.Services.Knx.Gateway!, _configuration.Services.Knx.Port),
            KnxConnectionType.Router => new IpRoutingConnectorParameters(IPAddress.Parse(_configuration.Services.Knx.MulticastAddress)),
            KnxConnectionType.Usb => CreateUsbParameters(),
            _ => null
        };
    }

    private ConnectorParameters CreateUsbParameters()
    {
        var usbDevices = KnxBus.GetAttachedUsbDevices().ToArray();
        if (usbDevices.Length == 0)
        {
            throw new InvalidOperationException("No KNX USB devices found");
        }
        return UsbConnectorParameters.FromDiscovery(usbDevices[0]);
    }

    private void OnGroupMessageReceived(object? sender, GroupEventArgs e)
    {
        var groupAddress = e.DestinationAddress.ToString();
        var value = e.Value?.ToString() ?? "null";

        LogKnxGroupValueReceived(groupAddress, value);

        // TODO: Handle incoming KNX commands and route to appropriate services
    }

    private string? GetGroupAddressForStatus(string statusId, int targetId)
    {
        return statusId switch
        {
            "ZONE_PLAYBACK_STATUS" when targetId == 1 => "2/1/5",
            "CLIENT_VOLUME_STATUS" when targetId == 1 => "3/1/2",
            "CLIENT_MUTE_STATUS" when targetId == 1 => "3/1/6",
            "CLIENT_ZONE_STATUS" when targetId == 1 => "3/1/11",
            _ => null
        };
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        Dispose(false);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _knxBus?.Dispose();
            _disposed = true;
        }
    }

    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (_knxBus != null)
        {
            await _knxBus.DisposeAsync();
            _isConnected = false;
        }
    }

    [LoggerMessage(EventId = 15157, Level = LogLevel.Information, Message = "KNX service disabled in configuration")]
    private partial void LogKnxDisabled();

    [LoggerMessage(EventId = 15158, Level = LogLevel.Information, Message = "KNX connected to {Host}:{Port} using {ConnectionType}")]
    private partial void LogKnxConnected(string Host, int Port, string ConnectionType);

    [LoggerMessage(EventId = 15159, Level = LogLevel.Error, Message = "KNX connection failed: {Error}")]
    private partial void LogKnxConnectionFailed(string Error);

    [LoggerMessage(EventId = 15160, Level = LogLevel.Information, Message = "KNX disconnected")]
    private partial void LogKnxDisconnected();

    [LoggerMessage(EventId = 15161, Level = LogLevel.Error, Message = "KNX disconnection failed: {Error}")]
    private partial void LogKnxDisconnectionFailed(string Error);

    [LoggerMessage(EventId = 15162, Level = LogLevel.Debug, Message = "KNX group value received: {GroupAddress} = {Value}")]
    private partial void LogKnxGroupValueReceived(string GroupAddress, string Value);

    [LoggerMessage(EventId = 15163, Level = LogLevel.Debug, Message = "KNX status sent: {StatusId} for target {TargetId} with value {Value}")]
    private partial void LogKnxStatusSent(string StatusId, int TargetId, string Value);

    [LoggerMessage(EventId = 15164, Level = LogLevel.Error, Message = "KNX status send failed: {StatusId} for target {TargetId} - {Error}")]
    private partial void LogKnxStatusSendFailed(string StatusId, int TargetId, string Error);

    [LoggerMessage(EventId = 15165, Level = LogLevel.Debug, Message = "KNX group value written: {GroupAddress} = {Value}")]
    private partial void LogKnxGroupValueWritten(string GroupAddress, string Value);

    [LoggerMessage(EventId = 15166, Level = LogLevel.Error, Message = "KNX group value write failed: {GroupAddress} - {Error}")]
    private partial void LogKnxGroupValueWriteFailed(string GroupAddress, string Error);

    [LoggerMessage(EventId = 15167, Level = LogLevel.Debug, Message = "KNX group value read: {GroupAddress}")]
    private partial void LogKnxGroupValueRead(string GroupAddress);

    [LoggerMessage(EventId = 15168, Level = LogLevel.Error, Message = "KNX group value read failed: {GroupAddress} - {Error}")]
    private partial void LogKnxGroupValueReadFailed(string GroupAddress, string Error);

    [LoggerMessage(EventId = 15169, Level = LogLevel.Debug, Message = "KNX client status published: {ClientIndex} - {EventType}")]
    private partial void LogKnxClientStatusPublished(string ClientIndex, string EventType);

    [LoggerMessage(EventId = 15170, Level = LogLevel.Debug, Message = "KNX zone status published: {ZoneIndex} - {EventType}")]
    private partial void LogKnxZoneStatusPublished(int ZoneIndex, string EventType);

    [LoggerMessage(EventId = 15171, Level = LogLevel.Debug, Message = "KNX global status published: {EventType}")]
    private partial void LogKnxGlobalStatusPublished(string EventType);
}
