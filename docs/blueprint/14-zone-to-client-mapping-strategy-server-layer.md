# 19. Zone-to-Client Mapping Strategy (Server Layer)

## 19.1. Overview

This section details the strategy and implementation for managing the relationship between SnapDog2's logical **Zones** and the physical **Snapcast Clients** (speakers), ensuring alignment with the underlying Snapcast server's grouping mechanism. SnapDog2 adopts a clear and direct approach by establishing a **strict one-to-one mapping between each SnapDog2 Zone and a corresponding Snapcast Group**. This means every Zone managed within SnapDog2 maps directly to a single Group on the Snapcast server, and vice-versa (for groups managed by SnapDog2).

## 19.2. Core Principles

The mapping strategy adheres to the following core principles:

1. **1:1 Zone-Group Correspondence**: Every instance of `ZoneService` within the `/Server` layer maintains a reference to a unique Snapcast Group ID.
2. **Exclusive Client Assignment**: Each discovered Snapcast Client can belong to, at most, one SnapDog2 Zone at any given time. This translates directly to the client belonging to the corresponding single Snapcast Group managed by that Zone. Clients not assigned to a SnapDog2 Zone remain unassigned or in externally managed Snapcast groups.
3. **Authoritative Zone/Group Management**: SnapDog2 assumes authority over the lifecycle of Snapcast Groups that correspond to its configured Zones. It handles the creation of Snapcast Groups when Zones are initialized (if they don't exist) and potentially the deletion of groups when Zones are removed (though zone removal is not a planned feature based on static configuration). Renaming a Zone (via configuration change and restart) should trigger renaming the corresponding Snapcast Group.
4. **Adaptive Synchronization**: While SnapDog2 manages its zones/groups, it must also react gracefully to changes made externally (e.g., via Snapweb or another controller modifying Snapcast groups/client assignments). SnapDog2 will **adapt** its internal state (`ClientState.ZoneId`, `ZoneState.ClientIds`) to reflect the actual state reported by Snapcast server events, logging these changes.

## 19.3. Implementation (`ZoneManager`, `ClientManager`, `SnapcastService`)

The mapping and synchronization logic is primarily implemented within the core managers located in the `/Server/Managers` folder, utilizing the abstractions provided by `/Core` and implemented in `/Infrastructure`.

### 19.3.1. `ZoneManager` (/Server/Managers/ZoneManager.cs)

* **Responsibilities:** Manages the collection of active `IZoneService` instances, creates/synchronizes Snapcast Groups based on `ZoneConfig`, provides zone lookup capabilities.
* **Dependencies:** `List<ZoneConfig>`, `ISnapcastService`, `ISnapcastStateRepository`, `IClientManager`, `IMediator`, `Func<ZoneState, IZoneService>` (Factory for ZoneService), `ILogger<ZoneManager>`.
* **Initialization (`InitializeAsync` called by Worker):**
    1. Retrieves all currently existing groups from the Snapcast server via `_snapcastStateRepo.GetAllGroups()` (which was populated by `SnapcastService`).
    2. Iterates through the `List<ZoneConfig>` loaded from environment variables.
    3. For each `ZoneConfig`:
        * Checks if a Snapcast Group with a matching name already exists in the repository state.
        * **If Yes:** Retrieves the existing `SnapcastGroupId`. Verifies if another SnapDog2 Zone is already mapped to this Group ID (logs error if collision detected). Maps the `ZoneConfig.Id` to this `SnapcastGroupId`.
        * **If No:** Calls `_snapcastService.CreateGroupAsync(zoneConfig.Name)` to create the corresponding group on the Snapcast server. Stores the returned `SnapcastGroupId`. Maps `ZoneConfig.Id` to the new `SnapcastGroupId`. Handles potential errors using the Result pattern.
        * Creates the initial `ZoneState` record using data from `ZoneConfig` and the determined `SnapcastGroupId`.
        * Uses the injected `Func<ZoneState, IZoneService>` factory to create a `ZoneService` instance for this zone, passing the initial state.
        * Stores the `IZoneService` instance in an internal dictionary, keyed by `ZoneConfig.Id`.
        * Stores the `SnapcastGroupId` -> `ZoneConfig.Id` mapping.
    4. (Optional Cleanup): Identify any Snapcast groups present on the server (via repository) that do *not* correspond to any `ZoneConfig`. Log a warning about these "unmanaged" groups. Do not delete them automatically.
* **Lookup:** Provides methods like `GetZoneAsync(int zoneId)`, `GetAllZonesAsync()`, `TryGetZoneIdByGroupId(string snapcastGroupId, out int zoneId)`.
* **Event Handling:** Handles Cortex.Mediator `SnapcastGroupChangedNotification` (published by `SnapcastService`). If a *managed* group's name changes externally, logs a warning and potentially calls `_snapcastService.SetGroupNameAsync` to revert it back to the configured name (Authoritative approach for names), or updates the internal `ZoneService.Name` (Adaptive). *Decision: Adopt Adaptive approach for external name changes - update internal state and log.*

### 19.3.2. `ClientManager` (/Server/Managers/ClientManager.cs)

* **Responsibilities:** Discovers clients, maps Snapcast Client IDs to internal SnapDog2 Client IDs, manages `ClientState`, handles assigning clients to zones, responds to Snapcast client events.
* **Dependencies:** `ISnapcastService`, `ISnapcastStateRepository`, `IZoneManager`, `IMediator`, `List<ClientConfig>`, `ILogger<ClientManager>`.
* **Mapping:** Maintains internal mappings: `ConcurrentDictionary<string, int> _snapcastIdToInternalId` and `ConcurrentDictionary<int, ClientState> _internalClientStates`. Also `ConcurrentDictionary<string, int> _lastKnownZoneAssignment`.
* **Initialization (`InitializeAsync` called by Worker):**
    1. Calls `_snapcastStateRepo.GetAllClients()` to get the initial list.
    2. Populates internal mappings (`_snapcastIdToInternalId`, `_internalClientStates` by calling `MapSnapClientToClientState`).
    3. For each client, performs **Initial Assignment (Option B):**
        * Check the `Client` model from the repository to see if it's already assigned to a Snapcast Group ID.
        * Use `_zoneManager.TryGetZoneIdByGroupId` to see if this group corresponds to a managed SnapDog2 Zone.
        * **If Yes:** Assign the client to this existing `ZoneId` in its `ClientState` and update `_lastKnownZoneAssignment`.
        * **If No (or not in a group):** Get the `DefaultZoneId` from the client's corresponding `ClientConfig`. If valid, call `AssignClientToZoneAsync` to assign it to the default zone.
* **Client Assignment (`AssignClientToZoneAsync(int clientId, int zoneId)`):**
    1. Finds the internal `clientId` and corresponding `snapcastClientId`.
    2. Uses `_zoneManager.GetZoneAsync(zoneId)` to get the target `ZoneService` and its `SnapcastGroupId`.
    3. Calls `_snapcastService.AssignClientToGroupAsync(snapcastClientId, snapcastGroupId)`.
    4. If successful, updates the internal `_internalClientStates[clientId]` record with the new `ZoneId`.
    5. Updates `_lastKnownZoneAssignment[snapcastClientId] = zoneId`.
    6. Publishes a `StatusChangedNotification` for `CLIENT_ZONE_STATUS`.
* **Event Handling:** Implements `INotificationHandler` for Snapcast events published by `SnapcastService`:
  * `Handle(SnapcastClientConnectedNotification)`: Updates the corresponding `ClientState` (sets `Connected = true`, updates `LastSeenUtc`). Checks `_lastKnownZoneAssignment`. If the client isn't currently in a managed group (check state repo), calls `AssignClientToZoneAsync` to restore it to its last known zone. Publishes `StatusChangedNotification("CLIENT_CONNECTED", ..., true)`.
  * `Handle(SnapcastClientDisconnectedNotification)`: Updates the corresponding `ClientState` (sets `Connected = false`). Publishes `StatusChangedNotification("CLIENT_CONNECTED", ..., false)`.
  * `Handle(SnapcastClientVolumeChangedNotification)`: Updates the corresponding `ClientState` (Volume, IsMuted). Publishes relevant `StatusChangedNotification`.
  * `Handle(SnapcastClientLatencyChangedNotification)`: Updates `ClientState`. Publishes notification.
  * `Handle(SnapcastClientNameChangedNotification)`: Updates `ClientState.ConfiguredSnapcastName`. Publishes notification.
  * `Handle(SnapcastGroupChangedNotification)`: **(Adaptive External Change Handling - Option B)** Iterates through the changed group's clients (from the notification's `Group` object). For each client, updates its `ZoneId` in `_internalClientStates` to match the zone corresponding to the `GroupId`. Updates `_lastKnownZoneAssignment`. Publishes relevant `CLIENT_ZONE_STATUS` notifications. Logs the change clearly. Handles clients being *removed* from a group by setting their `ZoneId` to `null` if they aren't found in another managed group.
* **State Retrieval (`GetAllClientsAsync`, `GetClientAsync`):** Retrieves raw data from `_snapcastStateRepo` and merges/maps it with internal state (`internal ID`, configured `Name`, assigned `ZoneId`) to produce the final `ClientState` records.

### 19.3.3. `SnapcastService` (/Infrastructure/Snapcast/SnapcastService.cs)

* **Responsibilities:** Interface with `Sturd.SnapcastNet`.
* Listens for library events (`ClientConnected`, `GroupChanged`, etc.).
* **Updates `ISnapcastStateRepository`** immediately upon receiving events.
* **Publishes Cortex.Mediator `INotification`s** containing the **raw `Sturd.SnapcastNet.Models` objects** (e.g., `SnapcastClientConnectedNotification(Client client)`). This allows handlers like `ClientManager` to access the most up-to-date raw information.

## 19.4. Cortex.Mediator Notifications for Synchronization

These notifications, defined in `/Server/Notifications`, facilitate loose coupling between `SnapcastService` and Managers.

```csharp
namespace SnapDog2.Server.Notifications; // Example namespace

using System;
using Cortex.Mediator;
using Sturd.SnapcastNet.Models; // Use raw models from library

// Published by SnapcastService when underlying library raises event
public record SnapcastClientConnectedNotification(Client Client) : INotification;
public record SnapcastClientDisconnectedNotification(string SnapcastClientId) : INotification; // Only ID needed usually
public record SnapcastGroupChangedNotification(Group Group) : INotification; // Carries full Group info including clients
public record SnapcastClientVolumeChangedNotification(string SnapcastClientId, ClientVolume Volume) : INotification;
public record SnapcastClientLatencyChangedNotification(string SnapcastClientId, int Latency) : INotification; // Assuming event args provide this
public record SnapcastClientNameChangedNotification(string SnapcastClientId, string NewName) : INotification; // Assuming event args provide this
public record SnapcastGroupMuteChangedNotification(string GroupId, bool IsMuted) : INotification; // Assuming event args provide this
public record SnapcastStreamChangedNotification(string StreamId, Stream Stream) : INotification; // Example for stream updates
// ... other notifications as needed based on Sturd.SnapcastNet events ...

// General internal status notification (published by Managers/Services after state update)
public record StatusChangedNotification(string StatusType, string TargetId, object Value) : INotification;

```

## 19.5. Data Contracts (`ClientState`, `ZoneState`)

*(Canonical definitions in Section 4.2.1, updated to include more fields mapped from Snapcast)*

## 19.6. Registration in DI Container

`ISnapcastService`, `ISnapcastStateRepository`, `IZoneManager`, `IClientManager` are registered as singletons in `/Worker/DI`. Relevant Cortex.Mediator notification handlers (including those within `ClientManager` and `ZoneManager`) are registered automatically by `services.AddCortex.Mediator()`.

```csharp
// In /Worker/DI/CoreServicesExtensions.cs (Example)
namespace SnapDog2.Worker.DI;

using Microsoft.Extensions.DependencyInjection;
using SnapDog2.Core.Abstractions;
using SnapDog2.Infrastructure.Snapcast; // For Service & Repo implementations
using SnapDog2.Server.Managers; // For Manager implementations

public static class CoreServicesExtensions
{
    public static IServiceCollection AddCoreServicesAndInfrastructure(this IServiceCollection services)
    {
        // Register Infrastructure Service Abstraction Implementations
        services.AddSingleton<ISnapcastService, SnapcastService>();
        services.AddSingleton<ISnapcastStateRepository, SnapcastStateRepository>();
        // ... register other infra services (IKnxService, IMqttService etc.)

        // Register Core Managers/Services (Server Layer)
        services.AddSingleton<IZoneManager, ZoneManager>();
        services.AddSingleton<IClientManager, ClientManager>();
        services.AddSingleton<IPlaylistManager, PlaylistManager>();
        // Register ZoneService Factory if using Func<ZoneState, IZoneService>
        // services.AddTransient<IZoneService, ZoneService>(); // Or register directly if factory not needed

        return services;
    }
}
```

This detailed strategy ensures SnapDog2 maintains its own logical view of zones and clients while staying synchronized with the underlying Snapcast server state in an adaptive manner.
