# Zone Grouping Test Updates

## New Test File: ContinuousZoneGroupingTests.cs

### 🎯 **Startup Behavior Test**
- `StartupBehavior_ShouldPerformInitialReconciliation()`
- Validates that the background service performs proper initial reconciliation
- Checks that system starts in healthy state with correct grouping

### 🔄 **Continuous Monitoring Test**
- `ContinuousMonitoring_ShouldDetectAndCorrectManualChanges()`
- **Key test for your requirement**: Tests automatic correction after manual changes
- Manually breaks grouping via Snapcast API
- Waits up to 45 seconds for automatic correction
- Validates that background service detects and fixes the issue

### 🚨 **Edge Case Test**
- `EdgeCase_MultipleManualChanges_ShouldHandleGracefully()`
- Tests system resilience with multiple rapid manual changes
- Ensures system eventually stabilizes even under stress

### 📊 **Monitoring Endpoints Test**
- `MonitoringEndpoints_ShouldProvideAccurateStatus()`
- Validates that status and validation endpoints provide consistent information
- Tests read-only API functionality

### 🏷️ **Client Name Synchronization Test**
- `ClientNameSynchronization_ShouldHappenAutomatically()`
- Validates that client names are automatically synchronized
- Ensures friendly names replace MAC addresses

## Updated Existing Tests: ZoneGroupingRealWorldTests.cs

### ✅ **Removed Manual API Tests**
- Removed `Scenario_05_RecoveryAPI_ShouldFixBrokenGrouping()` (manual sync endpoint)
- Removed `Scenario_06_FullReconciliation_ShouldHandleComplexBrokenState()` (manual reconcile endpoint)

### 🔄 **Replaced with Automatic Tests**
- `Scenario_05_AutomaticRecovery_ShouldFixBrokenGroupingAutomatically()`
  - Tests automatic recovery without manual intervention
  - Waits for background service to detect and fix issues
- `Scenario_06_ComplexBrokenState_ShouldHandleAutomatically()`
  - Tests automatic handling of complex broken states
  - Validates system can recover from all-clients-in-one-group scenario

### 📈 **Updated Performance Test**
- `Scenario_08_PerformanceUnderLoad_ShouldHandleMultipleOperations()`
- Removed manual operation calls (no longer exist)
- Focuses on monitoring endpoint performance
- Increased concurrent operations for better load testing

## Test Coverage

### ✅ **Startup Scenarios**
- Initial reconciliation on service startup
- Proper zone grouping establishment
- Client name synchronization during startup

### ✅ **Runtime Scenarios**
- **Manual grouping changes detection** (your key requirement)
- Automatic correction within monitoring interval (30s)
- Multiple rapid changes handling
- Complex broken state recovery

### ✅ **Edge Cases**
- Service resilience under load
- Multiple concurrent monitoring requests
- System stability after corrections
- Cross-system consistency validation

### ✅ **Monitoring & Observability**
- Status endpoint accuracy
- Validation endpoint consistency
- Performance under concurrent load
- Health state reporting

## Key Test Scenarios for Your Requirements

### 🎯 **After Startup Test**
```csharp
StartupBehavior_ShouldPerformInitialReconciliation()
```
- Validates proper grouping after service startup
- Ensures background service completes initial reconciliation

### 🎯 **Manual Change Detection Test**
```csharp
ContinuousMonitoring_ShouldDetectAndCorrectManualChanges()
```
- **This is the key test for your requirement**
- Manually changes grouping via Snapcast API
- Validates automatic detection and correction
- Tests the 30-second monitoring interval behavior

## Running the Tests

### Individual Test Categories
```bash
# Run only continuous monitoring tests
dotnet test --filter "FullyQualifiedName~ContinuousZoneGroupingTests"

# Run specific edge case test
dotnet test --filter "FullyQualifiedName~ContinuousMonitoring_ShouldDetectAndCorrectManualChanges"

# Run all zone grouping tests
dotnet test --filter "FullyQualifiedName~ZoneGrouping"
```

### Test Timing Considerations
- **Startup tests**: ~2-5 seconds (wait for service initialization)
- **Continuous monitoring tests**: ~45-60 seconds (wait for automatic correction)
- **Edge case tests**: ~50-90 seconds (multiple corrections)
- **Performance tests**: ~5-10 seconds (concurrent operations)

## Expected Behavior Validation

### ✅ **Automatic Operation**
- No manual API calls needed
- Background service handles everything
- Self-healing system behavior

### ✅ **Timing Validation**
- Corrections happen within 30-45 seconds
- System stabilizes after changes
- Performance remains good under load

### ✅ **Resilience Testing**
- Multiple rapid changes handled gracefully
- Complex broken states recovered automatically
- System maintains consistency across operations
