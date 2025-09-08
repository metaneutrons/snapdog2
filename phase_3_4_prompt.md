# SNAPDOG2 MEDIATOR REMOVAL - PHASE 3.4 IMPLEMENTATION PROMPT

## CURRENT PHASE: Phase 3.4 - Integration Services Migration
## CURRENT STEP: 3.4.1 - IKnxService and IMqttService Direct Service Calls
## LAST COMPLETED: Phase 3.3 - Complete Domain Service Mediator Removal
## NEXT OBJECTIVE: Migrate integration services to direct service calls

## IMPLEMENTATION STATUS

- **Files Modified**: ZoneManager.cs, ClientManager.cs, PlaylistManager.cs (mediator removed)
- **Files Removed**: 0
- **Services Migrated**: ZoneManager (✓), ClientManager (✓), PlaylistManager (✓)
- **Tests Updated**: 0
- **Build Status**: PASS (0 errors, ≤20 warnings)

## CRITICAL PATTERNS ESTABLISHED

- **LoggerMessage**: Fully implemented across all services
- **Service Injection**: Direct service calls proven working in domain services
- **StateStore Events**: Not yet implemented
- **Attribute Migration**: CommandId/StatusId preserved in constants

## CURRENT ARCHITECTURE STATE

```
Domain Services → Direct Service Calls (✓ complete)
Integration Services → Cortex.Mediator (needs migration)
  ├── KnxService → mediator.SendCommandAsync()
  ├── MqttService → mediator.SendCommandAsync()
  └── GlobalStatusService → mediator.PublishAsync()
```

## PHASE 3.4 OBJECTIVES

### 1. Integration Services Analysis

**Current Integration Pattern**:
```csharp
// KnxService.cs - Current mediator usage
var result = await mediator.SendCommandAsync<SetZoneVolumeCommand, Result>(command);

// MqttService.cs - Current mediator usage  
var result = await mediator.SendCommandAsync<SetPlaylistCommand, Result>(command);
```

**Target Pattern**:
```csharp
// Direct service calls
var result = await _zoneService.SetVolumeAsync(zoneIndex, volume);
var result = await _zoneService.SetPlaylistAsync(zoneIndex, playlistIndex);
```

### 2. Service Dependency Mapping

**KnxService Required Dependencies**:
- `IZoneService` - For zone operations (volume, playlist, playback)
- `IClientService` - For client operations (volume, mute, zone assignment)
- Remove: `IMediator`, `IServiceScopeFactory`

**MqttService Required Dependencies**:
- `IZoneService` - For zone operations
- `IClientService` - For client operations  
- Remove: `IMediator`, `IServiceScopeFactory`

**GlobalStatusService Required Dependencies**:
- `IHubContext<SnapDogHub>` - For SignalR notifications
- Remove: `IMediator`, `IServiceScopeFactory`

### 3. Command to Service Method Mapping

**Zone Commands → IZoneService Methods**:
- `SetPlaylistCommand` → `SetPlaylistAsync(zoneIndex, playlistIndex)`
- `SetZoneVolumeCommand` → `SetVolumeAsync(zoneIndex, volume)`
- `PlayCommand` → `PlayAsync(zoneIndex)`
- `PauseCommand` → `PauseAsync(zoneIndex)`
- `NextTrackCommand` → `NextTrackAsync(zoneIndex)`
- `PreviousTrackCommand` → `PreviousTrackAsync(zoneIndex)`
- `SetTrackCommand` → `PlayTrackAsync(zoneIndex, trackIndex)`

**Client Commands → IClientService Methods**:
- `SetClientVolumeCommand` → `SetVolumeAsync(clientIndex, volume)`
- `SetClientMuteCommand` → `SetMuteAsync(clientIndex, muted)`
- `AssignClientToZoneCommand` → `AssignToZoneAsync(clientIndex, zoneIndex)`

## IMPLEMENTATION STEPS

### Step 1: KnxService Migration

```csharp
// Current constructor
public KnxService(
    ILogger<KnxService> logger,
    IOptions<SnapDogConfiguration> configuration,
    IZoneService zoneService,           // Already exists
    IClientService clientService        // ADD
)

// Remove mediator-related fields
// private readonly IServiceScopeFactory _serviceScopeFactory; // REMOVE
```

**Migration Pattern**:
1. Add `IClientService` dependency
2. Replace `mediator.SendCommandAsync<SetZoneVolumeCommand>()` with `_zoneService.SetVolumeAsync()`
3. Replace `mediator.SendCommandAsync<SetClientVolumeCommand>()` with `_clientService.SetVolumeAsync()`
4. Remove all mediator service resolution code

### Step 2: MqttService Migration

```csharp
// Current constructor  
public MqttService(
    ILogger<MqttService> logger,
    IOptions<SnapDogConfiguration> configuration,
    IZoneService zoneService,           // Already exists
    IClientService clientService        // ADD
)
```

**Migration Pattern**:
1. Add `IClientService` dependency
2. Replace all `mediator.SendCommandAsync()` calls with direct service calls
3. Map MQTT command parsing to appropriate service methods
4. Remove mediator infrastructure

### Step 3: GlobalStatusService Migration

```csharp
// Current constructor
public GlobalStatusService(
    ILogger<GlobalStatusService> logger,
    IOptions<SnapDogConfiguration> configuration,
    IHubContext<SnapDogHub> hubContext  // ADD
)
```

**Migration Pattern**:
1. Add `IHubContext<SnapDogHub>` dependency
2. Replace `mediator.PublishAsync()` with direct SignalR hub calls
3. Remove timer-based mediator publishing
4. Implement direct status publishing

### Step 4: Update Dependency Injection

```csharp
// Program.cs - Update service registrations
builder.Services.AddScoped<IZoneService, ZoneService>();      // Ensure registered
builder.Services.AddScoped<IClientService, ClientService>();  // Ensure registered

// Integration services already registered, just need updated constructors
```

## RISK MITIGATION

- **Service Interface Changes**: Verify IZoneService and IClientService have required methods
- **Command Parameter Mapping**: Ensure command properties map correctly to service method parameters
- **Error Handling**: Preserve existing error handling patterns
- **Performance**: Monitor for any performance changes

## SUCCESS CRITERIA

- [ ] KnxService uses direct IZoneService and IClientService calls
- [ ] MqttService uses direct service calls instead of mediator
- [ ] GlobalStatusService uses direct SignalR hub calls
- [ ] Build: 0 errors, ≤20 warnings
- [ ] No mediator references in integration services
- [ ] All integration functionality preserved
- [ ] Command parsing logic maintained

## VALIDATION COMMANDS

```bash
# Check current mediator usage in integration services
grep -rn "mediator\|IMediator\|SendCommandAsync" SnapDog2/Infrastructure/Integrations/

# Build verification
dotnet build SnapDog2/SnapDog2.csproj --verbosity quiet

# Verify direct service calls
grep -rn "_zoneService\|_clientService\|_hubContext" SnapDog2/Infrastructure/Integrations/

# Check for remaining mediator references
grep -rn "IMediator\|mediator\." SnapDog2/Infrastructure/Integrations/
```

## NEXT PHASE PREPARATION

After Phase 3.4 completion:
- **Phase 3.5**: Remove all mediator infrastructure and packages
- **Phase 4**: State Store Event-Driven Architecture
- **Phase 5**: Integration Publisher Abstraction

## INTEGRATION SERVICE PATTERNS

**KNX Command Processing**:
```csharp
// Before: mediator command
var command = new SetZoneVolumeCommand { ZoneIndex = zoneIndex, Volume = volume };
var result = await mediator.SendCommandAsync<SetZoneVolumeCommand, Result>(command);

// After: direct service call
var result = await _zoneService.SetVolumeAsync(zoneIndex, volume);
```

**MQTT Command Processing**:
```csharp
// Before: mediator command
var command = CommandFactory.CreateSetPlaylistCommand(zoneIndex, playlistIndex);
var result = await mediator.SendCommandAsync<SetPlaylistCommand, Result>(command);

// After: direct service call
var result = await _zoneService.SetPlaylistAsync(zoneIndex, playlistIndex);
```

**Status Publishing**:
```csharp
// Before: mediator notification
await mediator.PublishAsync(new SystemStatusNotification { ... });

// After: direct SignalR
await _hubContext.Clients.All.SendAsync("SystemStatus", statusData);
```
