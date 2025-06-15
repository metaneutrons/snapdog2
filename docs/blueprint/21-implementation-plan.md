# Implementation Plan

## 21.1 Overview

This implementation plan outlines a phased approach for developing SnapDog2, prioritizing the delivery of core functionality first (Minimum Viable Product - MVP) followed by iterative enhancements. The plan assumes the **single server project structure** (`SnapDog2.csproj`) with logical separation via folders (`/Core`, `/Server`, `/Infrastructure`, `/Api`, `Worker`) as defined in the architecture. Adherence to the Coding Style & Conventions (Section 1) is expected throughout. Checkboxes (`[ ]`) are provided for tracking task completion.

## 21.2 Phase 0: Project Setup & Foundation (Bootstrap)

*Goal: Establish the basic solution structure, tooling, configurations, and CI pipeline.*

* `[ ]` **0.1:** Initialize Git repository with a standard .NET `.gitignore` file.
* `[ ]` **0.2:** Create solution (`SnapDog2.sln`) using `dotnet new sln`. Create the main web API project (`dotnet new webapi -n SnapDog2 -o SnapDog2 --framework net9.0`). Create the test project (`dotnet new xunit -n SnapDog2.Tests -o SnapDog2.Tests --framework net9.0`). Add both projects to the solution.
* `[ ]` **0.3:** Establish the primary folder structure within `SnapDog2`: `/Core`, `/Server`, `/Infrastructure`, `/Api`, `/Worker`. Create subfolders as needed (e.g., `/Core/Abstractions`, `/Core/Models`, `/Server/Features`, `/Infrastructure/Snapcast`). Add initial `.gitkeep` files to empty folders if required.
* `[ ]` **0.4:** Create and configure `.editorconfig` and `stylecop.json` files at the solution root with rules specified in Section 1. Update `SnapDog2.csproj` to enable Nullable context (`<Nullable>enable</Nullable>`) and XML documentation file generation (`<GenerateDocumentationFile>true</GenerateDocumentationFile>`), suppressing warning `1591` initially.
* `[ ]` **0.5:** Create `Directory.Packages.props` at the solution root. Enable Central Package Management (`<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>`). Add initial `<PackageVersion>` entries for core dependencies: `Microsoft.Extensions.Hosting`, `MediatR`, `Serilog.AspNetCore`, `StyleCop.Analyzers`, `SonarAnalyzer.CSharp`, `Microsoft.CodeAnalysis.NetAnalyzers`. Reference analyzers in `SnapDog2.csproj` using `<PackageReference Include="..." Version="" PrivateAssets="all" />`.
* `[ ]` **0.6:** Implement `/Worker/Program.cs` with minimal `WebApplication.CreateBuilder` and `app.Run()`. Configure Serilog via `builder.Host.UseSerilog()` using the basic Console sink setup from Section 5.2. Create `/Worker/GlobalUsings.cs` and add common `System.*` namespaces.
* `[ ]` **0.7:** Define Core `IResult`, `Result`, `Result<T>` types in `/Core/Models` as specified in Section 5.1.1, including XML documentation.
* `[ ]` **0.8:** Create and configure the Dev Container environment (`/.devcontainer` folder with `devcontainer.json`, `Dockerfile`, `docker-compose.yml` using shared network, basic service configs) as specified in Section 15.
* `[ ]` **0.9:** Setup a basic GitHub Actions workflow (`.github/workflows/build-check.yml`) triggered on push/pull_request to perform: `dotnet restore`, `dotnet format --verify-no-changes`, `dotnet build --configuration Release`.

## 21.3 Phase 1: Core Engine & Snapcast Connection

*Goal: Application connects to Snapcast, discovers server state, populates the internal state repository, and basic internal state queries function.*

* `[ ]` **1.1:** Implement Configuration Classes (`/Core/Configuration`) for `SnapcastOptions`, `ZoneConfig`, `ClientConfig`, `RadioConfiguration`, `KnxOptions`, `MqttOptions`, `SubsonicOptions`, `ApiAuthConfiguration`, `TelemetryOptions`. Implement `/Infrastructure/EnvConfigHelper.cs` for loading values. Ensure `KnxZoneConfig`, `KnxClientConfig` use `Knx.Falcon.GroupAddress?` and loading attempts parsing (validation in step 1.10). Implement `RadioConfiguration.Load` based on `_URL` discovery.
* `[ ]` **1.2:** Define Core Abstractions (`/Core/Abstractions`): `ISnapcastService`, `ISnapcastStateRepository`, `IClientManager`, `IZoneManager`, `IZoneService`, `IPlaylistManager`. Define method signatures based on required functionality (e.g., `ISnapcastStateRepository` needs `UpdateClient`, `GetClient`, `GetAllClients`, etc.). Use `Result`/`Result<T>` return types. Add full XML documentation.
* `[ ]` **1.3:** Implement `SnapcastStateRepository` (`/Infrastructure/Snapcast/SnapcastStateRepository.cs`): Implement `ISnapcastStateRepository`. Use `ConcurrentDictionary` for `Client`, `Group`, `Stream` storage (using `Sturd.SnapcastNet.Models`). Implement thread-safe methods for updating and retrieving state.
* `[ ]` **1.4:** Implement `SnapcastService` (`/Infrastructure/Snapcast/SnapcastService.cs`): Implement `ISnapcastService`. Inject dependencies (`IOptions<SnapcastOptions>`, `IMediator`, `ISnapcastStateRepository`, `ILogger`). Implement `InitializeAsync` using `Sturd.SnapcastNet.SnapcastClient`, apply Polly connection policy. On connect, call `GetStatusAsync` and populate `_stateRepository`. Implement `DisposeAsync`. Wrap library calls (e.g., `SetClientVolumeAsync`) with Polly operation policy and `Result` pattern. Implement event handlers for `Sturd.SnapcastNet` events, update `_stateRepository`, and publish corresponding MediatR `INotification`s (defined in step 1.5). Use LoggerMessage pattern.
* `[ ]` **1.5:** Define Core State Models & Notifications: `ZoneState`, `ClientState`, `TrackInfo`, `PlaylistInfo`, `PlaybackStatus` enum in `/Core/Models` (using updated definitions reflecting full Snapcast data mapping). Define MediatR notification records in `/Server/Notifications` (e.g., `SnapcastClientConnectedNotification(Client client)`, `SnapcastClientVolumeChangedNotification(string clientId, ClientVolume volume)`, `StatusChangedNotification(string type, string targetId, object value)`).
* `[ ]` **1.6:** Implement `ClientManager` (`/Server/Managers/ClientManager.cs`): Implement `IClientManager`. Inject `ISnapcastService`, `IMediator`, `ISnapcastStateRepository`, `List<ClientConfig>`, `ILogger`. Maintain internal ID mappings. Implement `InitializeAsync` (discover clients from repo, apply **Initial Assignment Option B**). Implement `AssignClientToZoneAsync`. Implement `GetAllClientsAsync` (map from repo + internal data). Implement MediatR handlers for Snapcast notifications (update internal state, handle **External Changes Option B**).
* `[ ]` **1.7:** Implement `ZoneManager` (`/Server/Managers/ZoneManager.cs`): Implement `IZoneManager`. Inject `ISnapcastService`, `IClientManager`, `IMediator`, `List<ZoneConfig>`, `ILogger`, ZoneService Factory (`Func<ZoneState, IZoneService>`). Implement `InitializeAsync` (sync zones/groups with repo, create Snapcast groups if needed, instantiate `ZoneService`). Implement `GetZoneAsync`, `GetAllZonesAsync`. Handle `SnapcastGroupChangedNotification` (External Changes Option B).
* `[ ]` **1.8:** Implement `ZoneService` (`/Server/Features/Zones/ZoneService.cs`): Implement `IZoneService`. Inject `IMediator`, `ILogger`, `ISnapcastService` (and other services later). Takes initial `ZoneState`. Implements state update methods (`SetVolumeAsync`, etc.) using lock, Result pattern, calls infra abstractions, publishes notifications.
* `[ ]` **1.9:** Implement Basic MediatR Queries & Handlers (`/Server/Features/...`): `GetAllClientsQueryHandler` (uses `IClientManager`), `GetZoneStateQueryHandler` (uses `IZoneManager` + `IZoneService.GetStateAsync`).
* `[ ]` **1.10:** DI Registration (`/Worker/DI`): Create extension methods (`AddCoreServices`, `AddInfrastructureServices`, `AddMediatRProcessing`). Register all implemented services/managers/repositories/options/handlers with correct lifetimes. Implement `/Worker/ConfigurationValidator.cs` and call `ConfigurationValidator.Validate(host.Services)` in `Program.cs` **before** `host.Run()`, handling validation failure by preventing startup.
* `[ ]` **1.11:** Basic Unit Tests (`/tests`): Test `ClientManager`, `ZoneManager`, `SnapcastStateRepository` logic (mock dependencies). Test `ConfigurationValidator`. Test mapping logic.
* `[ ]` **1.12:** Implement Main Hosted Service (`/Worker/Worker.cs`): Inject `ILogger`, `IServiceProvider` or specific services. In `ExecuteAsync`, call `InitializeAsync` on `SnapcastService`, `ZoneManager`, `ClientManager` (handle potential `Result.Failure`). Keep service running.

*Goal: App starts, connects to Snapcast, validates config, syncs Snapcast state into repo, basic internal state/mapping established, basic queries work.*

## 21.4 Phase 2: Basic Playback & Media Source

*Goal: Enable playback of specific tracks from Radio and Subsonic sources via internal commands.*

* `[ ]` **2.1:** Add NuGet Dependencies: `LibVLCSharp`, `VideoLAN.LibVLC.Linux`, `SubSonicMedia` (Sec 16). Implement `SubsonicOptions` config class.
* `[ ]` **2.2:** Define Core Abstractions: `IMediaPlayerService`, `ISubsonicService`, `IPlaylistManager` (`/Core/Abstractions`).
* `[ ]` **2.3:** Implement `MediaPlayerService` (`/Infrastructure/Media/MediaPlayerService.cs`): Implement `IMediaPlayerService`. Initialize LibVLC (`Core.Initialize`). Create/manage `LibVLC` and `MediaPlayer` instances per zone. Implement `PlayAsync(zoneId, trackInfo)` to create `Media` object from `trackInfo.Id` (URL) and use `:sout=#file{dst=...}` option pointing to the zone's sink path (`ZoneConfig.SnapcastSink`). Implement `StopAsync`, `PauseAsync`. Handle `MediaPlayer` events (`EndReached`, `EncounteredError`) and publish MediatR notifications. Implement `DisposeAsync`.
* `[ ]` **2.4:** Implement `SubsonicService` (`/Infrastructure/Subsonic/SubsonicService.cs`): Implement `ISubsonicService`. Inject resilient `HttpClient`. Initialize `SubSonicMedia.SubsonicClient`. Implement `GetStreamUrlAsync`, `GetPlaylistAsync`, `GetTrackInfoAsync` by wrapping library calls, handling errors with Result pattern, and **mapping library models to Core models** (`PlaylistInfo`, `TrackInfo`, `PlaylistWithTracks`). **No general caching implementation.**
* `[ ]` **2.5:** Implement `PlaylistManager` (`/Server/Managers/PlaylistManager.cs`): Implement `IPlaylistManager`. Inject `ISubsonicService`, `RadioOptions`. Implement `GetPlaylistsAsync` (prepends Radio playlist). Implement `GetPlaylistAsync` (handles "radio" ID or delegates to Subsonic). Implement `GetTrackForPlaybackAsync(playlistIdOrIndex, trackIndex1Based)` to return `TrackInfo` for requested track (handling **1-based indexing** and Radio vs. Subsonic logic).
* `[ ]` **2.6:** Define MediatR Commands (`/Server/Features/.../Commands`): `PlayZoneCommand`, `PauseZoneCommand`, `StopZoneCommand`, `NextTrackCommand`, `PreviousTrackCommand`, `SetTrackCommand`, `SetPlaylistCommand`. Define corresponding Handlers.
* `[ ]` **2.7:** Update `ZoneService` (`/Server/Features/Zones/ZoneService.cs`): Inject `IMediaPlayerService`, `IPlaylistManager`. Implement command methods (`PlayAsync`, `StopAsync`, `PauseAsync`, `SetTrackAsync`, `SetPlaylistAsync`, `NextTrackAsync`, `PreviousTrackAsync`) which use `PlaylistManager` to determine the `TrackInfo` and then call `IMediaPlayerService`. Update internal `_currentState` (`Status`, `CurrentTrack`, `CurrentTrackIndex`, `CurrentPlaylist`, `CurrentPlaylistIndex`) upon successful actions. Publish relevant `StatusChangedNotification`s (e.g., `PLAYBACK_STATE`, `TRACK_INFO`, `TRACK_INDEX`, `PLAYLIST_INFO`, `PLAYLIST_INDEX`). Handle `TrackEndedNotification` from `MediaPlayerService` to trigger `NextTrackAsync`.
* `[ ]` **2.8:** Register Media/Playlist services in DI (`/Worker/DI`).
* `[ ]` **2.9:** Add Unit/Integration tests for `PlaylistManager`, `SubsonicService` (mocking HttpClient or using Testcontainer), `MediaPlayerService` (mocking LibVLCSharp difficult, focus on logic around it), and playback command handlers.

*Goal: Play/Stop/Next/Prev of specific Subsonic/Radio tracks via MediatR commands.*

## 21.5 Phase 3: Communication Layer - MQTT

*Goal: Control basic playback/status via MQTT.*

* `[ ]` **3.1:** Add MQTTnet v5 dependency (`MQTTnet.Extensions.ManagedClient`), `MqttOptions` config class.
* `[ ]` **3.2:** Define `IMqttService` abstraction (`/Core/Abstractions`).
* `[ ]` **3.3:** Implement `MqttService` (`/Infrastructure/Mqtt/MqttService.cs`): Implement `IMqttService`. Use `MqttClientFactory`, configure options (TLS, LWT, AutoReconnect). Use Polly for initial `ConnectAsync`. Handle `ConnectedAsync` (subscribe), `DisconnectedAsync` (log), `ApplicationMessageReceivedAsync`. Implement `PublishAsync`. Map incoming messages on configured command topics (using user-preferred detailed structure, handle **1-based indices**) to MediatR commands and `_mediator.Send`.
* `[ ]` **3.4:** Implement `MqttStatusNotifier` (`INotificationHandler` in `/Infrastructure/Mqtt`): Handle `StatusChangedNotification`. Determine MQTT topic based on config and notification `StatusType`/`TargetId`. Serialize value (handle enums/bools) and call `_mqttService.PublishAsync` for specific status topics AND the full `/state` topic. Handle **1-based indices** for status payloads.
* `[ ]` **3.5:** Register MQTT services conditionally in DI (`/Worker/DI`).
* `[ ]` **3.6:** Add Integration tests using Testcontainers for Mosquitto. Test command dispatching and status publishing for key commands (Play, Volume, Track, Playlist).

## 21.6 Phase 4: Communication Layer - KNX

*Goal: Control basic playback/status via KNX.*

* `[ ]` **4.1:** Add `Knx.Falcon.Sdk` dependency, `KnxOptions`, `KnxZoneConfig`, `KnxClientConfig`. Implement robust parsing/loading in config classes.
* `[ ]` **4.2:** Define `IKnxService` abstraction (`/Core/Abstractions`).
* `[ ]` **4.3:** Implement `KnxService` (`/Infrastructure/Knx/KnxService.cs`): Implement `IKnxService`. Handle Connection/Discovery (Option B logic). Implement `OnGroupValueReceived` -> `MapGroupAddressToCommand` -> `_mediator.Send`. Implement `OnGroupReadReceived` -> `FetchCurrentValueAsync` -> `SendKnxResponseAsync`. Implement `WriteToKnxAsync` helper using correct DPTs based on Appendix 20.3 (handle **1-based indices**, >255 rule). Use `Knx.Falcon.GroupAddress`. Apply resilience policies.
* `[ ]` **4.4:** Implement `KnxStatusNotifier` (`INotificationHandler` in `/Infrastructure/Knx`): Handle `StatusChangedNotification`. Determine target GA from config. Convert value to correct DPT format (handle **1-based indices**, >255 rule) and call `_knxService.SendStatusAsync`.
* `[ ]` **4.5:** Register KNX services conditionally in DI. Ensure `ConfigurationValidator` checks KNX config parsing results if enabled.
* `[ ]` **4.6:** Add Integration tests using **`knxd` Testcontainer** and an in-process **`KnxBus` test client**. Verify command dispatching and status updates for key GAs.

## 21.7 Phase 5: Communication Layer - API

*Goal: Control basic playback/status via REST API.*

* `[ ]` **5.1:** Setup ASP.NET Core routing/controllers in `/Worker/Program.cs`.
* `[ ]` **5.2:** Implement API Key authentication/authorization (`/Api/Auth`, register middleware in `Program.cs`).
* `[ ]` **5.3:** Implement `ApiResponse<T>` wrapper (`/Api/Models`). Define simple Request models (`/Api/Models` or `/Api/Controllers`).
* `[ ]` **5.4:** Implement Controllers (`/Api/Controllers`) mapping HTTP requests to MediatR Commands/Queries. Use Core Models (`ZoneState`, `ClientState`) directly in `ApiResponse<T>` where suitable. Handle **1-based indices** in requests/responses. Exclude dynamic zone management endpoints.
* `[ ]` **5.5:** Configure Swagger/OpenAPI generation (`/Worker/StartupExtensions`). Add XML comments to controllers/actions/models.
* `[ ]` **5.6:** Add API tests (`/tests`) using `WebApplicationFactory` or `HttpClient` against running stack.

## 21.8 Phase 6: Advanced Features & Refinements

*Goal: Implement remaining core features and enhance usability.*

* `[ ]` **6.1:** Implement full Playlist Management features (if any required beyond basic selection, e.g., queue manipulation).
* `[ ]` **6.2:** Implement Shuffle / Repeat logic within `ZoneService` or `PlaylistManager`. Add MediatR commands (`SetShuffleCommand`, `SetRepeatCommand`, etc.) and update state/notifications. Update MQTT/KNX/API to expose control and status.
* `[ ]` **6.3:** Implement remaining commands from Command Framework (Sec 9) across MediatR, MQTT, KNX, API layers (e.g., `MUTE`, `LATENCY`, specific repeat/shuffle commands/status).
* `[ ]` **6.4:** *(Postponed)* Re-evaluate need for general caching (`ICacheService`) for Subsonic after performance testing. Implement if necessary.
* `[ ]` **6.5:** Implement Client Latency control commands and status updates across all layers.
* `[ ]` **6.6:** Add support for Internet Radio streams (treat as a special playlist similar to the configured `RadioConfiguration`, or allow adding URLs via command/API). Update `PlaylistManager`.
* `[ ]` **6.7:** Refine Polly resilience policies (timeouts, retry counts) based on observed behavior during testing.

## 21.9 Phase 7: Observability & Testing Polish

*Goal: Ensure application is monitorable, diagnosable, and robustly tested.*

* `[ ]` **7.1:** Implement `IMetricsService` using OpenTelemetry Meters (`/Infrastructure/Observability`). Add custom application-specific metrics (e.g., zones playing, commands processed, external call failures).
* `[ ]` **7.2:** Add detailed manual tracing (`ActivitySource.StartActivity`) around critical or complex code paths within `/Server` and `/Infrastructure` for better diagnostics.
* `[ ]` **7.3:** Implement detailed Health Checks (`/Infrastructure/HealthChecks`) for all external dependencies (Snapcast, MQTT broker, KNX gateway, Subsonic server) and register them with ASP.NET Core health checks (`/Worker/DI`).
* `[ ]` **7.4:** Review and refine all logging messages for clarity, consistency, and appropriate levels. Ensure sensitive data is not logged excessively.
* `[ ]` **7.5:** **Implement detailed Testing Strategy (Sec 18)**: Write comprehensive Unit tests (aiming for coverage targets), Integration tests (using Testcontainers, `knxd`), and API/Functional tests covering core scenarios, edge cases, and failure conditions identified during development.
* `[ ]` **7.6:** Perform basic performance testing under simulated load (if feasible) and identify/address any obvious bottlenecks using profiling tools and metrics.

## 21.10 Phase 8: Deployment & Documentation

*Goal: Prepare application for release and provide necessary documentation.*

* `[ ]` **8.1:** Finalize production Dockerfiles (`/docker/snapdog`, `/docker/snapserver`) and `docker-compose.yml`. Ensure secure configurations and minimal image sizes.
* `[ ]` **8.2:** Create example `.env` files and document all required environment variables (Section 10) clearly.
* `[ ]` **8.3:** Write comprehensive user documentation: Setup instructions (Docker, potentially bare metal), detailed configuration guide, API usage examples, MQTT topic/payload reference, KNX GA usage guide, basic troubleshooting steps.
* `[ ]` **8.4:** Update project README with overview, features, build/run instructions, link to full documentation.
* `[ ]` **8.5:** Tag final release version in Git. Create release artifacts (e.g., Docker images published to registry).
