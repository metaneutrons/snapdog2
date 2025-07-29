# 4. Core Components & State Management

## 4.1. Core Structure Overview

SnapDog2, while residing within a single primary project (`SnapDog2.csproj`), is architecturally designed with distinct logical layers achieved through disciplined folder and namespace organization (`/Core`, `/Server`, `/Infrastructure`, `/Api`, `/Worker`). The application's execution lifecycle and core service orchestration are managed by the **.NET Generic Host**, configured and launched from `/Worker/Program.cs`.

Key structural components and their roles within the logical layers:

1. **`/Worker` (Composition Root & Host):**
    * **`Program.cs`**: The application entry point. Configures the `WebApplicationBuilder` (or `HostBuilder`), sets up logging (Serilog), registers services via Dependency Injection (DI) using extension methods from other layers, builds the host, performs crucial **Configuration Validation** (Sec 10.3), and finally runs the application (`host.Run()`).
    * **Hosted Service(s) (e.g., `/Worker/SnapDogWorker.cs`)**: Implements `IHostedService` (typically inheriting from `BackgroundService`). Responsible for orchestrating the startup sequence of core infrastructure services (calling their `InitializeAsync` methods in the correct order) and potentially running long-running background tasks (like periodic checks, though none are defined initially). It manages graceful shutdown.
    * **`/Worker/DI/`**: Contains extension methods (`AddCoreServices`, `AddInfrastructureServices`, `AddCommandProcessing`, `AddApiServices`, etc.) used by `Program.cs` to register dependencies with the DI container, promoting modularity in startup configuration.

2. **`/Infrastructure` (External Interactions & Implementation):**
    * **Service Implementations (e.g., `/Infrastructure/Snapcast/SnapcastService.cs`)**: Concrete classes implementing `/Core` abstractions (`ISnapcastService`, `IKnxService`, `IMqttService`, `ISubsonicService`, `IMediaPlayerService`). These classes contain the logic specific to interacting with external libraries (`Sturd.SnapcastNet`, `Knx.Falcon.Sdk`, `MQTTnet`, `SubSonicMedia`, `LibVLCSharp`) and systems. They handle protocol details, resilience (Polly), error translation (to `Result` pattern), and state synchronization (updating `SnapcastStateRepository`, publishing MediatR notifications).
    * **State Repositories (e.g., `/Infrastructure/Snapcast/SnapcastStateRepository.cs`)**: Responsible for holding the latest known *raw* state received from certain external systems, primarily the Snapcast server state using `Sturd.SnapcastNet` models. Ensures thread-safe access to this in-memory representation.
    * **Helpers (e.g., `/Infrastructure/EnvConfigHelper.cs`, `/Infrastructure/Resilience/ResiliencePolicies.cs`)**: Utility classes supporting infrastructure concerns.

3. **`/Server` (Application & Domain Logic):**
    * **MediatR Handlers (`/Server/Features/...`)**: Contain the core application logic triggered by Commands and Queries. They orchestrate interactions between Core Managers/Services and Infrastructure *Abstractions*. Should remain thin and focused on coordinating actions for a specific use case. Organized by feature (e.g., Zones, Clients).
    * **Core Managers (`/Server/Managers/...`)**: Classes like `ZoneManager`, `ClientManager`, `PlaylistManager` that encapsulate the business logic and rules for managing collections or higher-level concepts related to zones, clients, and playlists *within the SnapDog2 context*. They manage the mapping between SnapDog2's view of the world (e.g., internal Zone/Client IDs) and external system identifiers (Snapcast Group/Client IDs). They hold and manage SnapDog2's derived state (`ClientState`, `ZoneState`). They depend on `/Core` abstractions.
    * **Domain Services (e.g., `/Server/Features/Zones/ZoneService.cs`)**: Represents the logic and state management for a single instance of a core domain entity, like an individual audio zone. Holds the `ZoneState` record.
    * **MediatR Messages (`/Server/.../Commands`, `/Server/.../Queries`, `/Server/Notifications`)**: Definitions of the Commands, Queries, and Notifications used for internal communication via the MediatR bus.
    * **Validation (`/Server/.../Validators`)**: FluentValidation classes for MediatR commands.
    * **Behaviors (`/Server/Behaviors`)**: MediatR pipeline behaviors (Logging, Validation, Performance).

4. **`/Core` (Foundation):**
    * **Abstractions (`/Core/Abstractions`)**: Interface definitions (`IZoneManager`, `ISnapcastService`, `ISnapcastStateRepository`, etc.) defining the contracts implemented by `/Infrastructure` or `/Server` layers.
    * **Models (`/Core/Models`)**: Immutable `record` definitions for SnapDog2's domain state (`ZoneState`, `ClientState`), DTOs (if distinct from state models), shared patterns (`Result`, `ErrorDetails`), and configuration models (`SnapcastOptions`, etc.).
    * **Enums (`/Core/Enums`)**: Common enumerations like `PlaybackStatus`, `KnxConnectionType`.

5. **`/Api` (Presentation):**
    * **Controllers (`/Api/Controllers`)**: ASP.NET Core controllers handling HTTP requests. Translate requests into MediatR commands/queries. Format responses using `/Core/Models` or API-specific DTOs and the standard `ApiResponse<T>`.
    * **Authentication (`/Api/Auth`)**: Handlers for API Key authentication.
    * **DTOs (`/Api/Models`)**: Request models and potentially response models if they differ significantly from Core models.

This layered structure, maintained through disciplined folder organization and namespace usage within the single project, facilitates separation of concerns, testability, and maintainability.

## 4.2. State Management

SnapDog2 employs a robust state management strategy centered around immutability and clear ownership, distinguishing between the raw state received from external systems and the application's internal, potentially derived state.

### 4.2.1. Immutable State Objects (Canonical Definitions)

All state representations, whether raw external state models or SnapDog2's internal models, should ideally be immutable. C# `record` types with `init`-only properties are the preferred mechanism.

```csharp
// Defined in /Core/Models folder
namespace SnapDog2.Core.Models;

using System;
using System.Collections.Generic;

// --- SnapDog2 Domain State Records ---

/// <summary>
/// Represents the immutable state of a Snapcast client as tracked by SnapDog2.
/// </summary>
public record ClientState { /* ... Properties as defined in Section 4.2.1 ... */ }

/// <summary>
/// Represents the immutable state of an audio zone managed by SnapDog2.
/// </summary>
public record ZoneState { /* ... Properties as defined in Section 4.2.1 ... */ }

// --- Supporting Records & Enums ---
public record TrackInfo(/* ... */);
public record PlaylistInfo(/* ... */);
public enum PlaybackStatus { Stopped, Playing, Paused, Buffering }
public record ErrorDetails(/* ... */);
public record SerializableExceptionInfo(/* ... */);
public record VersionDetails(/* ... */);
public record ServerStats(/* ... */);

// --- Note: Raw external models (like Sturd.SnapcastNet.Models.Client)
// --- are used within the Infrastructure layer (e.g., SnapcastStateRepository)
// --- but are mapped to the Core Models (ClientState, ZoneState) before
// --- being exposed to or managed by the Server layer.
```Updates to state held within services (like `ZoneService._currentState`) are achieved by creating a *new* record instance using a `with` expression, ensuring the state object itself is never mutated.

### 4.2.2. Thread Synchronization

State modifications must be thread-safe.

*   **In-Memory Repositories (`SnapcastStateRepository`):** Must use thread-safe collections like `System.Collections.Concurrent.ConcurrentDictionary<TKey, TValue>` for storing collections of raw external state objects (Clients, Groups). Access to individual complex structs stored within (like `ServerInfo`) might require simple `lock` statements if updates aren't atomic.
*   **Managers/Services (`ClientManager`, `ZoneService`):** When updating internal state variables (like `ZoneService._currentState` or internal mappings in `ClientManager`), access must be synchronized using `System.Threading.SemaphoreSlim(1, 1)`. Acquire the lock (`await _stateLock.WaitAsync().ConfigureAwait(false)`), perform the read-modify-write operation (creating a *new* immutable state record), update the reference, and release the lock (`_stateLock.Release()`) in a `finally` block.

```csharp
// Example within /Server/Features/Zones/ZoneService.cs - Updating internal state
private async Task Internal_UpdatePlaybackStatus(PlaybackStatus newStatus)
{
    await _stateLock.WaitAsync().ConfigureAwait(false);
    try
    {
        if (_currentState.Status == newStatus) return; // No change

        var previousStatus = _currentState.Status;
        // Create new state record
        _currentState = _currentState with { Status = newStatus };
        _logger.LogInformation("Zone {ZoneId} playback state changed from {OldStatus} to {NewStatus}", Id, previousStatus, newStatus);

        // Publish notification AFTER updating internal state
        await _mediator.Publish(new StatusChangedNotification("PLAYBACK_STATE", $"zone_{Id}", newStatus), CancellationToken.None).ConfigureAwait(false);
    }
    finally
    {
        _stateLock.Release();
    }
}
```

### 4.2.3. State Types Managed & Ownership

1. **Raw External State (e.g., Snapcast Server State):**
    * **Representation:** Uses models directly from the external library (e.g., `Sturd.SnapcastNet.Models.Client`, `Group`, `Stream`).
    * **Storage:** Held in dedicated, thread-safe, in-memory repositories within the `/Infrastructure` layer (e.g., `SnapcastStateRepository`).
    * **Updates:** Updated *only* by the corresponding Infrastructure service (e.g., `SnapcastService`) based on events received from the external system or periodic polling/status fetches.
    * **Access:** Primarily read by `/Server` layer components (Managers, Query Handlers) via repository abstractions (`ISnapcastStateRepository`) to get the latest known raw state.

2. **SnapDog2 Domain State (`ClientState`, `ZoneState`):**
    * **Representation:** Uses immutable `record` types defined in `/Core/Models`. This state represents SnapDog2's *view* of the system, potentially mapping, enriching, or differing slightly from the raw external state. Includes SnapDog2-specific concepts like internal IDs, playlist state, etc.
    * **Storage:** Held as instance variables within relevant `/Server` layer components (e.g., `ZoneService` holds its `_currentState: ZoneState`; `ClientManager` holds mappings and potentially derived `ClientState` info).
    * **Updates:** Updated by `/Server` components (Managers, Services) typically in response to MediatR commands or notifications originating from Infrastructure services. Updates use the immutable pattern (`with` expressions) protected by `SemaphoreSlim`.
    * **Access:** Read via MediatR queries which retrieve this derived state from Managers/Services.

This separation ensures that Infrastructure deals with raw external data, while the Server layer works with SnapDog2's consistent domain model. Mapping occurs at the boundary, often within Managers or Query Handlers.

### 4.2.4. State Change Flow

The flow maintains unidirectional data updates and clear responsibility:

1. **External Event Occurs:** (e.g., Snapcast client connects).
2. **Infrastructure Service Listener:** `SnapcastService` receives the event from `Sturd.SnapcastNet`.
3. **Update Raw State Repository:** `SnapcastService` updates the `SnapcastStateRepository` with the new raw `Client` model data.
4. **Publish Internal Notification:** `SnapcastService` publishes a MediatR `SnapcastClientConnectedNotification` (containing the raw `Client` model).
5. **Server Layer Handler:** `ClientManager` (as `INotificationHandler`) receives the notification.
6. **Update SnapDog2 Domain State:** `ClientManager` acquires its lock, finds/creates the internal mapping for the client, updates its internal `ClientState` record (mapping fields from the raw model and adding SnapDog2 context like ZoneId), and releases the lock.
7. **Publish Domain Status Notification:** `ClientManager` (or the handler) publishes a domain-level MediatR `StatusChangedNotification("CLIENT_CONNECTED", $"client_{internalId}", true)`.
8. **External Notification Handlers:** `MqttStatusNotifier`, `KnxStatusNotifier` receive the `StatusChangedNotification` and publish the update to MQTT/KNX.

This ensures changes flow from external -> infrastructure repo -> internal notification -> server state -> external notification -> external systems.

## 4.3. Event-Driven Architecture

Internal communication relies heavily on MediatR notifications (`INotification`).

* **Events Published By:** Primarily by `/Infrastructure` services upon detecting changes from external systems (e.g., `SnapcastClientVolumeChangedNotification`) and potentially by `/Server` Managers/Services after successfully processing a command that results in a state change (though often the external event notification is sufficient).
* **Events Handled By:** `/Server` layer components (Managers, other Services) to update their internal state based on external changes, and by `/Infrastructure` Notification Handlers (`MqttStatusNotifier`, `KnxStatusNotifier`) to propagate SnapDog2 state changes outwards.
* **Benefits:** Decouples components, allowing different parts of the system to react to events without direct dependencies.

## 4.4. Startup Sequence

Managed by `/Worker/Program.cs` and the main `IHostedService`.

1. Load Configuration (`IConfigurationBuilder`, Env Vars).
2. Setup Logging (Serilog).
3. Register Dependencies (DI Extensions for Core, Server, Infra, Api, MediatR, OTel, etc.).
4. Build `IServiceProvider`.
5. **Run Configuration Validation** (`ConfigurationValidator.Validate`). Abort on critical failure.
6. Start Host (`host.Run()`).
7. Main `IHostedService.StartAsync`:
    * Await `InitializeAsync` for critical infrastructure services sequentially:
        * `SnapcastService.InitializeAsync` (Connects, populates State Repo).
        * `MqttService.InitializeAsync` (Connects, subscribes).
        * `KnxService.InitializeAsync` (Connects/Discovers).
    * Await `InitializeAsync` for Server managers:
        * `ClientManager.InitializeAsync` (Reads initial state from repo, performs initial assignments).
        * `ZoneManager.InitializeAsync` (Syncs groups, creates ZoneService instances).
        * `PlaylistManager.InitializeAsync` (Loads radio playlist).
    * Publish initial state to MQTT (retain=true) / KNX status GAs.
8. `IHostedService.ExecuteAsync` runs (e.g., `await Task.Delay(Timeout.Infinite, stoppingToken)`).

## 4.5. Error Handling Strategy

Relies on:

1. **Result Pattern**: Universal signaling of success/failure (Sec 5.1).
2. **Structured Logging**: Contextual error details (Sec 1.5, Sec 5.2).
3. **Resilience Policies**: Polly for external calls (Sec 7).
4. **Graceful Degradation**: Attempt to function if optional services fail. Log critical failures robustly.
5. **Error Notifications**: `ERROR_STATUS` via MediatR/MQTT for critical system issues (Sec 9.2).
