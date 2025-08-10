# SnapDog2 Logging Refactoring Master Plan

> **📢 IMPORTANT UPDATE**: KnxMonitor components have been moved to their own independent repository. This refactoring plan now focuses exclusively on SnapDog2 components.

## 📋 Executive Summary

**Objective**: Convert remaining old-style logging calls to structured `[LoggerMessage]` pattern across SnapDog2 files

**Benefits**:
- 🚀 **Performance**: Compile-time optimized logging with zero allocations
- 🔒 **Type Safety**: Compile-time parameter validation
- 📊 **Consistency**: Standardized logging format across entire codebase
- 🛠️ **Maintainability**: Centralized logging method definitions

**Status**: 1/237 completed (PlayCommandHandler example)
**Estimated Effort**: 8-12 hours focused development

---

## 🎯 Priority Matrix

### P1 - CRITICAL (40+ violations) - **IMMEDIATE ACTION REQUIRED**
| File | Violations | Component | Complexity | Notes |
|------|------------|-----------|------------|-------|
| `ZoneCommandHandlers.cs` | **38** | SnapDog2.Server | Very High | ✅ Partially completed (PlayCommandHandler done) |

> **Note**: KnxMonitor components have been moved to their own repository and are no longer part of SnapDog2.

### P1 - CRITICAL (20+ violations) - **HIGH PRIORITY**
| File | Violations | Component | Complexity | Notes |
|------|------------|-----------|------------|-------|
| `TuiDisplayService.cs` | **29** | KnxMonitor | High | UI service with complex logging |

### P2 - HIGH (15-19 violations) - **NEXT SPRINT**
| File | Violations | Component | Complexity | Notes |
|------|------------|-----------|------------|-------|
| `FalcontDptService.cs` | **19** | KnxMonitor | Medium | DPT processing service |
| `MqttService.cs` | **18** | SnapDog2.Infrastructure | Medium | Core integration service |
| `GlobalStatusService.cs` | **17** | SnapDog2.Server | Medium | Status management |
| `FalconService.cs` | **15** | KnxMonitor | Medium | Core KNX service |

### P3 - MEDIUM (5-14 violations) - **FOLLOWING SPRINT**
| File | Violations | Component | Complexity | Notes |
|------|------------|-----------|------------|-------|
| `KnxMonitorService.cs` | **14** | KnxMonitor | Medium | Monitoring service |
| `SharedLoggingBehavior.cs` | **6** | SnapDog2.Server | Low | Behavior pipeline |
| `HealthCheckService.cs` | **6** | KnxMonitor | Low | Health monitoring |
| `FalconDptDecodingService.cs` | **5** | KnxMonitor | Low | DPT decoding |

### P4 - LOW (1-4 violations) - **CLEANUP PHASE**
| File | Violations | Component | Notes |
|------|------------|-----------|-------|
| `IntegrationServicesHostedService.cs` | **4** | SnapDog2.Worker | Worker service |
| `KnxService.cs` | **4** | SnapDog2.Infrastructure | ⚠️ Already has LoggerMessage (8001-8038) |
| `GetErrorStatusQueryHandler.cs` | **3** | SnapDog2.Server | Query handler |
| `DptDecodingService.cs` | **3** | KnxMonitor | DPT service |
| `DisplayService.cs` | **3** | KnxMonitor | Display service |
| `ResilientHost.cs` | **2** | SnapDog2.Hosting | Host service |
| `StatePublishingService.cs` | **1** | SnapDog2.Services | State service |
| `ZoneNotificationHandlers.cs` | **1** | SnapDog2.Server | ⚠️ Already has LoggerMessage (6001-6008) |
| `GlobalNotificationHandlers.cs` | **1** | SnapDog2.Server | Notification handler |
| `ClientNotificationHandlers.cs` | **1** | SnapDog2.Server | Notification handler |
| `SnapcastService.cs` | **1** | SnapDog2.Infrastructure | Integration service |

---

## 🔢 LoggerMessage ID Numbering Scheme

**CRITICAL**: Use this numbering scheme to avoid conflicts!

| Range | Component | Status | Notes |
|-------|-----------|--------|-------|
| **9000-9999** | Zone Command Handlers | 🟡 In Progress | 9001-9002 used (PlayCommandHandler) |
| **8000-8999** | Infrastructure Services | 🟡 Partial | 8001-8038 used (KnxService) |
| **7000-7999** | Integration Services | 🔴 Available | MQTT, Snapcast services |
| **6000-6999** | Notification Handlers | 🟡 Partial | 6001-6008 used (ZoneNotificationHandlers) |
| **5000-5999** | Global Services | 🔴 Available | GlobalStatusService, etc. |
| **4000-4999** | Behaviors & Middleware | 🔴 Available | SharedLoggingBehavior |
| **3000-3999** | Hosting & Worker Services | 🔴 Available | ResilientHost, IntegrationServices |
| **2000-2999** | KnxMonitor Services | 🔴 Available | All KnxMonitor components |
| **1000-1999** | Utility & Helper Services | 🔴 Available | StatePublishingService, etc. |

---

## 🛠️ Conversion Templates

### Template 1: Simple Information Logging
```csharp
// BEFORE
this._logger.LogInformation("Operation {Operation} completed for {EntityId}", operation, entityId);

// AFTER
this.LogOperationCompleted(operation, entityId);

[LoggerMessage(XXXX, LogLevel.Information, "Operation {Operation} completed for {EntityId}")]
private partial void LogOperationCompleted(string operation, int entityId);
```

### Template 2: Warning with Context
```csharp
// BEFORE
this._logger.LogWarning("Entity {EntityId} not found for {CommandName}", entityId, commandName);

// AFTER
this.LogEntityNotFound(entityId, commandName);

[LoggerMessage(XXXX, LogLevel.Warning, "Entity {EntityId} not found for {CommandName}")]
private partial void LogEntityNotFound(int entityId, string commandName);
```

### Template 3: Error with Exception
```csharp
// BEFORE
this._logger.LogError(ex, "Failed to process {Operation} for {EntityId}", operation, entityId);

// AFTER
this.LogOperationFailed(operation, entityId, ex);

[LoggerMessage(XXXX, LogLevel.Error, "Failed to process {Operation} for {EntityId}")]
private partial void LogOperationFailed(string operation, int entityId, Exception exception);
```

### Template 4: Class Declaration Changes
```csharp
// BEFORE
public class MyService

// AFTER
public partial class MyService
```

---

## 🚀 Execution Plan

### Phase 1: Critical Files (Week 1)
1. **✅ COMPLETED**: `ZoneCommandHandlers.cs` - PlayCommandHandler (2/38 violations)
2. **🎯 NEXT**: Complete remaining 17 handlers in `ZoneCommandHandlers.cs` (36 violations)
3. **🎯 NEXT**: `FalconKnxMonitorService.cs` (44 violations) - ID range 2000-2044
4. **🎯 NEXT**: `TuiDisplayService.cs` (29 violations) - ID range 2045-2074

### Phase 2: High Priority Files (Week 2)
1. `FalcontDptService.cs` (19 violations) - ID range 2075-2094
2. `MqttService.cs` (18 violations) - ID range 7001-7018
3. `GlobalStatusService.cs` (17 violations) - ID range 5001-5017
4. `FalconService.cs` (15 violations) - ID range 2095-2110

### Phase 3: Medium Priority Files (Week 3)
1. `KnxMonitorService.cs` (14 violations) - ID range 2111-2125
2. `SharedLoggingBehavior.cs` (6 violations) - ID range 4001-4006
3. `HealthCheckService.cs` (6 violations) - ID range 2126-2131
4. `FalconDptDecodingService.cs` (5 violations) - ID range 2132-2136

### Phase 4: Cleanup (Week 4)
1. Process all remaining 12 files with 1-4 violations each
2. **⚠️ SKIP**: Files that already have LoggerMessage patterns
3. Final validation and testing

---

## ✅ Validation Checklist

### Per-File Checklist
- [ ] Class declaration changed to `partial class`
- [ ] All `_logger.LogXXX()` calls replaced with `this.LogXXX()` calls
- [ ] All LoggerMessage methods added with unique IDs
- [ ] Proper parameter types (handle enums, exceptions)
- [ ] Build succeeds without warnings
- [ ] Logging functionality tested

### Project-Wide Checklist
- [ ] No ID conflicts across all files
- [ ] All old-style logging patterns eliminated
- [ ] Performance benchmarks show improvement
- [ ] Log output format remains consistent
- [ ] All tests pass

---

## 🔧 Automation Scripts

### Find Remaining Violations
```bash
# Count remaining violations
find . -name "*.cs" -type f -exec grep -c "_logger\.Log\(Information\|Warning\|Error\|Debug\)" {} + | grep -v ":0" | wc -l

# List files with violations
find . -name "*.cs" -type f -exec sh -c 'count=$(grep -c "_logger\.Log\(Information\|Warning\|Error\|Debug\)" "$1" 2>/dev/null || echo 0); if [ "$count" -gt 0 ]; then echo "$count violations: $1"; fi' _ {} \; | sort -nr
```

### Validate LoggerMessage IDs
```bash
# Check for ID conflicts
grep -r "LoggerMessage(" --include="*.cs" . | grep -o "LoggerMessage([0-9]*" | sort | uniq -d
```

---

## 📊 Progress Tracking

| Phase | Files | Violations | Status | Completion |
|-------|-------|------------|--------|------------|
| **Phase 1** | 3 files | 111 violations | 🟡 In Progress | 2/111 (1.8%) |
| **Phase 2** | 4 files | 69 violations | 🔴 Pending | 0/69 (0%) |
| **Phase 3** | 4 files | 31 violations | 🔴 Pending | 0/31 (0%) |
| **Phase 4** | 11 files | 26 violations | 🔴 Pending | 0/26 (0%) |
| **TOTAL** | **22 files** | **237 violations** | 🟡 **In Progress** | **2/237 (0.8%)** |

---

## 🎯 Success Metrics

- **Performance**: 15-20% reduction in logging overhead
- **Maintainability**: Zero old-style logging patterns remaining
- **Type Safety**: 100% compile-time parameter validation
- **Consistency**: Standardized logging format across all components
- **Code Quality**: Improved IntelliSense and refactoring support

---

**Last Updated**: January 9, 2025  
**Next Review**: After Phase 1 completion  
**Owner**: Development Team  
**Priority**: High - Technical Debt Reduction
