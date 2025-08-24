# Command-Status Flow Implementation Plan

## Overview

This plan outlines a systematic approach to transform SnapDog2 from the current mixed pattern to the pure Command-Status Flow pattern, ensuring commands only instruct external systems while events handle storage updates and integration publishing.

## Current State Analysis

### Existing Problematic Patterns

1. **Mixed Command Handlers** - Some update storage AND publish to integrations
2. **Inconsistent Event Handling** - Some events update storage, others don't
3. **Duplicate Publishing** - Both commands and events publish to integrations
4. **Race Conditions** - Commands might update storage before external system confirms

### Command Handlers Requiring Refactoring

Based on the codebase analysis, these handlers likely need refactoring:

**Client Commands:**

- `SetClientVolumeCommandHandler` ⚠️ (currently publishes StatusChangedNotification)
- `SetClientMuteCommandHandler`
- `ClientVolumeUpCommandHandler`
- `ClientVolumeDownCommandHandler`
- `ToggleClientMuteCommandHandler`
- `SetClientLatencyCommandHandler`
- `SetClientNameCommandHandler`
- `AssignClientToZoneCommandHandler`

**Zone Commands:**

- `ZoneNameCommandHandler`
- `ControlSetCommandHandler`
- Various zone playback commands

## Implementation Plan

### Phase 1: Event Handler Infrastructure (Weeks 1-2)

#### 1.1 Audit Current Event Handlers

- **Identify** existing event handlers that already follow the pattern
- **Document** current event → storage → integration flows
- **Map** missing event handlers for external system notifications

#### 1.2 Create Missing Event Handlers

- **SnapcastEventNotificationHandler** ✅ (already implemented)
- **LibVlcEventNotificationHandler** (for media playback events)
- **KnxEventNotificationHandler** (for KNX bus events)
- **MqttEventNotificationHandler** (for MQTT command responses)

#### 1.3 Standardize Storage Update Pattern

```csharp
// Standard pattern for all event handlers
public async Task Handle(ExternalSystemNotification notification, CancellationToken cancellationToken)
{
    // 1. Validate notification
    // 2. Update storage (single source of truth)
    // 3. Publish status notification for integrations
}
```

#### 1.4 Enhance Integration Publishing

- **Consolidate** all integration publishing in `IntegrationPublishingHandlers` (rename from `SmartMqttNotificationHandlers`)
- **Ensure** both MQTT and KNX publishing for all status notifications
- **Add** missing status notification types
- **Prepare** for future integrations (WebSocket, REST API cache invalidation, etc.)

### Phase 2: Command Handler Refactoring (Weeks 3-4)

#### 2.1 Identify Command Handler Categories

**Category A: Pure External System Commands** (✅ Already correct)

- Commands that only call external systems
- No storage updates or integration publishing
- Example: Commands that only call SnapcastService

**Category B: Mixed Commands** (⚠️ Need refactoring)

- Commands that call external systems AND update storage/integrations
- Need to remove storage updates and integration publishing
- Example: `SetClientVolumeCommandHandler` with StatusChangedNotification

**Category C: Internal Commands** (🤔 Special handling)

- Commands that don't interact with external systems
- May need different pattern or conversion to events

#### 2.2 Refactor Category B Commands

**Before (Mixed Pattern):**

```csharp
public async Task<Result> Handle(SetClientVolumeCommand request, CancellationToken cancellationToken)
{
    var result = await client.SetVolumeAsync(request.Volume);

    if (result.IsSuccess)
    {
        // ❌ Remove this - let events handle it
        await _mediator.PublishAsync(new StatusChangedNotification { ... });
    }

    return result;
}
```

**After (Pure Command Pattern):**

```csharp
public async Task<Result> Handle(SetClientVolumeCommand request, CancellationToken cancellationToken)
{
    // ✅ Only instruct external system
    var result = await client.SetVolumeAsync(request.Volume);

    // Note: Initially return 200 OK to avoid breaking changes
    // Will switch to 202 Accepted in Phase 3 (Week 5)
    return result;
    // External system will send notification → event handler → storage → integrations
}
```

#### 2.3 Handle Category C Commands

- **Evaluate** if these should be commands or events
- **Convert** internal state changes to event-driven pattern
- **Maintain** API compatibility where needed

### Phase 3: API Response Strategy (Week 5)

#### 3.1 Current API Response Analysis

**Current Pattern:**

```bash
PUT /api/v1/clients/1/volume
Content-Type: application/json
Body: 75

Response: 200 OK
Body: 75
```

**Characteristics:**

- Simple integer/primitive values as request body
- Returns the same value as response body
- No JSON wrapper objects
- No success/error messages in response body
- HTTP status codes indicate success/failure

#### 3.2 Proposed API Response Strategy

**Command-Status Flow with 202 Accepted (Recommended)**

```bash
PUT /api/v1/clients/1/volume
Body: 75

Response: 202 Accepted (command accepted, processing asynchronously)
Body: 75
```

#### 3.3 API Impact Assessment

**Breaking Changes:**

- Response codes change from 200 OK → 202 Accepted
- Semantic meaning changes from "completed" to "accepted for processing"
- Timing of state changes (eventual consistency vs immediate)

**Non-Breaking Enhancements:**

- Request/response body format remains unchanged (simple primitives)
- Additional response headers for request tracking (optional)
- WebSocket notifications for real-time updates (future)

**Migration Strategy:**

- **Phase 1 (Weeks 3-4)**: Implement Command-Status Flow with 200 OK (no breaking changes)
- **Phase 2 (Week 5)**: Switch to 202 Accepted (minor breaking change, semantically correct)
- **Communication**: Document status code change and async processing behavior

**Benefits of 202 Accepted:**

- ✅ Semantically correct for async command processing
- ✅ Sets proper client expectations about eventual consistency
- ✅ Standard HTTP practice for accepted but not completed operations
- ✅ Future-proof for advanced async patterns

### Phase 4: Storage Consistency (Week 6)

#### 4.1 Storage Update Consolidation

- **Ensure** all storage updates happen only in event handlers
- **Remove** storage updates from command handlers
- **Add** validation to prevent direct storage manipulation

#### 4.2 State Synchronization

- **Implement** periodic state sync with external systems
- **Handle** missed notifications or connection issues
- **Add** health checks for storage consistency

#### 4.3 Migration Strategy for Existing Data

- **Audit** current storage state vs external system state
- **Implement** one-time synchronization process
- **Add** monitoring for state drift

### Phase 5: Integration Testing & Validation (Week 7)

#### 5.1 End-to-End Testing

```csharp
[Test]
public async Task VolumeChangeFlow_ShouldFollowCommandStatusPattern()
{
    // 1. Send command
    var result = await _mediator.SendAsync(new SetClientVolumeCommand(1, 75));
    Assert.That(result.IsSuccess);

    // 2. Verify no immediate storage update
    var immediateVolume = await _storage.GetClientVolumeAsync(1);
    Assert.That(immediateVolume, Is.Not.EqualTo(75)); // Should be old value

    // 3. Simulate external system notification
    await _snapcastSimulator.TriggerVolumeChangeAsync(1, 75);

    // 4. Wait for event processing
    await Task.Delay(100);

    // 5. Verify storage updated
    var finalVolume = await _storage.GetClientVolumeAsync(1);
    Assert.That(finalVolume, Is.EqualTo(75));

    // 6. Verify integrations notified
    _mqttClient.AssertMessageReceived("snapdog/clients/1/volume", "75");
    _knxClient.AssertGroupAddressWritten("3/1/2", 75);
}
```

#### 5.2 Performance Testing

- **Measure** command response times
- **Test** high-frequency command scenarios
- **Validate** event processing throughput
- **Monitor** storage consistency under load

#### 5.3 Regression Testing

- **Verify** all existing functionality works
- **Test** error scenarios and edge cases
- **Validate** integration endpoint behavior

### Phase 6: Monitoring & Observability (Week 8)

#### 6.1 Metrics Implementation

```csharp
// Command metrics
_metrics.IncrementCounter("commands.executed", tags: new[] { $"type:{commandType}" });
_metrics.RecordValue("commands.duration", duration, tags: new[] { $"type:{commandType}" });

// Event processing metrics
_metrics.IncrementCounter("events.processed", tags: new[] { $"type:{eventType}" });
_metrics.RecordValue("events.processing_lag", lag, tags: new[] { $"type:{eventType}" });

// Storage consistency metrics
_metrics.RecordValue("storage.consistency_check", consistencyScore);
_metrics.IncrementCounter("storage.updates", tags: new[] { $"entity:{entityType}" });
```

#### 6.2 Health Checks

```csharp
public class CommandStatusFlowHealthCheck : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync()
    {
        // Check external system connectivity
        // Check event processing pipeline health
        // Check storage consistency
        // Check integration endpoint health
    }
}
```

#### 6.3 Alerting Strategy

- **Command failure rate** > threshold
- **Event processing lag** > threshold
- **Storage inconsistency** detected
- **Integration endpoint failures** > threshold

## Migration Timeline

### Week 1-2: Foundation

- ✅ Event handler infrastructure
- ✅ Storage update standardization
- ✅ Integration publishing consolidation

### Week 3-4: Command Refactoring

- ⚠️ Refactor mixed command handlers
- ⚠️ Remove storage updates from commands
- ⚠️ Remove integration publishing from commands

### Week 5: API Strategy

- 🔄 Switch from 200 OK to 202 Accepted responses for semantic correctness
- 🔄 Add optional request tracking headers (X-Request-Id, X-Status)
- 🔄 Update API documentation and client communication
- 🔄 Test client compatibility with new status codes
- 🔄 Implement gradual rollout with feature flags

### Week 6: Storage Consistency

- 🔄 Consolidate storage updates
- 🔄 Add state synchronization
- 🔄 Implement consistency monitoring

### Week 7: Testing & Validation

- 🧪 End-to-end testing
- 🧪 Performance validation
- 🧪 Regression testing

### Week 8: Observability

- 📊 Metrics implementation
- 📊 Health checks
- 📊 Alerting setup

## Risk Assessment

### High Risk

- **API Breaking Changes** - May require client updates
- **Timing Changes** - Eventual consistency vs immediate updates
- **Data Loss** - During storage update consolidation

### Medium Risk

- **Performance Impact** - Additional event processing overhead
- **Complexity** - More moving parts in the system
- **Testing Coverage** - Ensuring all scenarios are covered

### Low Risk

- **Integration Failures** - Already handled by existing resilience patterns
- **External System Changes** - Pattern is designed to handle this

## Success Criteria

### Functional

- ✅ All commands only instruct external systems
- ✅ All storage updates happen via events
- ✅ All integration publishing triggered by storage updates
- ✅ No race conditions between commands and events
- ✅ Eventual consistency achieved

### Non-Functional

- ✅ Command response time < 100ms (95th percentile)
- ✅ Event processing lag < 50ms (95th percentile)
- ✅ Storage consistency > 99.9%
- ✅ Integration delivery success > 99.5%
- ✅ Zero data loss during migration

### Operational

- ✅ Clear monitoring and alerting
- ✅ Comprehensive testing coverage
- ✅ Updated documentation
- ✅ Team training completed
