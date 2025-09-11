# SnapDog2 TestKit - Integration Tests

Comprehensive real-world integration testing for SnapDog2's MQTT and KNX integrations.

## Features

- **MQTT Integration Tests**: Real MQTT broker communication testing
- **KNX Integration Tests**: Live KNX gateway testing using Falcon SDK
- **Scenario Tests**: End-to-end API testing scenarios
- **Smart Home Scenarios**: Realistic automation testing

## Quick Start

```bash
# Run all tests (default)
dotnet run

# Run specific test types
dotnet run -- --tests mqtt
dotnet run -- --tests knx
dotnet run -- --tests health
dotnet run -- --tests scenarios

# Run multiple test types
dotnet run -- --tests mqtt,knx
dotnet run -- --tests health,mqtt,knx
```

## Configuration Options

### API Configuration
```bash
--api-url <url>         # API base URL (default: http://localhost:8000/api)
```

### MQTT Configuration
```bash
--mqtt-url <url>        # MQTT broker URL (default: mqtt://localhost:1883)
```

### KNX Configuration
```bash
--knx-host <host>       # KNX gateway host (default: localhost)
--knx-port <port>       # KNX gateway port (default: 3671)
--knx-connection <type> # Connection type: Tunnel, Router, Usb (default: Tunnel)
```

## Examples

### Local Development Testing
```bash
# Test all components
dotnet run

# Test specific integrations
dotnet run -- --tests mqtt,knx

# Test with custom MQTT broker
dotnet run -- --tests mqtt --mqtt-url mqtt://192.168.1.100:1883
```

### Production Environment Testing
```bash
# Test against production KNX gateway
dotnet run -- --tests knx --knx-host 192.168.1.50 --knx-connection Tunnel

# Test with external MQTT broker
dotnet run -- --tests mqtt --mqtt-url mqtt://broker.example.com:1883

# Health check only
dotnet run -- --tests health
```

### KNX Connection Types

#### IP Tunneling (Default)
```bash
dotnet run -- --knx-connection Tunnel --knx-host 192.168.1.100
```
- Most common connection type
- Connects via UDP tunneling to KNX/IP gateway
- Requires gateway IP address

#### IP Routing
```bash
dotnet run -- --knx-connection Router
```
- Direct access to KNX backbone
- Uses UDP multicast
- No gateway required

#### USB Connection
```bash
dotnet run -- --knx-connection Usb
```
- Direct hardware connection
- Requires KNX USB interface
- Host parameter ignored

## Test Categories

### MQTT Integration Tests
- ✅ Zone playback state notifications
- ✅ Zone track changed notifications  
- ✅ Client volume notifications
- ✅ Client mute notifications
- ✅ MQTT command handling

### KNX Integration Tests
- ✅ Zone playback state KNX telegrams
- ✅ Zone track changed KNX telegrams
- ✅ Client volume KNX telegrams
- ✅ Client mute KNX telegrams
- ✅ KNX command handling

### Smart Home Scenarios
- 🌅 Morning routine automation
- 🌙 Evening routine automation
- 🏠 Multi-room audio coordination
- 🔊 Volume control integration

## Expected Group Addresses

The tests expect the following KNX group addresses (from devcontainer config):

### Zone 1 Addresses
- `2/1/4` - Zone pause command
- `2/1/5` - Zone playback status
- `2/1/10` - Track title
- `2/1/11` - Track artist
- `2/1/12` - Track album

### Client 1 Addresses
- `3/1/1` - Volume command
- `3/1/2` - Volume status
- `3/1/5` - Mute command
- `3/1/6` - Mute status

## Prerequisites

- SnapDog2 API running and accessible
- MQTT broker accessible (for MQTT tests)
- KNX gateway accessible (for KNX tests)
- Proper network connectivity

## Troubleshooting

### MQTT Connection Issues
```bash
# Test MQTT broker connectivity
mosquitto_pub -h localhost -p 1883 -t test -m "hello"
```

### KNX Connection Issues
```bash
# Verify KNX gateway is reachable
ping <knx-host>
telnet <knx-host> <knx-port>
```

### Common Issues
- **Timeout errors**: Check network connectivity and service availability
- **Group address errors**: Verify KNX configuration matches devcontainer setup
- **MQTT topic errors**: Ensure MQTT broker allows topic subscriptions
- **API errors**: Verify SnapDog2 API is running and accessible

## Development

### Adding New Tests

1. **MQTT Tests**: Extend `MqttIntegrationTestRunner.cs`
2. **KNX Tests**: Extend `KnxIntegrationTestRunner.cs`  
3. **Scenarios**: Add new scenario classes in `Scenarios/` folder

### Test Structure
```csharp
private async Task TestNewFeature()
{
    Console.WriteLine("🧪 Testing new feature...");
    
    // Setup
    _receivedMessages.Clear();
    
    // Action
    var response = await _httpClient.PostAsync($"{_apiBaseUrl}/v1/new-endpoint", null);
    response.EnsureSuccessStatusCode();
    
    // Verification
    await WaitForMessage("expected-topic", TimeSpan.FromSeconds(5));
    
    Console.WriteLine("✅ New feature test completed");
}
```
