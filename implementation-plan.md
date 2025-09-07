# 1. SnapDog2 Architecture Transformation - Implementation Plan

## 1.1. Overview

This document provides a comprehensive, step-by-step implementation plan to transform SnapDog2 from its current fragmented architecture to an enterprise-grade event-driven system with **complete mediator removal**. The plan systematically eliminates command/handler infrastructure while preserving blueprint validation through CommandId/StatusId attributes.

## 1.2. Phase 1: Foundation - Event-Driven State Management (Sprint 1-2)

### 1.2.1. Enhanced State Store Interfaces with Events

**Priority: HIGH** | **Effort: 2 days** | **Risk: LOW**

#### 1.2.1.1. Tasks

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

#### 1.2.1.2. Acceptance Criteria

- [ ] All event interfaces defined with proper typing
- [ ] Event args classes implement proper equality comparison
- [ ] Backward compatibility maintained
- [ ] Unit tests for event args classes

### 1.2.2. Smart State Change Detection

**Priority: HIGH** | **Effort: 3 days** | **Risk: MEDIUM**

#### 1.2.2.1. Tasks

1. **Implement Smart ZoneStateStore**

   ```csharp
   // File: Infrastructure/Storage/InMemoryZoneStateStore.cs
   public class InMemoryZoneStateStore : IZoneStateStore
   {
       public event EventHandler<ZoneStateChangedEventArgs>? ZoneStateChanged;
       public event EventHandler<ZonePlaylistChangedEventArgs>? ZonePlaylistChanged;
       public event EventHandler<ZoneVolumeChangedEventArgs>? ZoneVolumeChanged;

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

#### 1.2.2.2. Acceptance Criteria

- [ ] State stores detect and publish granular changes
- [ ] Performance impact < 5ms per state change
- [ ] No false positive events
- [ ] Integration tests verify event firing

### 1.2.3. Complete StatePublishingService Integration

**Priority: MEDIUM** | **Effort: 2 days** | **Risk: LOW**

#### 1.2.3.1. Tasks

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

#### 1.2.3.2. Acceptance Criteria

- [ ] All state changes trigger appropriate notifications
- [ ] SignalR receives real-time updates
- [ ] MQTT publishes state changes
- [ ] KNX receives state updates
- [ ] No duplicate notifications

## 1.3. Phase 2: Integration Layer Unification (Sprint 3-4)

### 1.3.1. Integration Publisher Abstraction

**Priority: HIGH** | **Effort: 4 days** | **Risk: MEDIUM**

#### 1.3.1.1. Tasks

1. **Create IIntegrationPublisher Interface**

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

#### 1.3.1.2. Acceptance Criteria

- [ ] All integration publishers implement common interface
- [ ] Each publisher handles its protocol-specific formatting
- [ ] Error handling is consistent across publishers
- [ ] Publishers can be enabled/disabled independently

### 1.3.2. Enhanced State Store Interfaces with Events

**Priority: HIGH** | **Effort: 2 days** | **Risk: LOW**

#### 1.3.2.1. Tasks

1. **Enhance IZoneStateStore Interface**

   ```csharp
   // File: Domain/Abstractions/IZoneStateStore.cs
   public interface IZoneStateStore
   {
       // Specific events for granular change detection
       [StatusId(StatusIds.ZoneStateChanged)]
       event EventHandler<ZoneStateChangedEventArgs> ZoneStateChanged;

       [StatusId(StatusIds.PlaylistStatus)]
       event EventHandler<ZonePlaylistChangedEventArgs> ZonePlaylistChanged;

       [StatusId(StatusIds.VolumeStatus)]
       event EventHandler<ZoneVolumeChangedEventArgs> ZoneVolumeChanged;

       [StatusId(StatusIds.TrackStatus)]
       event EventHandler<ZoneTrackChangedEventArgs> ZoneTrackChanged;

       [StatusId(StatusIds.PlaybackState)]
       event EventHandler<ZonePlaybackStateChangedEventArgs> ZonePlaybackStateChanged;

       // Existing methods
       ZoneState? GetZoneState(int zoneIndex);
       void SetZoneState(int zoneIndex, ZoneState state);
       Dictionary<int, ZoneState> GetAllZoneStates();
       void InitializeZoneState(int zoneIndex, ZoneState defaultState);
   }
   ```

2. **Create Event Args Classes with StatusId Attributes**

   ```csharp
   // File: Shared/Events/StateChangeEventArgs.cs
   public record ZoneStateChangedEventArgs(
       int ZoneIndex,
       ZoneState? OldState,
       ZoneState NewState,
       DateTime Timestamp = default
   );

   [StatusId(StatusIds.PlaylistStatus)]
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
       [StatusId(StatusIds.ClientStateChanged)]
       event EventHandler<ClientStateChangedEventArgs> ClientStateChanged;

       [StatusId(StatusIds.ClientVolumeStatus)]
       event EventHandler<ClientVolumeChangedEventArgs> ClientVolumeChanged;

       [StatusId(StatusIds.ClientConnected)]
       event EventHandler<ClientConnectionChangedEventArgs> ClientConnectionChanged;

       // Existing methods...
   }
   ```

#### 1.3.2.2. Acceptance Criteria

- [ ] All event interfaces defined with StatusId attributes
- [ ] Event args classes implement proper equality comparison
- [ ] Backward compatibility maintained
- [ ] Blueprint tests validate StatusId attributes on events
- [ ] Unit tests for event args classes

### 1.3.3. Smart State Change Detection

**Priority: HIGH** | **Effort: 3 days** | **Risk: MEDIUM**

#### 1.3.3.1. Tasks

1. **Implement Smart ZoneStateStore**

   ```csharp
   // File: Infrastructure/Storage/InMemoryZoneStateStore.cs
   public class InMemoryZoneStateStore : IZoneStateStore
   {
       [StatusId(StatusIds.ZoneStateChanged)]
       public event EventHandler<ZoneStateChangedEventArgs>? ZoneStateChanged;

       [StatusId(StatusIds.PlaylistStatus)]
       public event EventHandler<ZonePlaylistChangedEventArgs>? ZonePlaylistChanged;

       [StatusId(StatusIds.VolumeStatus)]
       public event EventHandler<ZoneVolumeChangedEventArgs>? ZoneVolumeChanged;

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

#### 1.3.3.2. Acceptance Criteria

- [ ] State stores detect and publish granular changes with StatusId attributes
- [ ] Performance impact < 5ms per state change
- [ ] No false positive events
- [ ] Blueprint tests validate StatusId attributes on all events
- [ ] Integration tests verify event firing

### 1.3.4. Complete StatePublishingService Integration

**Priority: HIGH** | **Effort: 2 days** | **Risk: LOW**

#### 1.3.4.1. Tasks

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

#### 1.3.4.2. Acceptance Criteria

- [ ] All state changes trigger appropriate notifications
- [ ] SignalR receives real-time updates
- [ ] MQTT publishes state changes
- [ ] KNX receives state updates
- [ ] No duplicate notifications

## 1.4. Phase 2: Integration Layer Unification (Sprint 3-4)

### 1.4.1. Integration Publisher Abstraction

**Priority: HIGH** | **Effort: 4 days** | **Risk: MEDIUM**

#### 1.4.1.1. Tasks

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

#### 1.4.1.2. Acceptance Criteria

- [ ] All integration publishers implement common interface
- [ ] Each publisher handles its protocol-specific formatting
- [ ] Error handling is consistent across publishers
- [ ] Publishers can be enabled/disabled independently

### 1.4.2. Integration Coordinator

**Priority: HIGH** | **Effort: 3 days** | **Risk: MEDIUM**

#### 1.4.2.1. Tasks

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

#### 1.4.2.2. Acceptance Criteria

- [ ] All integrations receive state changes simultaneously
- [ ] Integration failures don't affect other integrations
- [ ] Comprehensive error logging and monitoring
- [ ] Performance metrics for each integration

### 1.4.3. Remove Direct Integration Publishing

**Priority: MEDIUM** | **Effort: 2 days** | **Risk: LOW**

#### 1.4.3.1. Tasks

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

#### 1.4.3.2. Acceptance Criteria

- [ ] No domain services directly publish to integrations
- [ ] All integration publishing goes through IntegrationCoordinator
- [ ] Existing functionality remains unchanged
- [ ] Integration tests pass

## 1.5. Phase 3: Complete Mediator Removal (Sprint 5-6)

### 1.5.1. Controller Layer Simplification

**Priority: MEDIUM** | **Effort: 3 days** | **Risk: LOW**

#### 1.5.1.1. Tasks

1. **Create Method-Level CommandId Attributes**

   ```csharp
   // File: Shared/Attributes/CommandIdAttribute.cs - Extend to support methods
   [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
   public class CommandIdAttribute(string id) : Attribute
   {
       public string Id { get; } = id ?? throw new ArgumentNullException(nameof(id));
   }
   ```

2. **Replace Command Pattern with Direct Service Calls**

   ```csharp
   // File: Api/Controllers/V1/ZonesController.cs
   [ApiController]
   [Route("api/v1/zones")]
   public class ZonesController : ControllerBase
   {
       private readonly IZoneService _zoneService;

       [HttpPost("{zoneIndex}/playlist/{playlistIndex}")]
       [CommandId(CommandIds.SetPlaylist)]  // Method-level attribute for blueprint validation
       public async Task<IActionResult> SetPlaylist(int zoneIndex, int playlistIndex)
       {
           var result = await _zoneService.SetPlaylistAsync(zoneIndex, playlistIndex);
           return result.IsSuccess ? Ok() : BadRequest(result.ErrorMessage);
       }

       [HttpPost("{zoneIndex}/volume")]
       [CommandId(CommandIds.SetZoneVolume)]  // Method-level attribute for blueprint validation
       public async Task<IActionResult> SetVolume(int zoneIndex, [FromBody] int volume)
       {
           var result = await _zoneService.SetVolumeAsync(zoneIndex, volume);
           return result.IsSuccess ? Ok() : BadRequest(result.ErrorMessage);
       }
   }
   ```

3. **Implement IZoneService Interface with CommandId Attributes**

   ```csharp
   // File: Domain/Abstractions/IZoneService.cs
   public interface IZoneService
   {
       [CommandId(CommandIds.SetPlaylist)]
       Task<Result> SetPlaylistAsync(int zoneIndex, int playlistIndex);

       [CommandId(CommandIds.SetZoneVolume)]
       Task<Result> SetVolumeAsync(int zoneIndex, int volume);

       [CommandId(CommandIds.PlayTrack)]
       Task<Result> PlayTrackAsync(int zoneIndex, int trackIndex);

       [CommandId(CommandIds.NextTrack)]
       Task<Result> NextTrackAsync(int zoneIndex);

       [CommandId(CommandIds.PreviousTrack)]
       Task<Result> PreviousTrackAsync(int zoneIndex);
   }
   ```

4. **Implement IClientService Interface with CommandId Attributes**

   ```csharp
   // File: Domain/Abstractions/IClientService.cs
   public interface IClientService
   {
       [CommandId(CommandIds.SetClientVolume)]
       Task<Result> SetVolumeAsync(int clientIndex, int volume);

       [CommandId(CommandIds.SetClientMute)]
       Task<Result> SetMuteAsync(int clientIndex, bool muted);

       [CommandId(CommandIds.AssignClientToZone)]
       Task<Result> AssignToZoneAsync(int clientIndex, int zoneIndex);
   }
   ```

#### 1.5.1.2. Acceptance Criteria

- [ ] Controllers use direct service calls instead of mediator
- [ ] CommandId attributes preserved on service methods and controller actions
- [ ] API response times improve by 50-60%
- [ ] Stack traces are cleaner and easier to debug
- [ ] Blueprint tests validate CommandId attributes on service methods

### 1.5.2. Remove Command Infrastructure

**Priority: LOW** | **Effort: 2 days** | **Risk: LOW**

#### 1.5.2.1. Tasks

1. **Delete Command/Handler Infrastructure**

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

   # Remove command factories
   rm -rf SnapDog2/Server/Shared/Factories/CommandFactory.cs

   # Remove mediator configuration (keep notification parts)
   # Edit: Application/Extensions/DependencyInjection/MediatorConfiguration.cs
   # Remove: Command and handler registrations, keep notification registrations
   ```

2. **Remove Cortex.Mediator Package References**

   ```xml
   <!-- Remove from SnapDog2.csproj -->
   <!-- <PackageReference Include="Cortex.Mediator" Version="x.x.x" /> -->

   <!-- Keep only notification-related mediator usage -->
   <!-- Preserve INotification, INotificationHandler<T> for domain events -->
   ```

3. **Update Dependency Injection**

   ```csharp
   // File: Program.cs - Remove command registrations
   // Remove: builder.Services.AddMediatorCommands();
   // Keep: builder.Services.AddMediatorNotifications();

   // Add direct service registrations
   builder.Services.AddScoped<IZoneService, ZoneService>();
   builder.Services.AddScoped<IClientService, ClientService>();
   ```

4. **Update Blueprint Tests**

   ```csharp
   // File: SnapDog2.Tests/Architecture/BlueprintValidationTests.cs
   [Test]
   public void AllCommandIds_ShouldHaveCorrespondingServiceMethod()
   {
       var commandIds = typeof(CommandIds).GetFields();
       var serviceTypes = new[] { typeof(IZoneService), typeof(IClientService) };

       foreach (var commandId in commandIds)
       {
           var commandIdValue = commandId.GetValue(null)?.ToString();
           var methodExists = serviceTypes
               .SelectMany(t => t.GetMethods())
               .Any(m => m.GetCustomAttribute<CommandIdAttribute>()?.Id == commandIdValue);

           Assert.IsTrue(methodExists, $"No service method found for CommandId: {commandId.Name}");
       }
   }

   [Test]
   public void AllStatusIds_ShouldHaveCorrespondingNotificationClass()
   {
       var statusIds = typeof(StatusIds).GetFields();
       var notificationTypes = Assembly.GetExecutingAssembly()
           .GetTypes()
           .Where(t => t.GetInterfaces().Contains(typeof(INotification)));

       foreach (var statusId in statusIds)
       {
           var statusIdValue = statusId.GetValue(null)?.ToString();
           var notificationExists = notificationTypes
               .Any(t => t.GetCustomAttribute<StatusIdAttribute>()?.Id == statusIdValue);

           Assert.IsTrue(notificationExists, $"No notification class found for StatusId: {statusId.Name}");
       }
   }
   ```

5. **Clean Up Integration Services**

   ```csharp
   // File: Infrastructure/Integrations/Knx/KnxService.cs
   // Remove: All command-related using statements
   // Remove: GetHandler<T> method
   // Remove: ExecuteCommandAsync method (replaced with direct service calls)

   // File: Infrastructure/Integrations/Mqtt/MqttService.cs
   // Remove: All command-related using statements
   // Remove: GetHandler<T> method
   // Remove: ExecuteCommandAsync method (replaced with direct service calls)
   ```

6. **Preserve Domain Events**

   ```csharp
   // Keep these files and registrations:
   // - INotification and INotificationHandler<T> interfaces
   // - All notification classes with [StatusId] attributes
   // - Notification registration in DI container
   // - StatePublishingService notification publishing
   ```

#### 1.5.2.2. Files to Delete

**Command Classes** (Complete removal):

```
SnapDog2/Server/Zones/Commands/Playlist/SetPlaylistCommand.cs
SnapDog2/Server/Zones/Commands/Volume/SetZoneVolumeCommand.cs
SnapDog2/Server/Zones/Commands/Track/SetTrackCommand.cs
SnapDog2/Server/Zones/Commands/Playback/PlayCommand.cs
SnapDog2/Server/Clients/Commands/Volume/SetClientVolumeCommand.cs
SnapDog2/Server/Clients/Commands/Config/AssignClientToZoneCommand.cs
... (all command classes)
```

**Handler Classes** (Complete removal):

```
SnapDog2/Server/Zones/Handlers/SetPlaylistCommandHandler.cs
SnapDog2/Server/Zones/Handlers/SetZoneVolumeCommandHandler.cs
SnapDog2/Server/Zones/Handlers/SetTrackCommandHandler.cs
SnapDog2/Server/Clients/Handlers/SetClientVolumeCommandHandler.cs
... (all handler classes)
```

**Factory Classes** (Complete removal):

```
SnapDog2/Server/Shared/Factories/CommandFactory.cs
```

#### 1.5.2.3. Files to Preserve

**Registry Classes** (Essential for blueprint validation):

```
SnapDog2/Shared/Constants/CommandIds.cs
SnapDog2/Shared/Constants/StatusIds.cs
SnapDog2/Shared/Attributes/CommandIdAttribute.cs (extend to support methods)
SnapDog2/Shared/Attributes/StatusIdAttribute.cs (for integration validation)
```

**SignalR Hub Notifications** (Client contracts - NOT mediator):

```
SnapDog2/Api/Hubs/Notifications/ZoneVolumeChangedNotification.cs
SnapDog2/Api/Hubs/Notifications/ZonePlaylistChangedNotification.cs
SnapDog2/Api/Hubs/Notifications/ClientVolumeChangedNotification.cs
... (all SignalR client notification contracts)
```

#### 1.5.2.4. Files to Delete (Complete Mediator Removal)

**All Mediator Infrastructure**:

```
# Remove Cortex.Mediator package entirely
SnapDog2/Application/Services/StatePublishingService.cs (replaced by IntegrationCoordinator)
SnapDog2/Application/Extensions/DependencyInjection/MediatorConfiguration.cs
```

**Server-Side Mediator Notifications** (NOT SignalR):

```
SnapDog2/Server/Zones/Notifications/ZoneNotifications.cs
SnapDog2/Server/Clients/Notifications/ClientNotifications.cs
SnapDog2/Application/Notifications/PlaylistCountStatusNotification.cs
... (all server-side mediator notification classes)
```

#### 1.5.2.5. New Architecture Flow

```
StateStore Events → IntegrationCoordinator → Integrations:
                                          ├── MQTT Publisher
                                          ├── KNX Publisher
                                          └── SignalR Hub (sends hub notifications to clients)
```

**Blueprint Validation**:

- **CommandIds**: Validated on service methods with `[CommandId]` attributes
- **StatusIds**: Validated by checking IntegrationCoordinator publishes correct status IDs

#### 1.5.2.6. Acceptance Criteria

- [ ] All command/handler infrastructure removed (500+ files deleted)
- [ ] **Complete Cortex.Mediator removal** (package uninstalled)
- [ ] Server-side notification classes removed (mediator notifications only)
- [ ] **SignalR hub notifications preserved** (client contracts)
- [ ] StatePublishingService removed (replaced by IntegrationCoordinator)
- [ ] Blueprint tests validate service methods with CommandId attributes
- [ ] Blueprint tests validate IntegrationCoordinator publishes StatusIds
- [ ] Direct StateStore → IntegrationCoordinator → Integrations flow working
- [ ] SignalR clients still receive real-time updates
- [ ] Build: 0 errors, 0 warnings
- [ ] All integration tests pass
- [ ] **Code reduction: ~4,000+ lines removed** (commands + handlers + server notifications)

## 1.6. Phase 4: Quality Assurance & Monitoring (Sprint 7)

### 1.6.1. Architectural Tests

**Priority: HIGH** | **Effort: 2 days** | **Risk: LOW**

#### 1.6.1.1. Tasks

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

### 1.6.2. Performance Monitoring

**Priority: MEDIUM** | **Effort: 2 days** | **Risk: LOW**

#### 1.6.2.1. Tasks

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

## 1.7. Implementation Timeline

### 1.7.1. Sprint 1 (Week 1-2): Foundation

- **Days 1-2**: Enhanced State Store Interfaces with StatusId attributes
- **Days 3-5**: Smart State Change Detection with events
- **Days 6-7**: Complete StatePublishingService Integration
- **Days 8-10**: Testing and Bug Fixes

### 1.7.2. Sprint 2 (Week 3-4): Integration Layer

- **Days 1-4**: Integration Publisher Abstraction with StatusId attributes
- **Days 5-7**: Integration Coordinator
- **Days 8-9**: Remove Direct Integration Publishing
- **Day 10**: Integration Testing

### 1.7.3. Sprint 3 (Week 5-6): Complete Mediator Removal

- **Days 1-3**: Integration Services Migration (IKnxService, IMqttService)
- **Days 4-6**: Controller Layer Simplification with CommandId preservation
- **Days 7-8**: Remove Command Infrastructure
- **Days 9-10**: Migration and Testing

### 1.7.4. Sprint 4 (Week 7): Quality Assurance

- **Days 1-2**: Architectural Tests and Blueprint Validation
- **Days 3-4**: Performance Monitoring
- **Days 5**: Documentation Updates
- **Days 6-7**: Final Integration Testing

## 1.8. Risk Mitigation

### 1.8.1. High-Risk Items

1. **State Change Detection Performance**
   - **Mitigation**: Implement with benchmarks, use efficient comparison algorithms
   - **Fallback**: Simplified change detection if performance issues arise

2. **Integration Coordinator Reliability**
   - **Mitigation**: Comprehensive error handling, circuit breaker pattern
   - **Fallback**: Temporary direct publishing if coordinator fails

3. **Blueprint Validation Preservation**
   - **Mitigation**: Maintain CommandId/StatusId attributes throughout migration
   - **Fallback**: Restore attribute validation if tests fail

### 1.8.2. Success Metrics

- [ ] **Architecture**: 100% of state changes trigger appropriate events with StatusId attributes
- [ ] **Performance**: 50-60% improvement in API response times
- [ ] **Integration**: 0% integration publishing failures under normal load
- [ ] **Blueprint Validation**: 100% CommandId/StatusId coverage in service methods and StateStore events
- [ ] **Code Quality**: 90% reduction in debugging time for state-related issues
- [ ] **Test Coverage**: 100% test coverage for new event-driven components
- [ ] **Mediator Removal**: Complete elimination of command/handler infrastructure while preserving domain events

## 1.9. Final Architecture Achievement

- **Layer Reduction**: 7+ layers → 3 layers (API → Service → StateStore)
- **Performance**: 50-60% improvement through direct calls
- **Code Reduction**: 3,000+ lines → 800 lines (command/handler elimination)
- **Compatibility**: 100% API contract preservation
- **Events**: Full event-driven architecture with StateStore as SSoT
- **Blueprint Preservation**: CommandId/StatusId attributes maintained for validation
- **Integration Services**: IKnxService and IMqttService migrated to direct service calls

## 1.10. Risk Mitigation

### 1.10.1. High-Risk Items

1. **State Change Detection Performance**
   - **Mitigation**: Implement with benchmarks, use efficient comparison algorithms
   - **Fallback**: Simplified change detection if performance issues arise

2. **Integration Coordinator Reliability**
   - **Mitigation**: Comprehensive error handling, circuit breaker pattern
   - **Fallback**: Temporary direct publishing if coordinator fails

3. **Mediator Migration Complexity**
   - **Mitigation**: Gradual migration, maintain backward compatibility
   - **Fallback**: Keep existing mediator usage if migration proves too complex

### 1.10.2. Success Metrics

- [ ] **Production Fix**: Docker container startup success (Phase 0)
- [ ] **Performance**: 50-60% improvement in API response times
- [ ] **Architecture**: 100% of state changes trigger appropriate events with StatusId attributes
- [ ] **Integration**: 0% integration publishing failures under normal load
- [ ] **Blueprint Validation**: 100% CommandId/StatusId coverage in service methods and StateStore events
- [ ] **Code Quality**: 90% reduction in debugging time for state-related issues
- [ ] **Test Coverage**: 100% test coverage for new event-driven components
- [ ] **Mediator Removal**: Complete elimination of command/handler infrastructure while preserving domain events

## 1.11. Final Architecture Achievement

- **Layer Reduction**: 7+ layers → 3 layers (API → Service → StateStore)
- **Performance**: 50-60% improvement through direct calls
- **Code Reduction**: 3,000+ lines → 800 lines (command/handler elimination)
- **Compatibility**: 100% API contract preservation
- **Events**: Full event-driven architecture with StateStore as SSoT
- **Blueprint Preservation**: CommandId/StatusId attributes maintained for validation
- **Production Stability**: Critical IKnxService issue resolved immediately

## 1.12. Risk Mitigation

- **Rollback Plan**: Git commits for each phase
- **Testing**: Build verification after each step
- **Monitoring**: Docker container health checks
- **Validation**: API contract testing
