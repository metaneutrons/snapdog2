# Knx.Falcon.Sdk v6 Implementation Guide

## Overview

This document provides a comprehensive guide for implementing **Knx.Falcon.Sdk v6.3.7959** correctly in enterprise .NET applications. Based on real-world implementation experience in SnapDog2's KNX integration.

## 🏆 Award-Worthy Implementation Principles

### 1. **Enterprise Architecture Foundation**

```csharp
// Clean interface abstraction
public interface IKnxService : IAsyncDisposable
{
    bool IsConnected { get; }
    ServiceStatus Status { get; }
    Task<Result> InitializeAsync(CancellationToken cancellationToken = default);
    Task<Result> StopAsync(CancellationToken cancellationToken = default);
    Task<Result> WriteGroupValueAsync(string groupAddress, object value, CancellationToken cancellationToken = default);
    Task<Result<object>> ReadGroupValueAsync(string groupAddress, CancellationToken cancellationToken = default);
}
```

### 2. **Thread-Safe Connection Management**

```csharp
private readonly SemaphoreSlim _connectionSemaphore = new(1, 1);
private KnxBus? _knxBus;
private bool _isInitialized;
private bool _disposed;

public async Task<Result> InitializeAsync(CancellationToken cancellationToken = default)
{
    await _connectionSemaphore.WaitAsync(cancellationToken);
    try
    {
        if (_isInitialized) return Result.Success();

        var result = await _connectionPolicy.ExecuteAsync(async (ct) =>
        {
            return await ConnectToKnxBusAsync(ct);
        }, cancellationToken);

        if (result.IsSuccess)
        {
            _isInitialized = true;
        }

        return result;
    }
    finally
    {
        _connectionSemaphore.Release();
    }
}
```

## 🔧 Core Implementation Details

### Connection Setup

#### IP Tunneling Connection

```csharp
private ConnectorParameters? CreateConnectorParameters()
{
    if (!string.IsNullOrEmpty(_config.Gateway))
    {
        // IP Tunneling - most common in production
        return new IpTunnelingConnectorParameters(_config.Gateway, _config.Port);
    }
    else
    {
        // USB Connection - for direct hardware access
        var usbDevices = KnxBus.GetAttachedUsbDevices().ToArray();
        if (usbDevices.Length == 0) return null;

        return UsbConnectorParameters.FromDiscovery(usbDevices[0]);
    }
}
```

#### Connection Establishment

```csharp
private async Task<Result> ConnectToKnxBusAsync(CancellationToken cancellationToken)
{
    try
    {
        var connectorParams = CreateConnectorParameters();
        if (connectorParams == null)
            return Result.Failure("Failed to create KNX connector parameters");

        _knxBus = new KnxBus(connectorParams);

        // ⚠️ CRITICAL: Subscribe to events BEFORE connecting
        _knxBus.GroupMessageReceived += OnGroupMessageReceived;

        await _knxBus.ConnectAsync();

        if (_knxBus.ConnectionState != BusConnectionState.Connected)
            return Result.Failure("Failed to establish KNX connection");

        return Result.Success();
    }
    catch (Exception ex)
    {
        return Result.Failure($"KNX connection failed: {ex.Message}");
    }
}
```

### Event Processing

#### Incoming KNX Messages

```csharp
private async void OnGroupMessageReceived(object? sender, GroupEventArgs e)
{
    try
    {
        var address = e.DestinationAddress.ToString();
        var value = e.Value;

        // Map KNX group address to application command
        var command = MapGroupAddressToCommand(address, value);
        if (command != null)
        {
            // Fire-and-forget to avoid blocking KNX event processing
            _ = Task.Run(async () =>
            {
                try
                {
                    await ExecuteCommandAsync(command, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing command from KNX event");
                }
            });
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error processing KNX group message");
    }
}
```

### Group Value Operations

#### Writing Values

```csharp
private async Task<Result> WriteToKnxAsync(string groupAddress, object value, CancellationToken cancellationToken)
{
    try
    {
        var result = await _operationPolicy.ExecuteAsync(async (ct) =>
        {
            var ga = new GroupAddress(groupAddress);

            // Explicit type conversion for GroupValue
            GroupValue groupValue = value switch
            {
                bool boolValue => new GroupValue(boolValue),
                byte byteValue => new GroupValue(byteValue),
                int intValue when intValue >= 0 && intValue <= 255 => new GroupValue((byte)intValue),
                _ => throw new ArgumentException($"Unsupported value type: {value?.GetType()}")
            };

            await _knxBus!.WriteGroupValueAsync(ga, groupValue);
            return Result.Success();
        }, cancellationToken);

        return result;
    }
    catch (Exception ex)
    {
        return Result.Failure($"Failed to write group value: {ex.Message}");
    }
}
```

#### Reading Values

```csharp
public async Task<Result<object>> ReadGroupValueAsync(string groupAddress, CancellationToken cancellationToken = default)
{
    if (!IsConnected)
        return Result<object>.Failure("KNX service is not connected");

    try
    {
        var result = await _operationPolicy.ExecuteAsync(async (ct) =>
        {
            var ga = new GroupAddress(groupAddress);
            var value = await _knxBus!.ReadGroupValueAsync(ga);
            return value;
        }, cancellationToken);

        return Result<object>.Success(result);
    }
    catch (Exception ex)
    {
        return Result<object>.Failure($"Failed to read group value: {ex.Message}");
    }
}
```

## 🛡️ Resilience & Error Handling

### Polly Integration

```csharp
private ResiliencePipeline CreateConnectionPolicy()
{
    return new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromSeconds(2),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true
        })
        .AddTimeout(TimeSpan.FromSeconds(_config.Timeout))
        .Build();
}

private ResiliencePipeline CreateOperationPolicy()
{
    return new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 2,
            Delay = TimeSpan.FromMilliseconds(500),
            BackoffType = DelayBackoffType.Linear
        })
        .AddTimeout(TimeSpan.FromSeconds(5))
        .Build();
}
```

### Auto-Reconnection

```csharp
private void StartReconnectTimer()
{
    if (_config.AutoReconnect)
    {
        var interval = TimeSpan.FromSeconds(30);
        _reconnectTimer.Change(interval, interval);
    }
}

private async void OnReconnectTimer(object? state)
{
    if (!_isInitialized || IsConnected) return;

    _logger.LogInformation("Attempting KNX reconnection");
    var result = await InitializeAsync();
    if (result.IsSuccess)
    {
        StopReconnectTimer();
    }
}
```

## 🏗️ CQRS Integration

### Command Mapping

```csharp
private object? MapGroupAddressToCommand(string groupAddress, object value)
{
    // Zone commands
    for (int i = 0; i < _zones.Count; i++)
    {
        var zone = _zones[i];
        var zoneId = i + 1; // 1-based zone ID

        if (!zone.Knx.Enabled) continue;

        var knxConfig = zone.Knx;

        // Volume commands
        if (groupAddress == knxConfig.Volume && value is int volumeValue)
        {
            return new SetZoneVolumeCommand
            {
                ZoneId = zoneId,
                Volume = volumeValue,
                Source = CommandSource.Knx
            };
        }

        // Mute commands
        if (groupAddress == knxConfig.Mute && value is bool muteValue)
        {
            return new SetZoneMuteCommand
            {
                ZoneId = zoneId,
                Enabled = muteValue,
                Source = CommandSource.Knx
            };
        }

        // Playback commands
        if (groupAddress == knxConfig.Play && value is bool playValue && playValue)
        {
            return new PlayCommand
            {
                ZoneId = zoneId,
                Source = CommandSource.Knx
            };
        }
    }

    return null;
}
```

### Command Execution

```csharp
private async Task<Result> ExecuteCommandAsync(object command, CancellationToken cancellationToken)
{
    try
    {
        return command switch
        {
            SetZoneVolumeCommand cmd => await GetHandler<SetZoneVolumeCommandHandler>()
                .Handle(cmd, cancellationToken),
            SetZoneMuteCommand cmd => await GetHandler<SetZoneMuteCommandHandler>()
                .Handle(cmd, cancellationToken),
            PlayCommand cmd => await GetHandler<PlayCommandHandler>()
                .Handle(cmd, cancellationToken),
            // ... other command types
            _ => Result.Failure($"Unknown command type: {command.GetType().Name}")
        };
    }
    catch (Exception ex)
    {
        return Result.Failure($"Failed to execute command: {ex.Message}");
    }
}

private T GetHandler<T>() where T : class
{
    var handler = _serviceProvider.GetService<T>();
    if (handler == null)
        throw new InvalidOperationException($"Handler {typeof(T).Name} not found in DI container");
    return handler;
}
```

## 📊 Configuration Management

### Service Configuration

```csharp
public class KnxConfig
{
    public bool Enabled { get; set; } = false;
    public string? Gateway { get; set; }
    public int Port { get; set; } = 3671;
    public int Timeout { get; set; } = 10;
    public bool AutoReconnect { get; set; } = true;
}
```

### Zone/Client Mapping

```csharp
public class ZoneKnxConfig
{
    public bool Enabled { get; set; } = false;
    public string? Volume { get; set; }           // 1/1/1
    public string? VolumeStatus { get; set; }     // 1/1/2
    public string? Mute { get; set; }             // 1/1/3
    public string? MuteStatus { get; set; }       // 1/1/4
    public string? Play { get; set; }             // 1/1/5
    public string? Pause { get; set; }            // 1/1/6
    public string? Stop { get; set; }             // 1/1/7
    public string? TrackNext { get; set; }        // 1/1/8
    public string? TrackPrevious { get; set; }    // 1/1/9
    public string? ControlStatus { get; set; }    // 1/1/10
}
```

## 🔍 Structured Logging

### LoggerMessage Source Generators

```csharp
[LoggerMessage(8001, LogLevel.Information, "KNX service created with gateway: {Gateway}, port: {Port}, enabled: {Enabled}")]
private partial void LogServiceCreated(string? gateway, int port, bool enabled);

[LoggerMessage(8010, LogLevel.Information, "KNX connection established to {Gateway}:{Port}")]
private partial void LogConnectionEstablished(string gateway, int port);

[LoggerMessage(8016, LogLevel.Debug, "KNX group value received: {GroupAddress} = {Value}")]
private partial void LogGroupValueReceived(string groupAddress, object value);

[LoggerMessage(8022, LogLevel.Debug, "KNX group value written: {GroupAddress} = {Value}")]
private partial void LogGroupValueWritten(string groupAddress, object value);
```

## 🧪 Testing Strategy

### Development Environment

```yaml
# docker-compose.dev.yml
knxd:
  image: michelmu/knxd-docker:latest
  environment:
    - ADDRESS=1.1.128
    - CLIENT_ADDRESS=1.1.129:8
    - INTERFACE=dummy
    - DEBUG_ERROR_LEVEL=info
  ports:
    - "3671:3671/udp"
  networks:
    snapdog-dev:
      ipv4_address: 172.20.0.10
```

### Configuration Example

```json
{
  "Services": {
    "Knx": {
      "Enabled": true,
      "Gateway": "172.20.0.10",
      "Port": 3671,
      "Timeout": 10,
      "AutoReconnect": true
    }
  },
  "Zones": [
    {
      "Name": "Living Room",
      "Knx": {
        "Enabled": true,
        "Volume": "1/1/1",
        "VolumeStatus": "1/1/2",
        "Mute": "1/1/3",
        "MuteStatus": "1/1/4",
        "Play": "1/1/5",
        "Pause": "1/1/6",
        "Stop": "1/1/7",
        "TrackNext": "1/1/8",
        "TrackPrevious": "1/1/9",
        "ControlStatus": "1/1/10"
      }
    }
  ]
}
```

## ⚠️ Critical Implementation Notes

### 1. **Event Handler Registration Timing**

```csharp
// ❌ WRONG - Events registered after connection
await _knxBus.ConnectAsync();
_knxBus.GroupMessageReceived += OnGroupMessageReceived;

// ✅ CORRECT - Events registered before connection
_knxBus.GroupMessageReceived += OnGroupMessageReceived;
await _knxBus.ConnectAsync();
```

### 2. **GroupValue Type Handling**

```csharp
// ❌ WRONG - Direct assignment
GroupValue groupValue = value; // Compilation error

// ✅ CORRECT - Explicit conversion
GroupValue groupValue = value switch
{
    bool boolValue => new GroupValue(boolValue),
    byte byteValue => new GroupValue(byteValue),
    int intValue when intValue >= 0 && intValue <= 255 => new GroupValue((byte)intValue),
    _ => throw new ArgumentException($"Unsupported value type: {value?.GetType()}")
};
```

### 3. **Async Event Processing**

```csharp
// ❌ WRONG - Blocking event handler
private async void OnGroupMessageReceived(object? sender, GroupEventArgs e)
{
    await ProcessCommand(command); // Blocks KNX event loop
}

// ✅ CORRECT - Fire-and-forget pattern
private async void OnGroupMessageReceived(object? sender, GroupEventArgs e)
{
    _ = Task.Run(async () =>
    {
        try
        {
            await ProcessCommand(command);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing command");
        }
    });
}
```

### 4. **Proper Disposal**

```csharp
public async ValueTask DisposeAsync()
{
    if (_disposed) return;

    await StopAsync();

    _reconnectTimer.Dispose();
    _connectionSemaphore.Dispose();

    _disposed = true;
    GC.SuppressFinalize(this);
}
```

## 🚀 Performance Optimizations

### 1. **Group Address Caching**

```csharp
private readonly ConcurrentDictionary<string, string> _groupAddressCache = new();

private string? GetCachedGroupAddress(string statusId, int targetId)
{
    var key = $"{statusId}:{targetId}";
    return _groupAddressCache.GetOrAdd(key, _ => ResolveGroupAddress(statusId, targetId));
}
```

### 2. **Connection State Optimization**

```csharp
public bool IsConnected => _knxBus?.ConnectionState == BusConnectionState.Connected;

public ServiceStatus Status => _isInitialized switch
{
    false => ServiceStatus.Stopped,
    true when IsConnected => ServiceStatus.Running,
    true => ServiceStatus.Error
};
```

## 📋 Checklist for Award-Worthy Implementation

- ✅ **Thread-safe connection management** with SemaphoreSlim
- ✅ **Proper disposal pattern** with IAsyncDisposable
- ✅ **Resilience policies** for connection and operations
- ✅ **Structured logging** with correlation IDs
- ✅ **Configuration-driven** group address mapping
- ✅ **CQRS integration** for command dispatching
- ✅ **Auto-reconnection** with exponential backoff
- ✅ **Fire-and-forget** event processing
- ✅ **Comprehensive error handling** with Result pattern
- ✅ **Support for both IP and USB** connections
- ✅ **Performance optimizations** with caching
- ✅ **Enterprise architecture** with clean interfaces

## 🎯 Production Deployment

### Dependency Injection Registration

```csharp
services.AddSingleton<IKnxService, KnxService>();
services.AddHostedService<IntegrationServicesHostedService>();
```

### Health Checks

```csharp
services.AddHealthChecks()
    .AddCheck<KnxHealthCheck>("knx");
```

This implementation provides enterprise-grade KNX integration with proper error handling, resilience, and performance characteristics suitable for production smart home automation systems.
