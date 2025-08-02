# 22. Testing Strategy

A comprehensive, multi-layered testing strategy is essential for ensuring the quality, correctness, reliability, and maintainability of the SnapDog2 application. This strategy emphasizes testing components at different levels of integration, providing fast feedback during development while also verifying end-to-end functionality. The tests will reside initially within a single test project (`SnapDog2.Tests`), potentially organized into subfolders based on the test type (`/Unit`, `/Integration`, `/Api`).

## 22.1. Test Types and Goals

The strategy follows the principles of the testing pyramid/trophy, prioritizing different types of tests based on their scope, speed, and purpose:

1. **Unit Tests (`SnapDog2.Tests/Unit/`)**
    * **Goal:** Verify the correctness of the smallest testable parts of the software (individual classes or methods) in complete isolation from their dependencies. These tests should be very fast, stable, and provide immediate feedback to developers as they write code. The target is high code coverage for critical business logic.
    * **Scope:** Focuses on classes within the `/Core` layer (e.g., utility functions, model validation if any) and especially the `/Server` layer (Cortex.Mediator Handlers, Managers like `ZoneManager`, `ClientManager`, `PlaylistManager`, Validators, individual `ZoneService` logic). Simple logic within `/Infrastructure` or `/Api` layers can also be unit tested if isolated.
    * **Technique:** Utilize the xUnit testing framework. All external dependencies (interfaces from `/Core/Abstractions`, `ILogger`, `IMediator`) **must** be mocked using a mocking framework like Moq. Tests focus on verifying the internal logic of the unit under test: conditional paths, calculations, state transitions (for stateful services tested in isolation), validation rule enforcement, handling of input parameters, edge cases (nulls, empty collections, boundary values), and correct return values (including `Result` states). Assertions are made using a fluent assertion library like FluentAssertions for readability.

2. **Integration Tests (`SnapDog2.Tests/Integration/`)**
    * **Goal:** Verify the interaction and collaboration *between* different internal components of SnapDog2 or between SnapDog2 and real (or simulated via containers) external dependencies. These tests ensure that components work together correctly through their defined interfaces or message contracts (Cortex.Mediator). They are slower than unit tests but provide higher confidence in component integration.
    * **Scope & Technique - Sub-Types:**
        * **Internal Component Integration Tests:** Verify interactions within the application boundary, such as the Cortex.Mediator pipeline flow or the collaboration between a Cortex.Mediator handler and a Core Manager. Use the .NET `IServiceCollection`/`ServiceProvider` to build a limited DI container for the test, registering real implementations of the components under test (e.g., the handler, the manager, pipeline behaviors) but mocking the outermost infrastructure *interfaces* (`ISnapcastService`, `IKnxService`, `IMqttService`, `ISubsonicService`). This validates the internal wiring and logic flow without hitting actual external systems.
        * **Infrastructure Adapter Integration Tests (Testcontainers):** Verify the concrete infrastructure service implementations (`/Infrastructure/*`) against actual external services running in Docker containers managed by Testcontainers-dotnet. This validates the interaction with the real protocols and libraries.
            * **MQTT:** Test `MqttService` against a `Testcontainers.Mosquitto` container. Verify connection, subscription, publishing, message reception, LWT behavior, and topic mapping.
            * **Subsonic:** Test `SubsonicService` against a `Testcontainers` instance running Navidrome (or another Subsonic server image) seeded with test data. Verify API calls (`GetPlaylistsAsync`, `GetStreamUrlAsync`), data mapping, and resilience policy behavior (by manipulating the container or using fault-injection proxies if needed).
            * **KNX:** Test `KnxService` against a `Testcontainers` instance running **`knxd`**. The test fixture will instantiate both the `KnxService` (connecting to `knxd`'s IP/Port) and a separate `Knx.Falcon.Sdk.KnxBus` instance ("Test Client Bus") connected to the *same* `knxd` container. This allows testing connection logic, sending commands from the Test Client Bus to verify `KnxService`'s `OnGroupValueReceived` mapping to Cortex.Mediator, having `KnxService` send status updates via `SendStatusAsync` and asserting their reception on the Test Client Bus, and testing `OnGroupReadReceived` responses.
            * **Snapcast (Optional/Complex):** Testing `SnapcastService` against a containerized Snapcast server is possible but might be complex to automate state verification fully. An alternative is more thorough mocking of `ISnapcastService` in internal integration tests, combined with focused manual testing against a real Snapcast server during development.
    * **Tools:** xUnit, Testcontainers-dotnet (for Mosquitto, Navidrome, knxd), Moq (for mocking boundaries not covered by containers), FluentAssertions.

3. **API / Functional Tests (`SnapDog2.Tests/Api/`)**
    * **Goal:** Verify end-to-end functionality and application behavior from the perspective of an external API client, simulating real user interactions or system integrations. These are the slowest tests but provide the highest confidence that the system meets functional requirements.
    * **Scope:** Treat the fully deployed application stack (SnapDog2 application + dependent services like MQTT, Snapcast, Subsonic, running via `docker compose`) as a black box. Interact *exclusively* through the defined REST API endpoints (Section 11).
    * **Technique:**
        * Use `Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<TEntryPoint>` for in-memory testing of the API layer against a test-configured application host (allows mocking specific infrastructure services at the DI level for targeted tests).
        * *Or* (for full end-to-end including external services): Start the application stack using `docker compose up`. Use a standard `HttpClient` instance in the test project to send HTTP requests to the running `snapdog` container's exposed API port.
        * Send requests covering various API endpoints and scenarios defined by the Command Framework (Sec 9) and API Specification (Sec 11).
        * Assert HTTP status codes, `ApiResponse<T>` structure (success/error flags, error codes), and the content of the `Data` payload against expected results.
        * Optionally, verify side effects:
            * Use an MQTT test client library (like MQTTnet) to subscribe to relevant status topics and assert that expected messages are published after API calls.
            * (If feasible in test setup) Query the `ISnapcastStateRepository` or specific API status endpoints to confirm state changes post-command.
    * **Tools:** xUnit, `HttpClient`, `WebApplicationFactory<TEntryPoint>` (where `TEntryPoint` is often `Program` or `Startup`), FluentAssertions, optionally MQTTnet or other client libraries for side-effect verification.

## 22.2. Specific Component Testing Approaches

* **Cortex.Mediator Handlers (`/Server/Features/*`) (Unit):** Mock all injected dependencies (`ILogger`, `IZoneManager`, `ISnapcastService`, `IMediator` etc.). Provide command/query/notification. Assert mock interactions (`Verify`) and returned `Result`/`Result<T>`. Test all logic paths within the handler.
* **Cortex.Mediator Behaviors (`/Server/Behaviors`) (Integration - Internal):** Test via DI setup. Send request through pipeline. Assert behavior's specific actions (logs created, validation exception thrown, performance metrics recorded via mocked `IMetricsService`, downstream handler called/not called).
* **Core Managers (`/Server/Managers`) (Unit/Integration):**
  * Unit: Mock dependencies. Test internal state logic, calculations, mapping rules.
  * Integration: Use real manager, mock infrastructure interfaces. Test orchestration logic triggered by handlers.
* **Infrastructure Services (`/Infrastructure/*`) (Integration - Testcontainers/knxd):** Primary testing method. Verify against containerized external service. Test connection resilience, protocol serialization/deserialization, event mapping (external library event -> internal Cortex.Mediator notification), command mapping (internal `Result` -> external library call). Mock `IMediator` to verify correct notifications are published.
* **KNX Service (`KnxService`):** Requires the dedicated **`knxd` + `KnxBus` Test Client** integration test strategy outlined in Sec 18.1.2 to verify interaction with the KNXnet/IP protocol, command mapping, status updates, and read request handling. Unit tests cover any isolated logic (e.g., DPT conversion helpers if complex).
* **API Controllers (`/Api/Controllers`) (API/Functional):** Test primarily via `WebApplicationFactory` or `HttpClient`. Focus on request routing, model binding/validation, authentication/authorization, response status codes, and `ApiResponse<T>` structure. Minimal unit testing needed.
* **Configuration Loading/Validation (Integration):** Test `ConfigurationValidator` logic by manipulating environment variables within the test process (`Environment.SetEnvironmentVariable`) and running the validation against a test `ServiceProvider`. Assert that it passes/fails correctly based on valid/invalid/missing required settings. Test `EnvConfigHelper` parsing logic via Unit tests.

## 22.3. Specific Test Scenario Examples

* **Unit Test `PlaylistManager`:**
  * `GetPlaylistsAsync`: Given Radio configured & Subsonic mock returns 2 playlists, assert result list contains 3 items, with Radio details first (ID "radio", correct name/count), followed by mapped Subsonic playlists.
  * `GetTrackForPlaybackAsync`: Given PlaylistIndex=1, TrackIndex=2, assert correct `TrackInfo` for the second configured radio station is returned. Given PlaylistIndex=2, TrackIndex=5, assert `ISubsonicService.GetPlaylistAsync` is called for playlist index 2 (mapped from external ID/index), and the 5th track (index 4 internally) is returned. Test boundary conditions (invalid indices).
  * `NextTrack`: Test with Shuffle=On returns different random track. Test with RepeatPlaylist=On wraps from last to first track. Test with RepeatTrack=On disables repeat mode and returns next track.
* **Integration Test `KnxService` (using `knxd` & `KnxBus` test client):**
  * `Send/Receive Volume`: Test Client sends DPT 5.001=128 to Volume Set GA. Mock `IMediator` verifies `SetZoneVolumeCommand { Volume = 50 }` sent. Trigger Cortex.Mediator `StatusChangedNotification("VOLUME_STATUS", "zone_1", 75)`. Test Client asserts reception of DPT 5.001=191 on Volume Status GA.
  * `Read Request`: Test Client sends Read Req to Volume Status GA. Mock `IMediator` to return ZoneState with Volume=60. Test Client asserts reception of GroupValueResponse with DPT 5.001=153.
  * `Connection`: Test `InitializeAsync` connects to `knxd`. Stop/start `knxd` container, verify `KnxService` reconnects successfully via `ConnectionStateChanged` handling.
* **API Test:**
  * `Auth`: Send request to `GET /zones` without `X-API-Key`; assert 401 Unauthorized. Send with invalid key; assert 401. Send with valid key; assert 200 OK.
  * `End-to-End Play`: Send `PUT /zones/1/playlist` body `{"index": 1}` (Radio). Assert 202. Send `POST /zones/1/commands/play` body `{"trackIndex": 2}`. Assert 202. Send `GET /zones/1`. Assert 200 OK, body shows `playback_state: "play"`, `playlist.id: "radio"`, `track.index: 2`, `track.title` matches second radio station name. Subscribe to `snapdog/zones/1/state` via MQTT Test Client; assert JSON payload reflects these changes.

## 22.4. Supporting Strategies

* **Mocking Framework:** Moq.
* **Assertion Library:** FluentAssertions.
* **Test Data Generation:** Use builder patterns or simple factory methods for creating test objects (Commands, Models). Consider Bogus for generating larger sets of diverse data if needed later.
* **Code Coverage:** Use Coverlet integrated with CI (`dotnet test --collect:"XPlat Code Coverage"`).
  * **Target:** Aim for >85% line/branch coverage for `/Core` and `/Server` layers. Track coverage reports (e.g., upload to Codecov/SonarQube). Focus on testing logic branches, not just line coverage.
* **CI Integration:**
  * Run Unit & Internal Integration tests on every commit/PR.
  * Run Testcontainer Integration & API tests nightly or on merge to main branches (due to longer execution time).
  * Fail build on test failures. Report coverage changes.
