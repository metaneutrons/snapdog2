# SNAPDOG2 MEDIATOR REMOVAL - PHASE 3.3.3 IMPLEMENTATION PROMPT

## CURRENT PHASE: Phase 3.3 - Complete Mediator Removal (Sprint 5-6)
## CURRENT STEP: 3.3.3 - ClientManager Mediator Removal
## LAST COMPLETED: Phase 3.3.2 - ZoneManager direct service calls successful
## NEXT OBJECTIVE: Apply proven ZoneManager pattern to ClientManager

## IMPLEMENTATION STATUS

- **Files Modified**: ZoneManager.cs (✓ direct calls complete)
- **Files Removed**: 0
- **Services Migrated**: ZoneManager (✓ complete)
- **Tests Updated**: 0
- **Build Status**: PASS (0 errors, 26 warnings)

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

### 1. Apply Proven ZoneManager Pattern to ClientManager

**Investigation Required**:
1. Analyze ClientManager mediator usage patterns
2. Identify required service dependencies
3. Map notification types to SignalR equivalents

**Expected Pattern Analysis**:
```bash
# Find mediator usage in ClientManager
grep -n "mediator\|IMediator\|SendCommandAsync\|PublishAsync" SnapDog2/Domain/Services/ClientManager.cs

# Find service scope usage
grep -n "_serviceScopeFactory\|GetRequiredService" SnapDog2/Domain/Services/ClientManager.cs
```

### 2. Add Required Dependencies

**Likely Required Services**:
- `IHubContext<SnapDogHub>` - For SignalR notifications
- `IClientStateStore` - For state management (may already exist)
- `ISnapcastService` - For Snapcast operations (may already exist)

**Constructor Pattern** (based on ZoneManager success):
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

### 3. Replace Mediator Calls with Direct Service Calls

**Expected ClientManager Operations**:
- Client volume changes → Direct state store updates
- Client connection status → Direct state store updates
- Client zone assignment → Direct state store updates
- Client mute status → Direct state store updates

**Notification Mapping Strategy**:
- Client volume changes → `"ClientVolumeChanged"`
- Client connection status → `"ClientConnectionChanged"`
- Client zone assignment → `"ClientZoneChanged"`
- Client mute status → `"ClientMuteChanged"`

### 4. Remove Mediator Infrastructure

**After Direct Calls Working**:
1. Remove any remaining mediator service resolutions
2. Remove mediator-related using statements
3. Clean up any stub infrastructure

## IMPLEMENTATION STEPS

### Step 1: Analyze Current ClientManager State

```bash
# Check ClientManager mediator usage
grep -rn "mediator\|IMediator\|SendCommandAsync\|PublishAsync" SnapDog2/Domain/Services/ClientManager.cs

# Check current constructor dependencies
grep -A 10 "public.*ClientManager(" SnapDog2/Domain/Services/ClientManager.cs

# Check for service scope factory usage
grep -n "_serviceScopeFactory" SnapDog2/Domain/Services/ClientManager.cs
```

### Step 2: Add Required Dependencies (Following ZoneManager Pattern)

1. Identify what services ClientManager needs based on mediator usage
2. Add dependencies to constructor (likely `IHubContext<SnapDogHub>`)
3. Add private readonly fields
4. Initialize fields in constructor

### Step 3: Replace Mediator Calls with Direct Service Calls

1. Replace any command calls with direct service operations
2. Replace notification publishing with direct SignalR hub calls
3. Use same notification mapping pattern as ZoneManager

### Step 4: Build and Verify

1. Build verification: `dotnet build SnapDog2/SnapDog2.csproj --verbosity quiet`
2. Ensure no mediator references remain in ClientManager
3. Verify functionality preserved

### Step 5: Clean Up

1. Remove any unused using statements
2. Remove any remaining mediator-related code
3. Final build verification

## RISK MITIGATION

- **Unknown Dependencies**: Use same analysis approach as ZoneManager
- **Different Patterns**: Adapt proven ZoneManager approach
- **Build Issues**: Keep existing code as fallback until direct calls work
- **Missing Services**: Check existing DI registrations

## SUCCESS CRITERIA

- [ ] ClientManager mediator usage identified and documented
- [ ] Required service dependencies identified and added
- [ ] All mediator calls replaced with direct service calls
- [ ] All notifications replaced with direct SignalR hub calls
- [ ] Build: 0 errors, ≤26 warnings
- [ ] No mediator references in ClientManager
- [ ] Functionality preserved
- [ ] Pattern consistent with ZoneManager approach

## VALIDATION COMMANDS

```bash
# Pre-migration analysis
grep -rn "mediator\|IMediator" SnapDog2/Domain/Services/ClientManager.cs

# Build verification
dotnet build SnapDog2/SnapDog2.csproj --verbosity quiet

# Post-migration verification
grep -rn "IMediator\|mediator\." SnapDog2/Domain/Services/ClientManager.cs
grep -rn "_hubContext\|direct.*call" SnapDog2/Domain/Services/ClientManager.cs

# Verify direct service calls
grep -c "_hubContext\|direct" SnapDog2/Domain/Services/ClientManager.cs
```

## NEXT PHASE PREPARATION

After Phase 3.3.3 completion:
- **Phase 3.3.4**: Apply same pattern to PlaylistManager
- **Phase 3.3.5**: Remove all mediator infrastructure and packages
- **Phase 3.4**: Integration Services Migration (IKnxService, IMqttService)

## LESSONS FROM ZONEMANAGER SUCCESS

- **Proven Pattern**: Direct service injection works reliably
- **SignalR Integration**: Hub context injection is straightforward
- **Build Stability**: Incremental approach maintains compilation
- **Performance**: Direct calls eliminate mediator overhead
- **Debugging**: Cleaner call stacks improve troubleshooting
- **Repeatable**: Same pattern applies across all domain services

## EXPECTED OUTCOME

ClientManager will follow the same successful transformation as ZoneManager:
- **Before**: ClientManager → Mediator → Commands/Handlers → Services
- **After**: ClientManager → Direct Service Calls → StateStore → SignalR Hub

This continues the proven architectural simplification while maintaining all functionality.
