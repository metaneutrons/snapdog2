# SNAPDOG2 MEDIATOR REMOVAL - PHASE 4.2 IMPLEMENTATION PROMPT

## CURRENT PHASE: Phase 4.2 - Integration Coordinator Implementation
## CURRENT STEP: 4.2.1 - Replace StatePublishingService with Integration Coordinator
## LAST COMPLETED: Phase 4.1 - State Store Event-Driven Architecture
## NEXT OBJECTIVE: Centralized integration publishing from state events

## IMPLEMENTATION STATUS

- **Files Modified**: State Store interfaces (✓ events added)
- **Files Removed**: ~4,000+ lines (command/handler infrastructure)
- **Services Migrated**: ZoneManager (✓), StateStore (✓ events)
- **Tests Updated**: 0
- **Build Status**: PASS (0 errors, ≤30 warnings)

## CRITICAL PATTERNS ESTABLISHED

- **LoggerMessage**: Fully implemented across all services
- **Service Injection**: Direct service calls proven working
- **StateStore Events**: ✅ IMPLEMENTED - Single Source of Truth
- **Attribute Migration**: CommandId/StatusId preserved in constants

## CURRENT ARCHITECTURE STATE

```
API Controllers → Direct Service Calls
Domain Services → StateStore (read/write)
StateStore → Events (✅ IMPLEMENTED)
Integration Services → StateStore Events (MISSING - TO IMPLEMENT)
StatePublishingService → NEEDS REPLACEMENT with IntegrationCoordinator
```

## PHASE 4.2 OBJECTIVES

### 1. Integration Publisher Abstraction

**IIntegrationPublisher Interface**:
```csharp
// File: Domain/Abstractions/IIntegrationPublisher.cs
public interface IIntegrationPublisher
{
    string Name { get; }
    bool IsEnabled { get; }

    Task PublishZoneStateAsync(int zoneIndex, ZoneState state, CancellationToken cancellationToken = default);
    Task PublishClientStateAsync(int clientIndex, ClientState state, CancellationToken cancellationToken = default);

    // Granular publishing methods
    Task PublishZonePlaylistChangedAsync(int zoneIndex, PlaylistInfo? playlist, CancellationToken cancellationToken = default);
    Task PublishZoneVolumeChangedAsync(int zoneIndex, int volume, CancellationToken cancellationToken = default);
    Task PublishZoneTrackChangedAsync(int zoneIndex, TrackInfo? track, CancellationToken cancellationToken = default);
    Task PublishClientVolumeChangedAsync(int clientIndex, int volume, CancellationToken cancellationToken = default);
}
```

### 2. Integration Publisher Implementations

**MQTT Publisher**:
```csharp
// File: Infrastructure/Integrations/Mqtt/MqttIntegrationPublisher.cs
public class MqttIntegrationPublisher : IIntegrationPublisher
{
    public string Name => "MQTT";
    public bool IsEnabled => _mqttService.IsConnected;

    public async Task PublishZonePlaylistChangedAsync(int zoneIndex, PlaylistInfo? playlist, CancellationToken cancellationToken = default)
    {
        var topic = $"snapdog/zones/{zoneIndex}/playlist";
        var payload = playlist?.Index?.ToString() ?? "0";
        await _mqttService.PublishAsync(topic, payload, retain: true, cancellationToken);

        // Also publish playlist name
        var nameTopic = $"snapdog/zones/{zoneIndex}/playlist/name";
        var namePayload = playlist?.Name ?? "";
        await _mqttService.PublishAsync(nameTopic, namePayload, retain: true, cancellationToken);
    }
}
```

**KNX Publisher**:
```csharp
// File: Infrastructure/Integrations/Knx/KnxIntegrationPublisher.cs
public class KnxIntegrationPublisher : IIntegrationPublisher
{
    public string Name => "KNX";
    public bool IsEnabled => _knxService.IsConnected;

    public async Task PublishZonePlaylistChangedAsync(int zoneIndex, PlaylistInfo? playlist, CancellationToken cancellationToken = default)
    {
        var playlistIndex = playlist?.Index ?? 0;
        var knxValue = playlistIndex > 255 ? 0 : playlistIndex; // KNX DPT 5.010 limitation

        var groupAddress = _configuration.GetZonePlaylistStatusGA(zoneIndex);
        if (groupAddress != null)
        {
            await _knxService.WriteAsync(groupAddress, knxValue, cancellationToken);
        }
    }
}
```

**SignalR Publisher**:
```csharp
// File: Infrastructure/Integrations/SignalR/SignalRIntegrationPublisher.cs
public class SignalRIntegrationPublisher : IIntegrationPublisher
{
    public string Name => "SignalR";
    public bool IsEnabled => true; // Always enabled

    public async Task PublishZonePlaylistChangedAsync(int zoneIndex, PlaylistInfo? playlist, CancellationToken cancellationToken = default)
    {
        var notification = new { ZoneIndex = zoneIndex, Playlist = playlist };
        await _hubContext.Clients.All.SendAsync("ZonePlaylistChanged", notification, cancellationToken);
    }
}
```

### 3. Integration Coordinator

**Central Event Handler**:
```csharp
// File: Application/Services/IntegrationCoordinator.cs
public class IntegrationCoordinator : IHostedService
{
    private readonly IEnumerable<IIntegrationPublisher> _publishers;
    private readonly ILogger<IntegrationCoordinator> _logger;

    public IntegrationCoordinator(
        IZoneStateStore zoneStateStore,
        IClientStateStore clientStateStore,
        IEnumerable<IIntegrationPublisher> publishers,
        ILogger<IntegrationCoordinator> logger)
    {
        _publishers = publishers;
        _logger = logger;

        // Subscribe to state change events
        zoneStateStore.ZonePlaylistChanged += OnZonePlaylistChanged;
        zoneStateStore.ZoneVolumeChanged += OnZoneVolumeChanged;
        zoneStateStore.ZoneTrackChanged += OnZoneTrackChanged;
        
        clientStateStore.ClientVolumeChanged += OnClientVolumeChanged;
        clientStateStore.ClientConnectionChanged += OnClientConnectionChanged;
    }

    private async void OnZonePlaylistChanged(object? sender, ZonePlaylistChangedEventArgs e)
    {
        var tasks = _publishers
            .Where(p => p.IsEnabled)
            .Select(p => PublishWithErrorHandling(
                () => p.PublishZonePlaylistChangedAsync(e.ZoneIndex, e.NewPlaylist),
                p.Name,
                "ZonePlaylistChanged"));

        await Task.WhenAll(tasks);
    }

    private async Task PublishWithErrorHandling(Func<Task> publishAction, string publisherName, string eventType)
    {
        try
        {
            await publishAction();
            LogPublishSuccess(eventType, publisherName);
        }
        catch (Exception ex)
        {
            LogPublishError(ex, eventType, publisherName);
            // Don't rethrow - one integration failure shouldn't affect others
        }
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Successfully published {EventType} to {Publisher}")]
    private partial void LogPublishSuccess(string eventType, string publisher);

    [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "Failed to publish {EventType} to {Publisher}")]
    private partial void LogPublishError(Exception exception, string eventType, string publisher);
}
```

### 4. Replace StatePublishingService

**Remove Old Service**:
```csharp
// File: Application/Services/StatePublishingService.cs - DELETE ENTIRE FILE
// This service is replaced by IntegrationCoordinator
```

**Update Dependency Injection**:
```csharp
// File: Program.cs
// Remove old service
// builder.Services.AddHostedService<StatePublishingService>(); // REMOVE

// Add new integration system
builder.Services.AddSingleton<IIntegrationPublisher, MqttIntegrationPublisher>();
builder.Services.AddSingleton<IIntegrationPublisher, KnxIntegrationPublisher>();
builder.Services.AddSingleton<IIntegrationPublisher, SignalRIntegrationPublisher>();
builder.Services.AddHostedService<IntegrationCoordinator>();
```

## IMPLEMENTATION STEPS

### Step 1: Create Integration Publisher Interface

1. Create `Domain/Abstractions/IIntegrationPublisher.cs`
2. Define all required publishing methods
3. Include Name and IsEnabled properties

### Step 2: Implement Integration Publishers

1. Create `MqttIntegrationPublisher.cs`
2. Create `KnxIntegrationPublisher.cs`
3. Create `SignalRIntegrationPublisher.cs`
4. Each publisher handles protocol-specific formatting

### Step 3: Create Integration Coordinator

1. Create `Application/Services/IntegrationCoordinator.cs`
2. Subscribe to all state store events
3. Implement error handling and logging
4. Use LoggerMessage pattern for performance

### Step 4: Update Dependency Injection

1. Remove StatePublishingService registration
2. Add all IIntegrationPublisher implementations
3. Add IntegrationCoordinator as hosted service

### Step 5: Remove Old Service

1. Delete `StatePublishingService.cs`
2. Remove any references to old service
3. Clean up unused using statements

## RISK MITIGATION

- **Integration Failures**: Error handling prevents cascade failures
- **Performance**: Parallel publishing with Task.WhenAll
- **Memory Leaks**: Proper event subscription/unsubscription
- **Startup Issues**: Graceful handling of disabled integrations

## SUCCESS CRITERIA

- [ ] All integration publishers implement common interface
- [ ] IntegrationCoordinator subscribes to all state events
- [ ] Error handling prevents integration cascade failures
- [ ] StatePublishingService completely removed
- [ ] Build: 0 errors, ≤30 warnings
- [ ] All integrations receive state changes simultaneously
- [ ] Performance metrics for each integration
- [ ] LoggerMessage pattern used for high-performance logging

## VALIDATION COMMANDS

```bash
# Build verification
dotnet build SnapDog2/SnapDog2.csproj --verbosity quiet

# Verify integration publishers
find SnapDog2/Infrastructure/Integrations -name "*IntegrationPublisher.cs"

# Check IntegrationCoordinator
grep -r "IntegrationCoordinator" SnapDog2/Application/Services/

# Verify old service removal
find SnapDog2 -name "*StatePublishingService*"
```

## NEXT PHASE PREPARATION

After Phase 4.2 completion:
- **Phase 4.3**: Remove Direct Integration Calls from Services
- **Phase 4.4**: Wire All Services to Use StateStore Events Only
- **Phase 5**: Service-by-Service Direct Call Migration

## ARCHITECTURE ACHIEVEMENT

This phase establishes the **Integration Coordinator Pattern**:

```
Before: Services → Direct Integration Calls (MQTT, KNX, SignalR)
After:  Services → StateStore → Events → IntegrationCoordinator → All Integrations
```

**Benefits**:
- **Reliability**: All integrations guaranteed to receive state changes
- **Error Isolation**: One integration failure doesn't affect others
- **Consistency**: Same state data sent to all integrations
- **Monitoring**: Centralized logging and metrics for all integrations
- **Scalability**: Easy to add new integration types
