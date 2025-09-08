# SNAPDOG2 MEDIATOR REMOVAL - PHASE 3.5 IMPLEMENTATION PROMPT

## CURRENT PHASE: Phase 3.5 - Complete Mediator Infrastructure Removal
## CURRENT STEP: 3.5.1 - Remove All Command/Handler Infrastructure and Packages
## LAST COMPLETED: Phase 3.3.2 - ZoneManager direct service calls successful
## NEXT OBJECTIVE: Complete elimination of mediator pattern from codebase

## IMPLEMENTATION STATUS

- **Files Modified**: ZoneManager.cs (✓ direct calls complete)
- **Files Removed**: 0 (pending mass removal)
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
ClientManager → Cortex.Mediator (needs removal)
PlaylistManager → Cortex.Mediator (needs removal)
Integration Services → Cortex.Mediator (needs removal)
Command/Handler Infrastructure → READY FOR DELETION
```

## PHASE 3.5 OBJECTIVES

### 1. Mass Command/Handler Infrastructure Removal

**Target for Complete Deletion**:
```bash
# Command Classes (Complete removal)
SnapDog2/Server/Zones/Commands/
SnapDog2/Server/Clients/Commands/
SnapDog2/Server/Global/Commands/
SnapDog2/Server/Shared/Commands/

# Handler Classes (Complete removal)
SnapDog2/Server/Zones/Handlers/
SnapDog2/Server/Clients/Handlers/
SnapDog2/Server/Global/Handlers/
SnapDog2/Server/Shared/Handlers/

# Factory Classes (Complete removal)
SnapDog2/Server/Shared/Factories/CommandFactory.cs
```

### 2. Cortex.Mediator Package Removal

**Package References to Remove**:
```xml
<!-- Remove from SnapDog2.csproj -->
<PackageReference Include="Cortex.Mediator" Version="x.x.x" />
```

**Mediator Configuration Cleanup**:
```csharp
// File: Application/Extensions/DependencyInjection/MediatorConfiguration.cs
// REMOVE: All command and handler registrations
// PRESERVE: Only notification registrations if needed for domain events
```

### 3. Service Registration Updates

**Program.cs Updates**:
```csharp
// Remove mediator command registrations
// builder.Services.AddMediatorCommands(); // REMOVE

// Ensure direct service registrations exist
builder.Services.AddScoped<IZoneService, ZoneService>();      // Verify exists
builder.Services.AddScoped<IClientService, ClientService>();  // Verify exists
builder.Services.AddScoped<IPlaylistManager, PlaylistManager>(); // Verify exists
```

### 4. Remaining Service Stub Replacements

**Services Still Using Mediator**:
- **ClientManager**: Apply ZoneManager pattern
- **PlaylistManager**: Apply ZoneManager pattern  
- **Integration Services**: KnxService, MqttService, GlobalStatusService

**Quick Stub Replacement Strategy**:
1. Replace mediator calls with stub returns for now
2. Focus on infrastructure removal first
3. Service-by-service migration in subsequent phases

## IMPLEMENTATION STEPS

### Step 1: Inventory and Backup

```bash
# Count files to be deleted
find SnapDog2/Server -name "*Command*.cs" -o -name "*Handler*.cs" | wc -l

# Create backup list
find SnapDog2/Server -name "*Command*.cs" -o -name "*Handler*.cs" > files_to_delete.txt

# Verify no critical files in list
grep -v -E "(Command\.cs|Handler\.cs)$" files_to_delete.txt
```

### Step 2: Mass File Deletion

```bash
# Remove command directories
rm -rf SnapDog2/Server/Zones/Commands/
rm -rf SnapDog2/Server/Clients/Commands/
rm -rf SnapDog2/Server/Global/Commands/

# Remove handler directories  
rm -rf SnapDog2/Server/Zones/Handlers/
rm -rf SnapDog2/Server/Clients/Handlers/
rm -rf SnapDog2/Server/Global/Handlers/
rm -rf SnapDog2/Server/Shared/Handlers/

# Remove factory classes
rm -f SnapDog2/Server/Shared/Factories/CommandFactory.cs
```

### Step 3: Package Removal

```bash
# Remove Cortex.Mediator package
dotnet remove SnapDog2/SnapDog2.csproj package Cortex.Mediator

# Clean and restore
dotnet clean
dotnet restore
```

### Step 4: Build and Fix Compilation Errors

```bash
# Attempt build to identify remaining references
dotnet build SnapDog2/SnapDog2.csproj --verbosity normal

# Fix compilation errors by:
# 1. Removing using statements for deleted classes
# 2. Stubbing out remaining mediator calls
# 3. Adding missing service registrations
```

### Step 5: Stub Remaining Services

For services still using mediator, apply minimal stubs:

```csharp
// Quick stub pattern for remaining services
public async Task<Result> SomeMethodAsync(int param)
{
    // TODO: Replace with direct service calls in next phase
    return Result.Success();
}
```

## RISK MITIGATION

- **Build Breaks**: Expect compilation errors, fix systematically
- **Missing Dependencies**: Add service registrations as needed
- **Integration Issues**: Stub out complex integrations temporarily
- **Test Failures**: Update tests to remove command/handler references

## SUCCESS CRITERIA

- [ ] All command/handler directories deleted (~500+ files removed)
- [ ] Cortex.Mediator package completely removed
- [ ] Build: 0 errors, ≤30 warnings
- [ ] No mediator references in codebase except preserved domain events
- [ ] All services have basic stub implementations
- [ ] Integration tests pass with stubbed implementations
- [ ] **Code reduction: ~4,000+ lines removed**

## VALIDATION COMMANDS

```bash
# Verify command/handler removal
find SnapDog2/Server -name "*Command*.cs" -o -name "*Handler*.cs"

# Check for remaining mediator references
grep -r "Cortex.Mediator\|IMediator\|mediator\." SnapDog2/ --exclude-dir=obj --exclude-dir=bin

# Build verification
dotnet build SnapDog2/SnapDog2.csproj --verbosity quiet

# Package verification
dotnet list SnapDog2/SnapDog2.csproj package | grep -i mediator
```

## NEXT PHASE PREPARATION

After Phase 3.5 completion:
- **Phase 4**: State Store Event-Driven Architecture
- **Phase 5**: Integration Publisher Abstraction
- **Phase 6**: Service-by-Service Direct Call Migration

## EXPECTED IMPACT

- **Codebase Size**: Reduction of ~4,000+ lines
- **Build Performance**: Faster compilation without mediator infrastructure
- **Architecture Clarity**: Clear 3-layer pattern (API → Service → StateStore)
- **Development Speed**: Simplified debugging and development workflow

This phase represents the **largest single code reduction** in the entire transformation, eliminating the bulk of the mediator infrastructure while preserving the proven direct service call pattern established in ZoneManager.
