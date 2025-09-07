# SnapDog2 Architecture Transformation - Implementation Plan

## Overview

This document provides a comprehensive, step-by-step implementation plan to transform SnapDog2 from its current fragmented architecture to an enterprise-grade event-driven system. The plan is divided into phases that can be executed incrementally without breaking existing functionality.

## Phase 1: Foundation - Event-Driven State Management (Sprint 1-2)

### 1.1 Enhanced State Store Interfaces

**Priority: CRITICAL** | **Effort: 2 days** | **Risk: LOW**

#### Tasks:
1. **Enhance IZoneStateStore Interface**
   ```csharp
   // File: Domain/Abstractions/IZoneStateStore.cs
   public interface IZoneStateStore
   {
       // Specific events for granular change detection
       event EventHandler<ZoneStateChangedEventArgs> ZoneStateChanged;
       event EventHandler<ZonePlaylistChangedEventArgs> ZonePlaylistChanged;
       event EventHandler<ZoneVolumeChangedEventArgs> ZoneVolumeChanged;
       event EventHandler<ZoneTrackChangedEventArgs> ZoneTrackChanged;
       event EventHandler<ZonePlaybackStateChangedEventArgs> ZonePlaybackStateChanged;
       
       // Existing methods
       ZoneState? GetZoneState(int zoneIndex);
       void SetZoneState(int zoneIndex, ZoneState state);
       Dictionary<int, ZoneState> GetAllZoneStates();
       void InitializeZoneState(int zoneIndex, ZoneState defaultState);
   }
   ```

2. **Create Event Args Classes**
   ```csharp
   // File: Shared/Events/StateChangeEventArgs.cs
   public record ZoneStateChangedEventArgs(
       int ZoneIndex, 
       ZoneState? OldState, 
       ZoneState NewState,
       DateTime Timestamp = default
   );
   
   public record ZonePlaylistChangedEventArgs(
       int ZoneIndex,
       PlaylistInfo? OldPlaylist,
       PlaylistInfo? NewPlaylist,
       DateTime Timestamp = default
   ) : ZoneStateChangedEventArgs(ZoneIndex, null, null, Timestamp);
   ```

3. **Enhance IClientStateStore Interface**
   ```csharp
   // File: Domain/Abstractions/IClientStateStore.cs
   public interface IClientStateStore
   {
       event EventHandler<ClientStateChangedEventArgs> ClientStateChanged;
       event EventHandler<ClientVolumeChangedEventArgs> ClientVolumeChanged;
       event EventHandler<ClientConnectionChangedEventArgs> ClientConnectionChanged;
       
       // Existing methods...
   }
   ```

#### Acceptance Criteria:
- [ ] All event interfaces defined with proper typing
- [ ] Event args classes implement proper equality comparison
- [ ] Backward compatibility maintained
- [ ] Unit tests for event args classes

### 1.2 Smart State Change Detection

**Priority: CRITICAL** | **Effort: 3 days** | **Risk: MEDIUM**

#### Tasks:
1. **Implement Smart ZoneStateStore**
   ```csharp
   // File: Infrastructure/Storage/InMemoryZoneStateStore.cs
   public class InMemoryZoneStateStore : IZoneStateStore
   {
       public void SetZoneState(int zoneIndex, ZoneState newState)
       {
           var oldState = GetZoneState(zoneIndex);
           _states[zoneIndex] = newState;
           
           // Detect specific changes and fire targeted events
           DetectAndPublishChanges(zoneIndex, oldState, newState);
           
           // Always fire general state change
           ZoneStateChanged?.Invoke(this, new ZoneStateChangedEventArgs(
               zoneIndex, oldState, newState));
       }
       
       private void DetectAndPublishChanges(int zoneIndex, ZoneState? oldState, ZoneState newState)
       {
           // Playlist changes
           if (oldState?.Playlist?.Index != newState.Playlist?.Index)
           {
               ZonePlaylistChanged?.Invoke(this, new ZonePlaylistChangedEventArgs(
                   zoneIndex, oldState?.Playlist, newState.Playlist));
           }
           
           // Volume changes
           if (oldState?.Volume != newState.Volume)
           {
               ZoneVolumeChanged?.Invoke(this, new ZoneVolumeChangedEventArgs(
                   zoneIndex, oldState?.Volume ?? 0, newState.Volume));
           }
           
           // Track changes
           if (oldState?.Track?.Index != newState.Track?.Index)
           {
               ZoneTrackChanged?.Invoke(this, new ZoneTrackChangedEventArgs(
                   zoneIndex, oldState?.Track, newState.Track));
           }
           
           // Playback state changes
           if (oldState?.PlaybackState != newState.PlaybackState)
           {
               ZonePlaybackStateChanged?.Invoke(this, new ZonePlaybackStateChangedEventArgs(
                   zoneIndex, oldState?.PlaybackState ?? PlaybackState.Stopped, newState.PlaybackState));
           }
       }
   }
   ```

2. **Implement Smart ClientStateStore**
   ```csharp
   // File: Infrastructure/Storage/InMemoryClientStateStore.cs
   public class InMemoryClientStateStore : IClientStateStore
   {
       // Similar implementation with client-specific change detection
   }
   ```

#### Acceptance Criteria:
- [ ] State stores detect and publish granular changes
- [ ] Performance impact < 5ms per state change
- [ ] No false positive events
- [ ] Integration tests verify event firing

### 1.3 Complete StatePublishingService Integration

**Priority: HIGH** | **Effort: 2 days** | **Risk: LOW**

#### Tasks:
1. **Wire StatePublishingService to Events**
   ```csharp
   // File: Application/Services/StatePublishingService.cs
   public partial class StatePublishingService : BackgroundService
   {
       public StatePublishingService(
           IServiceScopeFactory serviceScopeFactory,
           ILogger<StatePublishingService> logger,
           SnapDogConfiguration configuration,
           IZoneStateStore zoneStateStore,
           IClientStateStore clientStateStore)
       {
           // Subscribe to all state change events
           zoneStateStore.ZonePlaylistChanged += OnZonePlaylistChanged;
           zoneStateStore.ZoneVolumeChanged += OnZoneVolumeChanged;
           zoneStateStore.ZoneTrackChanged += OnZoneTrackChanged;
           zoneStateStore.ZonePlaybackStateChanged += OnZonePlaybackStateChanged;
           
           clientStateStore.ClientVolumeChanged += OnClientVolumeChanged;
           clientStateStore.ClientConnectionChanged += OnClientConnectionChanged;
       }
       
       private async void OnZonePlaylistChanged(object? sender, ZonePlaylistChangedEventArgs e)
       {
           using var scope = _serviceScopeFactory.CreateScope();
           var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
           
           await mediator.PublishAsync(new ZonePlaylistChangedNotification(
               e.ZoneIndex, e.NewPlaylist));
       }
       
       // Similar handlers for other events...
   }
   ```

#### Acceptance Criteria:
- [ ] All state changes trigger appropriate notifications
- [ ] SignalR receives real-time updates
- [ ] MQTT publishes state changes
- [ ] KNX receives state updates
- [ ] No duplicate notifications

## Phase 2: Integration Layer Unification (Sprint 3-4)

### 2.1 Integration Publisher Abstraction

**Priority: HIGH** | **Effort: 4 days** | **Risk: MEDIUM**

#### Tasks:
1. **Create IIntegrationPublisher Interface**
   ```csharp
   // File: Domain/Abstractions/IIntegrationPublisher.cs
   public interface IIntegrationPublisher
   {
       string Name { get; }
       bool IsEnabled { get; }
       
       Task PublishZoneStateAsync(int zoneIndex, ZoneState state, CancellationToken cancellationToken = default);
       Task PublishClientStateAsync(int clientIndex, ClientState state, CancellationToken cancellationToken = default);
       Task PublishSystemStatusAsync(SystemStatus status, CancellationToken cancellationToken = default);
       
       // Granular publishing methods
       Task PublishZonePlaylistChangedAsync(int zoneIndex, PlaylistInfo? playlist, CancellationToken cancellationToken = default);
       Task PublishZoneVolumeChangedAsync(int zoneIndex, int volume, CancellationToken cancellationToken = default);
       Task PublishZoneTrackChangedAsync(int zoneIndex, TrackInfo? track, CancellationToken cancellationToken = default);
   }
   ```

2. **Implement MQTT Publisher**
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

3. **Implement KNX Publisher**
   ```csharp
   // File: Infrastructure/Integrations/Knx/KnxIntegrationPublisher.cs
   public class KnxIntegrationPublisher : IIntegrationPublisher
   {
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

4. **Implement SignalR Publisher**
   ```csharp
   // File: Infrastructure/Integrations/SignalR/SignalRIntegrationPublisher.cs
   public class SignalRIntegrationPublisher : IIntegrationPublisher
   {
       public async Task PublishZonePlaylistChangedAsync(int zoneIndex, PlaylistInfo? playlist, CancellationToken cancellationToken = default)
       {
           await _hubContext.Clients.All.SendAsync("ZonePlaylistChanged", zoneIndex, playlist, cancellationToken);
       }
   }
   ```

#### Acceptance Criteria:
- [ ] All integration publishers implement common interface
- [ ] Each publisher handles its protocol-specific formatting
- [ ] Error handling is consistent across publishers
- [ ] Publishers can be enabled/disabled independently

### 2.2 Integration Coordinator

**Priority: HIGH** | **Effort: 3 days** | **Risk: MEDIUM**

#### Tasks:
1. **Create IntegrationCoordinator**
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
           // ... other subscriptions
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
               _logger.LogDebug("Successfully published {EventType} to {Publisher}", eventType, publisherName);
           }
           catch (Exception ex)
           {
               _logger.LogError(ex, "Failed to publish {EventType} to {Publisher}", eventType, publisherName);
               // Don't rethrow - one integration failure shouldn't affect others
           }
       }
   }
   ```

#### Acceptance Criteria:
- [ ] All integrations receive state changes simultaneously
- [ ] Integration failures don't affect other integrations
- [ ] Comprehensive error logging and monitoring
- [ ] Performance metrics for each integration

### 2.3 Remove Direct Integration Publishing

**Priority: MEDIUM** | **Effort: 2 days** | **Risk: LOW**

#### Tasks:
1. **Remove Direct Publishing from Domain Services**
   - Remove `PublishTrackStatusAsync` calls from ZoneManager
   - Remove `PublishPlaylistStatusAsync` calls from ZoneManager
   - Remove direct MQTT/KNX publishing from handlers

2. **Update Dependency Injection**
   ```csharp
   // File: Program.cs
   builder.Services.AddSingleton<IIntegrationPublisher, MqttIntegrationPublisher>();
   builder.Services.AddSingleton<IIntegrationPublisher, KnxIntegrationPublisher>();
   builder.Services.AddSingleton<IIntegrationPublisher, SignalRIntegrationPublisher>();
   builder.Services.AddHostedService<IntegrationCoordinator>();
   ```

#### Acceptance Criteria:
- [ ] No domain services directly publish to integrations
- [ ] All integration publishing goes through IntegrationCoordinator
- [ ] Existing functionality remains unchanged
- [ ] Integration tests pass

## Phase 3: Command Layer Simplification (Sprint 5-6)

### 3.1 Mediator Usage Analysis

**Priority: MEDIUM** | **Effort: 3 days** | **Risk: LOW**

#### Tasks:
1. **Audit Current Mediator Usage**
   ```bash
   # Create analysis script
   find . -name "*.cs" -exec grep -l "ICommand\|IQuery\|INotification" {} \; > mediator-usage.txt
   ```

2. **Categorize Operations**
   - **Keep Mediator**: Complex workflows, cross-cutting concerns
   - **Convert to Direct**: Simple CRUD operations, single-step actions
   - **Remove Entirely**: Unnecessary abstractions

3. **Create Migration Plan**
   ```markdown
   ## Operations to Keep with Mediator
   - InitializeSystemCommand (complex startup workflow)
   - SynchronizeAllZonesCommand (multi-zone coordination)
   - BackupSystemStateCommand (cross-cutting operation)
   
   ## Operations to Convert to Direct Service Calls
   - SetZoneVolumeCommand → IZoneService.SetVolumeAsync()
   - SetPlaylistCommand → IZoneService.SetPlaylistAsync()
   - PlayTrackCommand → IZoneService.PlayTrackAsync()
   ```

### 3.2 Direct Service Implementation

**Priority: MEDIUM** | **Effort: 4 days** | **Risk: MEDIUM**

#### Tasks:
1. **Create Simplified Controllers**
   ```csharp
   // File: Api/Controllers/V1/ZonesController.cs
   [ApiController]
   [Route("api/v1/zones")]
   public class ZonesController : ControllerBase
   {
       private readonly IZoneService _zoneService;
       
       [HttpPost("{zoneIndex}/playlist/{playlistIndex}")]
       public async Task<IActionResult> SetPlaylist(int zoneIndex, int playlistIndex)
       {
           var result = await _zoneService.SetPlaylistAsync(zoneIndex, playlistIndex);
           return result.IsSuccess ? Ok() : BadRequest(result.ErrorMessage);
       }
       
       [HttpPost("{zoneIndex}/volume")]
       public async Task<IActionResult> SetVolume(int zoneIndex, [FromBody] int volume)
       {
           var result = await _zoneService.SetVolumeAsync(zoneIndex, volume);
           return result.IsSuccess ? Ok() : BadRequest(result.ErrorMessage);
       }
   }
   ```

2. **Implement IZoneService Interface**
   ```csharp
   // File: Domain/Abstractions/IZoneService.cs
   public interface IZoneService
   {
       Task<Result> SetPlaylistAsync(int zoneIndex, int playlistIndex);
       Task<Result> SetVolumeAsync(int zoneIndex, int volume);
       Task<Result> PlayTrackAsync(int zoneIndex, int trackIndex);
       Task<Result> NextTrackAsync(int zoneIndex);
       Task<Result> PreviousTrackAsync(int zoneIndex);
       // ... other simple operations
   }
   ```

#### Acceptance Criteria:
- [ ] Simple operations use direct service calls
- [ ] Complex workflows still use mediator
- [ ] API response times improve by 20-30%
- [ ] Stack traces are cleaner and easier to debug

## Phase 4: Quality Assurance & Monitoring (Sprint 7)

### 4.1 Architectural Tests

**Priority: HIGH** | **Effort: 2 days** | **Risk: LOW**

#### Tasks:
1. **Create Architectural Rules Tests**
   ```csharp
   // File: SnapDog2.Tests/Architecture/ArchitecturalRulesTests.cs
   [Test]
   public void DomainServices_ShouldNotDirectlyPublishToIntegrations()
   {
       var domainAssembly = typeof(ZoneManager).Assembly;
       var integrationTypes = new[] { "MqttService", "KnxService", "SignalRHub" };
       
       var violations = domainAssembly.GetTypes()
           .Where(t => t.Namespace?.Contains("Domain") == true)
           .SelectMany(t => t.GetMethods())
           .Where(m => integrationTypes.Any(it => 
               m.GetParameters().Any(p => p.ParameterType.Name.Contains(it))))
           .ToList();
       
       Assert.That(violations, Is.Empty, 
           "Domain services should not directly depend on integration services");
   }
   
   [Test]
   public void StateStores_ShouldBeOnlySourceOfTruth()
   {
       // Verify no other components maintain independent state
   }
   ```

2. **Integration Tests for Event Flow**
   ```csharp
   [Test]
   public async Task WhenPlaylistChanges_AllIntegrationsShouldReceiveUpdate()
   {
       // Arrange
       var mockMqtt = new Mock<IIntegrationPublisher>();
       var mockKnx = new Mock<IIntegrationPublisher>();
       var mockSignalR = new Mock<IIntegrationPublisher>();
       
       // Act
       await _zoneService.SetPlaylistAsync(1, 2);
       
       // Assert
       mockMqtt.Verify(m => m.PublishZonePlaylistChangedAsync(1, It.IsAny<PlaylistInfo>(), default), Times.Once);
       mockKnx.Verify(k => k.PublishZonePlaylistChangedAsync(1, It.IsAny<PlaylistInfo>(), default), Times.Once);
       mockSignalR.Verify(s => s.PublishZonePlaylistChangedAsync(1, It.IsAny<PlaylistInfo>(), default), Times.Once);
   }
   ```

### 4.2 Performance Monitoring

**Priority: MEDIUM** | **Effort: 2 days** | **Risk: LOW**

#### Tasks:
1. **Add Performance Metrics**
   ```csharp
   // File: Application/Services/PerformanceMonitoringService.cs
   public class PerformanceMonitoringService
   {
       private readonly IMetrics _metrics;
       
       public void RecordStateChangeLatency(string changeType, TimeSpan duration)
       {
           _metrics.Measure.Timer.Time(
               MetricNames.StateChangeLatency, 
               duration, 
               new MetricTags("change_type", changeType));
       }
       
       public void RecordIntegrationPublishSuccess(string integration, string eventType)
       {
           _metrics.Measure.Counter.Increment(
               MetricNames.IntegrationPublishSuccess,
               new MetricTags("integration", integration, "event_type", eventType));
       }
   }
   ```

2. **Add Health Checks**
   ```csharp
   // File: Application/HealthChecks/IntegrationHealthCheck.cs
   public class IntegrationHealthCheck : IHealthCheck
   {
       public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
       {
           var results = new Dictionary<string, object>();
           
           foreach (var publisher in _publishers)
           {
               results[publisher.Name] = publisher.IsEnabled ? "Healthy" : "Unhealthy";
           }
           
           var allHealthy = _publishers.All(p => p.IsEnabled);
           return allHealthy 
               ? HealthCheckResult.Healthy("All integrations are healthy", results)
               : HealthCheckResult.Degraded("Some integrations are unhealthy", data: results);
       }
   }
   ```

## Implementation Timeline

### Sprint 1 (Week 1-2): Foundation
- **Days 1-2**: Enhanced State Store Interfaces
- **Days 3-5**: Smart State Change Detection  
- **Days 6-7**: Complete StatePublishingService Integration
- **Days 8-10**: Testing and Bug Fixes

### Sprint 2 (Week 3-4): Integration Layer
- **Days 1-4**: Integration Publisher Abstraction
- **Days 5-7**: Integration Coordinator
- **Days 8-9**: Remove Direct Integration Publishing
- **Day 10**: Integration Testing

### Sprint 3 (Week 5-6): Command Simplification
- **Days 1-3**: Mediator Usage Analysis
- **Days 4-7**: Direct Service Implementation
- **Days 8-10**: Migration and Testing

### Sprint 4 (Week 7): Quality Assurance
- **Days 1-2**: Architectural Tests
- **Days 3-4**: Performance Monitoring
- **Days 5**: Documentation Updates
- **Days 6-7**: Final Integration Testing

## Risk Mitigation

### High-Risk Items:
1. **State Change Detection Performance**
   - **Mitigation**: Implement with benchmarks, use efficient comparison algorithms
   - **Fallback**: Simplified change detection if performance issues arise

2. **Integration Coordinator Reliability**
   - **Mitigation**: Comprehensive error handling, circuit breaker pattern
   - **Fallback**: Temporary direct publishing if coordinator fails

3. **Mediator Migration Complexity**
   - **Mitigation**: Gradual migration, maintain backward compatibility
   - **Fallback**: Keep existing mediator usage if migration proves too complex

### Success Metrics:
- [ ] 100% of state changes trigger appropriate events
- [ ] 0% integration publishing failures under normal load
- [ ] 20-30% improvement in API response times
- [ ] 90% reduction in debugging time for state-related issues
- [ ] 100% test coverage for new event-driven components

## Rollback Strategy

Each phase includes rollback procedures:
1. **Feature flags** for new event system
2. **Parallel running** of old and new integration publishing
3. **Database migrations** are reversible
4. **Configuration switches** to revert to old behavior

This ensures that any phase can be rolled back without affecting system stability.
