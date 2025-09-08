# SNAPDOG2 MEDIATOR REMOVAL - PHASE 3.3.3 IMPLEMENTATION PROMPT

## CURRENT PHASE: Phase 3.3 - Complete Mediator Removal (Sprint 5-6)
## CURRENT STEP: 3.3.3 - ClientManager Mediator Removal
## LAST COMPLETED: Phase 3.3.2 - ZoneManager direct service calls implementation
## NEXT OBJECTIVE: Apply same mediator removal pattern to ClientManager

## IMPLEMENTATION STATUS

- **Files Modified**: ZoneManager.cs (direct calls complete)
- **Files Removed**: 0
- **Services Migrated**: ZoneManager (✓ complete)
- **Tests Updated**: 0
- **Build Status**: PASS (0 errors, ≤20 warnings)

## CRITICAL PATTERNS ESTABLISHED

- **LoggerMessage**: Fully implemented across all services
- **Service Injection**: Direct service calls proven working in ZoneManager
- **StateStore Events**: Not yet implemented
- **Attribute Migration**: CommandId/StatusId preserved in constants

## CURRENT ARCHITECTURE STATE

```
ZoneManager → Direct Service Calls (✓ complete)
ClientManager → Cortex.Mediator (needs migration)
PlaylistManager → Cortex.Mediator (pending)
```

## PHASE 3.3.3 OBJECTIVES

### 1. Analyze ClientManager Mediator Usage

**Investigation Tasks**:
1. Search for mediator usage patterns in ClientManager
2. Identify required service dependencies
3. Map notification types to SignalR equivalents

**Expected Patterns**:
```bash
# Find mediator usage
grep -n "mediator\|IMediator" SnapDog2/Domain/Services/ClientManager.cs

# Find service scope usage
grep -n "_serviceScopeFactory\|GetRequiredService" SnapDog2/Domain/Services/ClientManager.cs
```

### 2. Apply Proven Migration Pattern

**Step 1: Add Stub Infrastructure**
- Add temporary IMediator stub interface (reuse from ZoneManager)
- Replace all mediator service resolutions with stub

**Step 2: Identify Required Services**
- Determine what services ClientManager needs for direct calls
- Add service dependencies to constructor

**Step 3: Replace with Direct Calls**
- Replace query calls with direct service calls
- Replace notification publishing with SignalR hub calls

**Step 4: Clean Up**
- Remove stub infrastructure
- Verify build and functionality

### 3. Service Dependencies Analysis

**Likely Required Services**:
- `IHubContext<SnapDogHub>` - For SignalR notifications
- `IClientStateStore` - For state management (may already exist)
- `ISnapcastService` - For Snapcast operations (may already exist)

**Constructor Pattern**:
```csharp
public ClientManager(
    ILogger<ClientManager> logger,
    ISnapcastStateRepository snapcastStateRepository,
    ISnapcastService snapcastService,
    IClientStateStore clientStateStore,
    IHubContext<SnapDogHub> hubContext,        // ADD if needed
    SnapDogConfiguration configuration
) : IClientManager
```

### 4. Notification Mapping Strategy

**Expected ClientManager Notifications**:
- Client volume changes → `"ClientVolumeChanged"`
- Client connection status → `"ClientConnectionChanged"`
- Client zone assignment → `"ClientZoneChanged"`
- Client mute status → `"ClientMuteChanged"`

## IMPLEMENTATION STEPS

### Step 1: Analyze Current State

```bash
# Check ClientManager mediator usage
grep -rn "mediator\|IMediator\|SendQueryAsync\|PublishAsync" SnapDog2/Domain/Services/ClientManager.cs

# Check current constructor
grep -A 10 "public.*ClientManager(" SnapDog2/Domain/Services/ClientManager.cs
```

### Step 2: Apply Stub Pattern (Proven Working)

1. Copy stub infrastructure from ZoneManager
2. Replace mediator service resolutions with stub
3. Verify build passes

### Step 3: Add Required Service Dependencies

1. Identify needed services based on mediator usage
2. Add to constructor parameters
3. Add private readonly fields

### Step 4: Replace Stub with Direct Calls

1. Replace query calls with direct service calls
2. Replace notifications with SignalR hub calls
3. Remove stub infrastructure

### Step 5: Verification

1. Build verification
2. Compare functionality with ZoneManager pattern
3. Ensure no mediator references remain

## RISK MITIGATION

- **Unknown Dependencies**: Use same analysis approach as ZoneManager
- **Different Patterns**: Adapt proven ZoneManager approach
- **Build Issues**: Keep stub as fallback until direct calls work
- **Missing Services**: Check existing DI registrations

## SUCCESS CRITERIA

- [ ] ClientManager mediator usage identified and documented
- [ ] Stub infrastructure successfully applied
- [ ] Required service dependencies identified and added
- [ ] Direct service calls replace all mediator usage
- [ ] Build: 0 errors, ≤20 warnings
- [ ] No mediator references in ClientManager
- [ ] Functionality preserved

## VALIDATION COMMANDS

```bash
# Pre-migration analysis
grep -rn "mediator\|IMediator" SnapDog2/Domain/Services/ClientManager.cs

# Build verification
dotnet build SnapDog2/SnapDog2.csproj --verbosity quiet

# Post-migration verification
grep -rn "IMediator\|mediator\." SnapDog2/Domain/Services/ClientManager.cs
grep -rn "_hubContext\|direct.*call" SnapDog2/Domain/Services/ClientManager.cs
```

## NEXT PHASE PREPARATION

After Phase 3.3.3 completion:
- **Phase 3.3.4**: Apply same pattern to PlaylistManager
- **Phase 3.3.5**: Remove all mediator infrastructure and packages
- **Phase 3.4**: Integration Services Migration (IKnxService, IMqttService)

## LESSONS FROM ZONEMANAGER

- Stub approach works well for gradual migration
- Direct service calls improve performance and debugging
- SignalR hub context injection is straightforward
- Build stability maintained throughout process
- Pattern is repeatable across services
