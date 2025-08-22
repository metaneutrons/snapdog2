# 28. Appendices

This section contains supplementary information, including a glossary of terms, references to external systems and design patterns, and specific technical details like KNX Datapoint mappings.

## 28.1. Glossary

This glossary defines key terms used throughout the SnapDog2 blueprint for clarity and consistency.

| Term                    | Definition                                                                                           |
| :---------------------- | :--------------------------------------------------------------------------------------------------- |
| **Abstraction**         | An interface or abstract class defined in the `/Core` layer, representing a contract for a service or component implemented in another layer (typically `/Infrastructure`). |
| **ActivitySource**      | .NET class (`System.Diagnostics.ActivitySource`) used within OpenTelemetry for creating traces and spans. |
| **API Layer**           | The logical layer (`/Api` folder) responsible for handling incoming HTTP requests, exposing the REST API, performing authentication/authorization, and mapping requests/responses to/from the Server layer (Cortex.Mediator). |
| **Application Layer**   | See **Server Layer**.                                                                                  |
| **Client** (Snapcast)   | A Snapcast client endpoint device (e.g., a speaker connected to a Raspberry Pi) that receives and plays an audio stream from the Snapcast server. Identified by a unique ID (often MAC address). |
| **Client** (SnapDog2)   | SnapDog2's internal representation and management object for a Snapcast Client, identified by an internal integer ID and associated with a `ClientState` record. |
| **Command** (CQRS)      | An instruction (typically an `IRequest<Result>` Cortex.Mediator message) representing an intent to change the system's state (e.g., Play, Set Volume, Assign Client). Handled by Command Handlers. |
| **Command Framework**   | The definition (Section 9) of logical commands and status updates supported by SnapDog2 across different interfaces (MQTT, KNX, API). |
| **Configuration**       | Application settings, primarily loaded from environment variables into strongly-typed classes (Section 10). |
| **Core Layer**          | The central, innermost logical layer (`/Core` folder) containing abstractions (interfaces), domain models (records), shared patterns (`Result<T>`), enums, and configuration models. Has no dependencies on other layers. |
| **CQRS**                | Command Query Responsibility Segregation. An architectural pattern separating operations that change state (Commands) from operations that read state (Queries). Implemented via Cortex.Mediator. |
| **Dev Container**       | A Docker-based development environment configured via `.devcontainer` files for VS Code, ensuring consistency (Section 15). |
| **DI (Dependency Injection)**| A design pattern where dependencies are provided (injected) into classes rather than created internally, typically managed by a DI container (e.g., .NET's built-in `IServiceProvider`). |
| **DPT** (KNX)           | Datapoint Type. A KNX standard defining the data structure and interpretation of values transmitted on the bus for specific functions (e.g., DPT 1.001 for On/Off, DPT 5.001 for Percentage). |
| **DTO** (Data Transfer Object)| Simple object used to transfer data between layers, often used in the API layer for request/response models. SnapDog2 primarily uses Core Models directly where appropriate. |
| **Encapsulation**       | Bundling data and methods that operate on the data within one unit (class/component) and restricting access to internal details. Achieved via namespaces/folders and access modifiers (`internal`). |
| **Fault Tolerance**     | The system's ability to continue operating correctly despite failures in external components or transient errors. Implemented using Polly (Section 7). |
| **FIFO** (Named Pipe)   | A Linux inter-process communication mechanism used as an audio sink for Snapcast streams. SnapDog2's `MediaPlayerService` writes decoded audio to these pipes. |
| **GA** (KNX)            | Group Address. The logical address on the KNX bus used to link devices for communication (e.g., `1/2/3`). Used to trigger commands or report status. |
| **Group** (Snapcast)    | A set of Snapcast clients synchronized to play the same audio stream from a specific Snapcast sink. Managed 1:1 with SnapDog2 Zones. |
| **Handler** (Cortex.Mediator)   | A class implementing `IMessageHandler<TRequest, TResponse>` or `INotificationHandler<TNotification>` responsible for processing a specific Command, Query, or Notification. Resides in `/Server/Features`. |
| **Hosted Service**      | A .NET background service implementing `IHostedService`, managed by the Generic Host, used for long-running tasks or application startup orchestration (`/Worker/Worker.cs`). |
| **Idempotency**         | An operation that can be applied multiple times without changing the result beyond the initial application (e.g., `DELETE /resource/123`, `PUT /resource/123`). |
| **Infrastructure Layer**| The logical layer (`/Infrastructure` folder) containing concrete implementations of Core abstractions. Handles interactions with external libraries and systems (databases, network protocols, file system, etc.). |
| **Integration Test**    | A test verifying the interaction between multiple components or between the application and external dependencies (often using Testcontainers). |
| **KNX**                 | A standardized (ISO/IEC 14543-3) network communications protocol for building automation (lighting, HVAC, audio control, etc.). |
| **LibVLCSharp**         | .NET bindings for the cross-platform libVLC media player library, used for audio decoding and streaming to Snapcast sinks. |
| **LWT** (MQTT Last Will)| A feature where the MQTT broker publishes a predefined message on a client's behalf if the client disconnects ungracefully. Used for `SYSTEM_STATUS`. |
| **Mapping**             | The process of converting data from one model structure to another (e.g., from `Sturd.SnapcastNet.Models.Client` to `SnapDog2.Core.Models.ClientState`). Often done in Managers or Handlers. |
| **Cortex.Mediator**             | A popular .NET library implementing the Mediator and CQRS patterns, used for decoupling command/query/notification dispatching and handling. |
| **Cortex.Mediator Pipeline**    | A sequence of `IPipelineBehavior` instances that intercept and process Cortex.Mediator requests before they reach the handler, used for cross-cutting concerns. |
| **Meter** (OpenTelemetry)| .NET class (`System.Diagnostics.Metrics.Meter`) used to create and record OpenTelemetry metrics (Counters, Histograms, etc.). |
| **Metrics**             | Quantitative measurements about application performance and behavior (e.g., request duration, error rates). Collected via OpenTelemetry, exported via Prometheus. |
| **Monolithic Project**  | A software architecture where all application layers and components reside within a single deployable project/assembly (`SnapDog2.csproj`). Logical separation is maintained via folders/namespaces. |
| **MQTT**                | Message Queuing Telemetry Transport. A lightweight publish/subscribe messaging protocol used for communication with smart home systems and potentially UI clients. |
| **MQTTnet**             | The .NET library used for MQTT client implementation. |
| **Notification** (Cortex.Mediator)| An `INotification` message representing an event that has occurred within the application. Published via `IMediator.Publish` and handled by zero or more `INotificationHandler`s. |
| **Observability**       | The ability to understand the internal state and behavior of the system based on its outputs (Logs, Metrics, Traces). Implemented via OpenTelemetry. |
| **OTLP** (OpenTelemetry Protocol)| The standardized protocol for exporting telemetry data (traces, metrics, logs) from OpenTelemetry-instrumented applications to compatible backends (like Jaeger, Tempo, Prometheus). |
| **Playlist**            | A collection of tracks. Can originate from Subsonic or be the list of configured Radio stations. |
| **Polly**               | A .NET library providing resilience and transient-fault-handling capabilities (e.g., Retry, Circuit Breaker, Timeout policies). |
| **Prometheus**          | An open-source monitoring and alerting toolkit, used as the backend for storing and querying metrics collected via OpenTelemetry. |
| **Query** (CQRS)        | An instruction (typically an `IQuery<Result<T>>` Cortex.Mediator message) representing a request to retrieve data without changing system state. Handled by Query Handlers. |
| **Radio Playlist**      | A special, internally managed playlist (external index `1`) consisting of radio stations configured via environment variables. |
| **Record** (C#)         | A reference type (or struct) providing simplified syntax for creating immutable objects with value-based equality semantics. Used extensively for models, commands, queries, etc. |
| **Resilience**          | The ability of a system to handle failures gracefully and recover automatically. See Fault Tolerance. |
| **Result Pattern**      | A design pattern (using `Result` and `Result<T>` classes) for explicitly representing the success or failure outcome of operations, including error details, without relying on exceptions for control flow. |
| **Serilog**             | A popular structured logging library for .NET, used as the concrete implementation behind `Microsoft.Extensions.Logging`. |
| **Server Layer**        | The logical layer (`/Server` folder) containing the core application logic, including Cortex.Mediator handlers (Features), domain service logic (Managers), validation, and notification definitions. Depends only on `/Core`. |
| **Sink** (Snapcast)     | An audio input source configured in the Snapcast server. For SnapDog2, these are typically named pipes (FIFOs). |
| **Snapcast Server**     | The external server application responsible for synchronizing audio streams to multiple Snapcast clients. |
| **SnapcastStateRepository**| An infrastructure component (`/Infrastructure/Snapcast`) holding the last known raw state of the Snapcast server entities (`Client`, `Group`, etc.) in memory. |
| **State Management**    | The approach for tracking and updating the application's state, using immutable records and synchronization where necessary. |
| **Subsonic**            | An API protocol for music streaming servers (e.g., Navidrome). SnapDog2 uses a client library (`SubSonicMedia`) to interact with these servers. |
| **Swagger / OpenAPI**   | A specification standard for describing REST APIs, allowing automatic generation of documentation and client SDKs. Implemented using Swashbuckle.AspNetCore. |
| **Testcontainers**      | A library enabling the use of ephemeral Docker containers within integration tests to provide real dependencies (like MQTT brokers, databases, `knxd`). |
| **Tracing** (Distributed)| A method for tracking requests as they flow through different components of an application or across distributed systems. Implemented via OpenTelemetry `ActivitySource`. |
| **Unit Test**           | A test verifying a small, isolated unit of code (e.g., a single class or method) with dependencies mocked. |
| **Worker Layer**        | The top-level logical layer (`/Worker` folder) containing the application entry point (`Program.cs`), main hosted service, and Dependency Injection setup/composition root. |
| **Zone** (SnapDog2)     | SnapDog2's logical representation of an audio zone, managing playback state, associated clients, and mapping 1:1 to a Snapcast Group. Identified by an internal integer ID. |

## 28.2. References

### 28.2.1. External Systems & Libraries

* **Snapcast Server:** [badaix/snapcast (GitHub)](https://github.com/badaix/snapcast)
* **Snapcast Client Library:** [SnapCastNet (GitHub)](https://github.com/metaneutrons/snapcast-net) / [GitHub Packages](https://github.com/metaneutrons/snapcast-net/packages) (v1.0.0+)
* **Subsonic API:** [Official Specification (Requires account?)](http://www.subsonic.org/pages/api.jsp) / [Navidrome Implementation](https://www.navidrome.org/docs/api/)
* **Subsonic Client Library:** [SubSonicMedia (NuGet)](https://www.nuget.org/packages/SubSonicMedia/) (v1.0.4-beta.1+)
* **Navidrome (Example Server):** [navidrome.org](https://www.navidrome.org/)
* **MQTT Protocol:** [MQTT Standard (OASIS)](https://mqtt.org/)
* **MQTT Client Library:** [MQTTnet (GitHub)](https://github.com/dotnet/MQTTnet) / [NuGet](https://www.nuget.org/packages/MQTTnet.Extensions.ManagedClient/) (v5.x)
* **KNX Protocol:** [KNX Association Standards](https://www.knx.org/knx-en/for-professionals/standardisation/specification/)
* **KNX Client Library:** [Knx.Falcon.Sdk (NuGet)](https://www.nuget.org/packages/Knx.Falcon.Sdk/) (v6.3.x)
* **KNX Daemon (for Testing):** [knxd (GitHub)](https://github.com/knxd/knxd)
* **Media Player Library:** [LibVLC](https://www.videolan.org/vlc/libvlc.html)
* **VLC Bindings:** [LibVLCSharp (GitHub)](https://github.com/videolan/libvlcsharp) / [NuGet](https://www.nuget.org/packages/LibVLCSharp/) (v3.8.2)
* **Native LibVLC Binaries:** [VideoLAN.LibVLC.* (NuGet)](https://www.nuget.org/profiles/VideoLAN)

### 28.2.2. .NET & Core Libraries

* **.NET 9:** [Microsoft .NET Documentation](https://learn.microsoft.com/en-us/dotnet/)
* **ASP.NET Core:** [Microsoft ASP.NET Core Documentation](https://learn.microsoft.com/en-us/aspnet/core/)
* **Cortex.Mediator:** [jbogard/Cortex.Mediator (GitHub)](https://github.com/jbogard/Cortex.Mediator)
* **Polly:** [App-vNext/Polly (GitHub)](https://github.com/App-vNext/Polly)
* **Serilog:** [serilog.net](https://serilog.net/)
* **FluentValidation:** [fluentvalidation.net](https://fluentvalidation.net/)
* **OpenTelemetry .NET:** [open-telemetry/opentelemetry-dotnet (GitHub)](https://github.com/open-telemetry/opentelemetry-dotnet)
* **Testcontainers for .NET:** [testcontainers.com](https://testcontainers.com/guides/getting-started-with-testcontainers-for-dotnet/)
* **xUnit.net:** [xunit.net](https://xunit.net/)
* **Moq:** [moq/moq4 (GitHub)](https://github.com/moq/moq4)
* **FluentAssertions:** [fluentassertions.com](https://fluentassertions.com/)

### 28.2.3. Analysis & Style Tools

* **StyleCop Analyzers:** [DotNetAnalyzers/StyleCopAnalyzers (GitHub)](https://github.com/DotNetAnalyzers/StyleCopAnalyzers)
* **SonarAnalyzer for C#:** [SonarSource/sonar-dotnet (GitHub)](https://github.com/SonarSource/sonar-dotnet)
* **.editorconfig:** [editorconfig.org](https://editorconfig.org/)

### 28.2.4. Design Patterns & Principles

* **SOLID Principles:** [Wikipedia](https://en.wikipedia.org/wiki/SOLID)
* **CQRS (Command Query Responsibility Segregation):** [Martin Fowler's Bliki](https://martinfowler.com/bliki/CQRS.html)
* **Result Pattern:** [Khalid Abuhakmeh Blog](https://khalidabuhakmeh.com/csharp-result-pattern-over-exceptions)
* **Mediator Pattern:** [Wikipedia](https://en.wikipedia.org/wiki/Mediator_pattern)
* **Circuit Breaker Pattern:** [Microsoft Learn](https://learn.microsoft.com/en-us/azure/architecture/patterns/circuit-breaker)
* **Clean Architecture:** [Uncle Bob Martin Blog (Conceptual)](http://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)

## 28.3. KNX DPT Value Mapping Summary

This section summarizes the mapping between SnapDog2 internal values/states and KNX Datapoint Types (DPTs) used for communication via configured Group Addresses. Implementation occurs in `KnxService`.

* **DPT 1.xxx (Boolean/Switch/Step/Enable):**
  * Used for: `PLAY`, `PAUSE`, `STOP`, `TRACK_REPEAT`, `TRACK_REPEAT_TOGGLE`, `PLAYLIST_REPEAT`, `PLAYLIST_REPEAT_TOGGLE`, `PLAYLIST_SHUFFLE`, `PLAYLIST_SHUFFLE_TOGGLE`, `MUTE`, `MUTE_TOGGLE`, `TRACK_NEXT`, `TRACK_PREVIOUS`, `PLAYLIST_NEXT`, `PLAYLIST_PREVIOUS`. Also for Status: `PLAYBACK_STATE`, `TRACK_REPEAT_STATUS`, `PLAYLIST_REPEAT_STATUS`, `PLAYLIST_SHUFFLE_STATUS`, `MUTE_STATUS`, `CLIENT_CONNECTED`.
  * Mapping:
    * `FALSE` / Off / Stopped / Paused / Action Not Triggered maps to KNX value `0`.
    * `TRUE` / On / Playing / Trigger Action maps to KNX value `1`.
  * Specifics:
    * `PLAY`/`PAUSE` Command via `_KNX_PLAY` GA: Send `1` to Play, `0` to Pause.
    * `STOP` Command via `_KNX_STOP` GA: Send `1` to Stop.
    * `PLAYBACK_STATE` Status via `_KNX_PLAYBACK_STATUS` GA: Send `1` if state is `Playing`, otherwise send `0`.
    * Toggle Commands: Receiving `1` triggers the toggle action.

* **DPT 3.007 (Dimming Control):**
  * Used for: `VOLUME_UP`, `VOLUME_DOWN` Commands via `_KNX_VOLUME_DIM` GA.
  * Mapping:
    * `VOLUME_UP`: Send telegram with Control Bit = `1` (Step), Direction Bit = `1` (Increase). Step Code can be `001` (1/64th step) for fine control, or higher for larger steps (e.g., `111` for 1/1). **Default: Send `0b1001` (Step Up by 1/64th)**.
    * `VOLUME_DOWN`: Send telegram with Control Bit = `1` (Step), Direction Bit = `0` (Decrease). Step Code usually matches Up. **Default: Send `0b0001` (Step Down by 1/64th)**.

* **DPT 5.001 (Scaling 0-100%):**
  * Used for: `VOLUME` Command (`_KNX_VOLUME`), `VOLUME_STATUS` (`_KNX_VOLUME_STATUS`), `CLIENT_VOLUME` (`_KNX_VOLUME`), `CLIENT_VOLUME_STATUS` (`_KNX_VOLUME_STATUS`).
  * Mapping:
    * Receiving (Command): Read byte value `knxVal` (0-255). Convert to internal percentage: `percent = Math.Clamp(Math.Round(knxVal / 2.55), 0, 100)`.
    * Sending (Status): Convert internal `percent` (0-100) to KNX byte: `knxVal = (byte)Math.Clamp(Math.Round(percent * 2.55), 0, 255)`.

* **DPT 5.010 (Unsigned Count 0-255):**
  * Used for: `TRACK` Command (`_KNX_TRACK`), `PLAYLIST` Command (`_KNX_PLAYLIST`), `CLIENT_ZONE` Command (`_KNX_ZONE`), `TRACK_STATUS` Status (`_KNX_TRACK_STATUS`), `PLAYLIST_STATUS` Status (`_KNX_PLAYLIST_STATUS`), `CLIENT_ZONE_STATUS` (`_KNX_ZONE_STATUS`).
  * Mapping:
    * Receiving (Command): Read byte value `knxVal`. This represents the **1-based index**. If `knxVal == 0`, treat as invalid/ignore. Use `knxVal` directly as the 1-based index internally.
    * Sending (Status): Get the internal **1-based** index (`statusIndex`). If `statusIndex > 255` or `statusIndex < 1`, send KNX value `0`. Otherwise, send `(byte)statusIndex`.

* **DPT 7.001 (Time Period ms 0-65535):**
  * Used for: `CLIENT_LATENCY` Command (`_KNX_LATENCY`), `CLIENT_LATENCY_STATUS` (`_KNX_LATENCY_STATUS`).
  * Mapping:
    * Receiving (Command): Read `ushort` value `knxVal`. Use directly as milliseconds. Clamp if necessary (e.g., max latency allowed by Snapcast/application).
    * Sending (Status): Send internal latency `ms` value as `ushort`. Clamp if value exceeds 65535.

* **DPT 1.002 (Boolean Status):**
  * Used for: `CLIENT_CONNECTED` Status (`_KNX_CONNECTED_STATUS`).
  * Mapping: Send `1` for Connected (`true`), `0` for Disconnected (`false`).

## 28.4. Error Codes Reference

*(This section to be populated during implementation)*

This table lists application-specific `ErrorCode` strings used within the `ErrorDetails` object (published via MQTT `ERROR_STATUS` topic and potentially logged). Codes help programmatic handling of errors by consumers.

| Error Code Prefix | Component/Area      | Example Codes                                   | Description                                   |
| :---------------- | :------------------ | :---------------------------------------------- | :-------------------------------------------- |
| `CONFIG_`         | Configuration       | `CONFIG_VALIDATION_FAILED`, `CONFIG_MISSING_VAR`, `CONFIG_INVALID_FORMAT_KNXGA` | Errors during startup config load/validation |
| `SNAPCAST_`       | Snapcast Service    | `SNAPCAST_CONNECT_FAILED`, `SNAPCAST_OP_TIMEOUT`, `SNAPCAST_CLIENT_NOT_FOUND`, `SNAPCAST_GROUP_NOT_FOUND`, `SNAPCAST_RPC_ERROR` | Errors interacting with Snapcast server       |
| `KNX_`            | KNX Service         | `KNX_CONNECT_FAILED`, `KNX_DISCOVERY_FAILED`, `KNX_WRITE_TIMEOUT`, `KNX_READ_TIMEOUT`, `KNX_WRITE_ERROR`, `KNX_INVALID_DPT` | Errors interacting with KNX bus/gateway       |
| `MQTT_`           | MQTT Service        | `MQTT_CONNECT_FAILED`, `MQTT_PUBLISH_FAILED`, `MQTT_SUBSCRIBE_FAILED` | Errors interacting with MQTT broker           |
| `SUBSONIC_`       | Subsonic Service    | `SUBSONIC_REQUEST_FAILED`, `SUBSONIC_AUTH_FAILED`, `SUBSONIC_NOT_FOUND`, `SUBSONIC_PARSE_ERROR` | Errors interacting with Subsonic API        |
| `MEDIAPLAYER_`    | Media Player Service| `MEDIAPLAYER_PLAYBACK_FAILED`, `MEDIAPLAYER_SINK_ERROR`, `MEDIAPLAYER_VLC_ERROR` | Errors related to audio playback (LibVLC)   |
| `PLAYLIST_`       | Playlist Manager    | `PLAYLIST_INVALID_INDEX`, `PLAYLIST_TRACK_NOT_FOUND` | Errors in playlist/track logic              |
| `API_`            | API Layer           | `API_BAD_REQUEST`, `API_UNAUTHORIZED`, `API_NOT_FOUND`, `API_VALIDATION` | Errors originating from API request handling |
| `INTERNAL_`       | General/Core        | `INTERNAL_UNHANDLED_EXCEPTION`, `INTERNAL_MAPPING_ERROR` | Unexpected or internal application errors   |
