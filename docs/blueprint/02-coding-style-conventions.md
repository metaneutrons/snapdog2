# 2. Coding Style & Conventions

## 2.1. Overview

This foundational chapter meticulously defines the mandatory coding standards, style guidelines, and development conventions for the SnapDog2 project. Adherence to these standards is paramount to ensure a high degree of code consistency, maintainability, readability, and overall quality throughout the application's lifecycle. SnapDog2 is built utilizing the **.NET 9.0 framework** and embraces the **latest stable C# language features** where they enhance clarity, performance, and developer productivity.

The application employs a **monolithic server project structure** (`SnapDog2.csproj`). While contained within a single project, a strict logical separation into distinct layers is maintained through the disciplined use of **folders and corresponding namespaces** (`Core`, `Server`, `Infrastructure`, `Api`, `Worker`). This approach promotes modularity and testability while simplifying the solution setup.

Code quality and style consistency are rigorously enforced through a multi-pronged strategy:

* **Static Analysis**: Industry-standard analyzers, specifically **StyleCop Analyzers** and **SonarAnalyzer**, are integrated directly into the build process via NuGet packages referenced in the main project file. These analyzers automatically detect deviations from configured style rules and potential code quality issues, providing immediate feedback to developers within the IDE and failing the build in the CI pipeline if violations occur. Configuration is managed centrally via `stylecop.json` and `.editorconfig`.
* **Documentation**: Comprehensive API documentation is **mandatory**. All public types (classes, interfaces, structs, records, enums, delegates) and public members (methods, properties, events, fields) **must** include well-formed XML documentation comments (`<summary>`, `<param>`, `<returns>`, `<typeparam>`, etc.). This requirement is enforced programmatically by setting the `<GenerateDocumentationFile>true</GenerateDocumentationFile>` property in the `SnapDog2.csproj` file and enabling relevant StyleCop documentation rules (SA1600+). The build process should treat missing documentation warnings as errors unless an explicit suppression with clear justification is provided.
* **Automated Formatting**: A root `.editorconfig` file, located at the solution level, defines and enforces fundamental code formatting rules, including indentation style, line endings, spacing preferences, and more. Developers are required to utilize automated code formatting tools integrated into their IDE (e.g., Visual Studio's Format Document feature, Rider's cleanup profiles) or command-line tools like `dotnet format` to ensure submitted code strictly adheres to these formatting standards.

## 2.2. General Conventions

These overarching principles guide the development process and architectural choices:

* **Framework & Language**: Target **.NET 9.0**. Actively utilize modern and appropriate **C# language features** (currently C# 12/13 features available in the .NET 9 SDK) such as primary constructors, collection expressions, `required` members, file-scoped namespaces, enhanced pattern matching, `record` types, etc., prioritizing clarity, conciseness, and performance benefits.
* **Immutability**: Strongly favor immutable data structures wherever possible. This applies particularly to Data Transfer Objects (DTOs) used in the API layer, configuration models, MediatR messages (Commands, Queries, Notifications), and internal state representations (`ZoneState`, `ClientState`). Use C# `record` types with `init`-only properties as the default mechanism for achieving immutability. Updates to state objects within services must be performed immutably by creating *new* state record instances using `with` expressions, rather than modifying existing objects in place.
* **Asynchronous Programming**: Employ `async`/`await` **mandatorily** for all operations that are potentially I/O-bound, including network communication (HTTP calls, MQTT, KNX, Snapcast control), file system interactions (logging sinks, configuration loading if applicable), and any future database operations. Strictly avoid `async void` methods, with the sole exception being top-level event handlers directly subscribing to external library events where the event signature dictates `void` return (e.g., `SnapcastClient.ClientConnected += MyHandler;`). Even in such cases, the handler body must contain comprehensive `try/catch` blocks to prevent unhandled exceptions from terminating the process. Consistently use `ConfigureAwait(false)` on awaited `Task` and `ValueTask` instances within the `/Infrastructure` and `/Core` layers to prevent deadlocks by avoiding unnecessary capturing and resuming on the original synchronization context. Ensure `CancellationToken` parameters are accepted and passed down through asynchronous call chains wherever feasible to support cooperative cancellation.
* **Error Handling**: Strict adherence to the **Result Pattern** (defined canonically in Section 5.1 using `Result` and `Result<T>`) is **mandatory** for all methods that represent operations which might fail due to predictable operational reasons (e.g., external service unavailable after retries, invalid input not caught by validation, business rule violation, resource not found). Exceptions must **never** be used for normal control flow or to signal expected failure conditions across internal component boundaries (between `Server`, `Infrastructure`, `Api` layers). Use `try/catch` blocks **only** at the lowest level of interaction with external systems or libraries (typically within `/Infrastructure` services) where the external code might throw exceptions. Any caught exception must be immediately logged with relevant context and converted into a `Result.Failure(ex)` object, which is then returned up the call stack for controlled handling by the caller. Unhandled exceptions should only occur in truly exceptional, unrecoverable circumstances (e.g., critical configuration missing at startup, out-of-memory) and should result in application termination after logging.
* **Dependency Injection**: Exclusively utilize .NET's built-in Dependency Injection container (`Microsoft.Extensions.DependencyInjection`). Define abstractions (interfaces) in the `/Core/Abstractions` folder. Implement these interfaces in the appropriate layer (`/Infrastructure` for external service interactions, `/Server` for core application logic services/managers). Register all services with their correct lifetimes (typically `Singleton` for stateless services, state repositories like `SnapcastStateRepository`, and configuration options; `Scoped` if request-specific context is needed, e.g., within API request handling; `Transient` for lightweight, stateless services like MediatR handlers and validators) within dedicated extension methods organized by layer or feature in the `/Worker/DI` folder. These extensions are called from `Program.cs`. Strictly favor **constructor injection** for resolving dependencies. Avoid service locator anti-patterns.
* **Logging**: Implement all application logging using the `Microsoft.Extensions.Logging` abstractions (`ILogger<T>`). **Mandatory** use of the **LoggerMessage Source Generator pattern** (detailed in Section 1.5) for all log messages to ensure maximum performance (by avoiding runtime boxing and formatting) and compile-time checking of log messages and parameters. **Serilog** is the configured concrete logging provider (backend) as detailed in Section 5.2. Logs must be structured and include relevant context, including Trace IDs and Span IDs provided by OpenTelemetry integration (Section 13) for correlation.
* **Disposal**: Correctly implement `IAsyncDisposable` (preferred for async cleanup) or `IDisposable` for any class that manages unmanaged resources (e.g., network connections, file handles, native library contexts like LibVLC) or subscribes to events from external or long-lived objects to prevent memory leaks. Implement the standard dispose pattern robustly. In public methods of disposable classes, check the disposal state at the beginning using `ObjectDisposedException.ThrowIf(this.disposed, this);` to provide immediate feedback on incorrect usage.
* **Null Handling**: Enable nullable reference types project-wide via `<Nullable>enable</Nullable>` in `SnapDog2.csproj`. All code must be null-aware. Eliminate compiler warnings related to nullability by explicitly handling potential `null` values using appropriate checks (`is not null`), pattern matching (`is { }`), null-conditional operators (`?.`), null-coalescing operators (`??`), or parameter/property validation. Use the `required` modifier for non-nullable properties in DTOs and configuration classes where initialization is mandatory. Avoid using the null-forgiving operator (`!`) unless it is absolutely necessary and its safety can be guaranteed and justified with a code comment.
* **Encapsulation**: Design components within logical layers (folders) to be highly cohesive (related responsibilities grouped together) and loosely coupled (interactions through interfaces/messages). Strictly adhere to the dependency rule: code should only depend inwards towards the `/Core` layer. `/Infrastructure` depends on `/Core`. `/Server` depends on `/Core`. `/Api` depends on `/Server` and `/Core`. `/Worker` depends on all other layers for composition. Use the `internal` access modifier for types and members that are not intended to be used outside their defining logical component or layer (assembly in this single-project case) to minimize the public API surface and enforce boundaries.

## 2.3. Formatting and Layout

These rules ensure visual consistency and readability. Primarily enforced by `.editorconfig` and StyleCop Analyzers.

* **Indentation**: Use **4 spaces** per indentation level. **Do not use tabs**. (`.editorconfig`: `indent_style = space`, `indent_size = 4`, `tab_width = 4`).
* **Braces**: Use **Allman style** braces, where the opening and closing braces are placed on their own lines, aligned with the preceding code block statement. Braces are **required** for all control flow blocks (`if`, `else`, `for`, `foreach`, `while`, `do`, `using`, `lock`, `try`, `catch`, `finally`), even if the block contains only a single statement (StyleCop SA1503, SA1519, SA1520).

    ```csharp
    // Correct Allman style with required braces
    if (someCondition)
    {
        ExecuteAction();
    }
    else
    {
        LogWarning("Condition not met.");
    }

    foreach (var item in collection)
    {
        ProcessItem(item);
    }
    ```

* **Line Length**: Target a maximum line length of **120 characters**. Enforced via `.editorconfig` (`max_line_length = 120`). Long lines should be broken after operators or commas, indented appropriately on subsequent lines to maintain readability.
* **Spacing**: Adhere strictly to StyleCop SA10xx rules:
  * Single space after keywords (`if`, `for`, `while`, `switch`, `catch`, etc.) and before opening parenthesis (`if (condition)`).
  * Single space around binary operators (`=`, `+`, `-`, `==`, `=>`, etc.).
  * No space after opening or before closing parentheses/brackets (`Method(arg1, arg2)`, `array[index]`).
  * Single space after commas in argument/parameter lists, array initializers, etc.
  * No space before commas or semicolons.
  * Specific rules apply around unary operators, type casts, generic brackets, etc. (Refer to SA10xx rule documentation).
* **`using` Directives**: Place all `using` directives **inside** the `namespace` declaration (StyleCop SA1200 configured via `stylecop.json`). Order directives as follows: `System.*` namespaces first, then other external library namespaces (e.g., `Microsoft.*`, `Knx.*`, `MediatR.*`), finally own project namespaces (`SnapDog2.*`). Within each group, directives must be sorted alphabetically by namespace (StyleCop SA1208, SA1210). Utilize **global usings** (`/Worker/GlobalUsings.cs`) for extremely common namespaces used throughout the application (e.g., `System`, `System.Collections.Generic`, `System.Linq`, `System.Threading.Tasks`, `SnapDog2.Core.Models`).

    ```csharp
    // Example: /Infrastructure/Knx/KnxService.cs
    namespace SnapDog2.Infrastructure.Knx; // File-scoped namespace

    // Usings INSIDE namespace, System first, then external, then internal, alphabetical within groups
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;

    using Knx.Falcon; // External library namespaces
    using Knx.Falcon.Configuration;
    using Knx.Falcon.Discovery;
    using Knx.Falcon.KnxnetIp;
    using Knx.Falcon.Sdk;
    using MediatR;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Polly;
    using Polly.Retry;

    using SnapDog2.Core.Abstractions; // Own project namespaces
    using SnapDog2.Core.Configuration;
    using SnapDog2.Core.Models;
    using SnapDog2.Infrastructure.Resilience; // If policies are here
    using SnapDog2.Server.Notifications; // Referencing notifications

    /// <summary>
    /// KNX Service Implementation.
    /// </summary>
    public partial class KnxService : IKnxService, IAsyncDisposable
    {
        // ... code ...
    }
    ```

* **File Organization**: Strictly one public type (class, interface, struct, enum, record, delegate) per file (SA1402). The filename must match the public type name exactly, including casing (SA1649). Use **file-scoped namespaces** (`namespace MyNamespace;`) as the default (C# 10+).
* **Blank Lines**: Use single blank lines judiciously to separate logical blocks of code (e.g., between methods, between property definitions, between fields and constructors, within methods to separate complex steps). Avoid multiple consecutive blank lines (SA1516, SA1517). Follow specific StyleCop rules for blank lines around braces and comments (SA1505, SA1508, SA1512, SA1513, etc.).

## 2.4. Naming Conventions

Consistent naming is crucial for readability and understanding code intent.

* **Types** (`class`, `struct`, `record`, `enum`, `delegate`, `interface`): Use `PascalCase`. Interfaces **must** be prefixed with `I` (SA1302).
  * Examples: `ZoneManager`, `PlaybackStatus`, `TrackInfo`, `IZoneService`, `VolumeChangedHandler`.
* **Methods** (Synchronous and Asynchronous), **Properties**, **Events**: Use `PascalCase`. Asynchronous methods (returning `Task` or `ValueTask`) **must** have the `Async` suffix.
  * Examples: `GetZoneStateAsync`, `Volume`, `PlaybackStateChanged`, `ConnectAsync`, `InitializeMediaPlayer`.
* **Public Fields** (Generally discouraged; use Properties), **Public Constants (`public const`)**: Use `PascalCase`.
  * Example: `public const int DefaultTimeoutMilliseconds = 10000;`.
* **Local Variables**, **Method Parameters**: Use `camelCase`. Names should be descriptive. Avoid single-letter variables except in very small scopes (e.g., LINQ lambdas `x => x.Id`, short loops `for(int i=0;...)`).
  * Examples: `int currentVolume`, `string playlistId`, `CancellationToken cancellationToken`.
* **Private/Protected Fields**: Use `_camelCase` (prefix `_`, then camelCase, enforced by SX1309). This clearly distinguishes instance fields.
  * Examples: `private readonly ILogger<MyClass> _logger;`, `private SemaphoreSlim _stateLock;`.
* **Static Readonly Fields**: Use `PascalCase` if they represent logical constants or are publicly accessible (rare). Use `_camelCase` if they are private implementation details (more common).
* **Constants (`private const`, `internal const`)**: Use `PascalCase` (SA1303).
  * Example: `private const string RadioPlaylistId = "radio";`.
* **Type Parameters** (Generics): Use `TPascalCase` (prefix `T`, then descriptive PascalCase name, SA1314).
  * Examples: `Result<TResponse>`, `IRequestHandler<TCommand, TResult>`, `List<TZoneConfig>`.
* **Abbreviations**: Treat common acronyms (2-3 letters) as words unless only two letters. Capitalize only the first letter or keep all caps if standard (like `IO`). Prefer full words over abbreviations where clarity is improved.
  * Correct: `HtmlParser`, `GetZoneId`, `UseApiAuth`, `IoService`.
  * Incorrect: `HTMLParser`, `GetZoneID`, `UseAPIAuth`.
* **Hungarian Notation**: Strictly forbidden for all identifiers (variables, fields, parameters, etc.) (SA1305, SA1309). Do not use prefixes indicating type (e.g., `strName`, `iCount`, `bEnabled`).

## 2.5. Logging (LoggerMessage Source Generator Pattern)

Logging is performed exclusively through the `Microsoft.Extensions.Logging.ILogger<T>` interface, injected via constructor. The **LoggerMessage Source Generator pattern is mandatory** for defining log messages to ensure high performance and compile-time validation.

1. **Declare Class as `partial`:** The class using the logger must be declared `partial`.
2. **Inject `ILogger<T>`:** Inject the logger via the constructor.
3. **Define `partial void` Methods:** For each distinct log message, define a `private partial void` method.
4. **Decorate with `[LoggerMessage]`:** Apply the `LoggerMessage` attribute to the partial method.
    * `EventId`: Assign a unique integer ID within the class/component for structured filtering. Consider a convention (e.g., 1xx for Class A, 2xx for Class B).
    * `Level`: Specify the appropriate `LogLevel` (Trace, Debug, Information, Warning, Error, Critical).
    * `Message`: Define the log message template using curly brace placeholders (`{PlaceHolderName}`). Placeholder names should match method parameter names.
5. **Method Signature:** Define parameters matching the placeholders in the `Message` string, using specific types (e.g., `int`, `string`, `Guid`). Include an `Exception` parameter if the log should capture exception details.
6. **Call Generated Method:** Invoke the defined partial method directly in your code where the log event should occur, passing the required arguments.

```csharp
// Example within /Server/Managers/ClientManager.cs
namespace SnapDog2.Server.Managers;

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Models;
using SnapDog2.Server.Notifications; // For publishing internal events

/// <summary>
/// Manages Snapcast clients and their relationship to SnapDog2 zones.
/// Must be declared partial for LoggerMessage generation.
/// </summary>
public partial class ClientManager : IClientManager, // Implements Core abstraction
                                     INotificationHandler<SnapcastClientConnectedNotification>, // Handles MediatR notification
                                     IAsyncDisposable
{
    private readonly ISnapcastService _snapcastService;
    private readonly IMediator _mediator;
    private readonly ILogger<ClientManager> _logger;
    private readonly ISnapcastStateRepository _stateRepository; // Read raw state
    // Internal state mapping (example - adjust as needed)
    private readonly ConcurrentDictionary<string, int> _snapcastIdToInternalId = new();
    private readonly ConcurrentDictionary<int, ClientState> _internalClientStates = new();
    private readonly ConcurrentDictionary<string, int> _lastKnownZoneAssignment = new(); // snapcastId -> zoneId
    private readonly SemaphoreSlim _stateLock = new SemaphoreSlim(1, 1);
    private bool _disposed = false;

    // --- LoggerMessage Definitions ---
    [LoggerMessage(EventId = 301, Level = LogLevel.Information, Message = "Initializing ClientManager. Discovering initial clients...")]
    private partial void LogInitializing();

    [LoggerMessage(EventId = 302, Level = LogLevel.Error, Message = "Failed to discover initial Snapcast clients.")]
    private partial void LogDiscoveryError(Exception ex);

    [LoggerMessage(EventId = 303, Level = LogLevel.Information, Message = "Discovered {ClientCount} initial Snapcast clients.")]
    private partial void LogDiscoveryComplete(int clientCount);

    [LoggerMessage(EventId = 304, Level = LogLevel.Information, Message = "Handling SnapcastClientConnected event for SnapcastId {SnapcastId}. Client Name: {ClientName}")]
    private partial void LogHandlingClientConnected(string snapcastId, string clientName);

    [LoggerMessage(EventId = 305, Level = LogLevel.Information, Message = "Handling SnapcastClientDisconnected event for SnapcastId {SnapcastId}.")]
    private partial void LogHandlingClientDisconnected(string snapcastId);

    [LoggerMessage(EventId = 306, Level = LogLevel.Information, Message = "Client {SnapcastId} (Internal ID: {InternalId}) updated/added.")]
    private partial void LogClientUpdated(string snapcastId, int internalId);

    [LoggerMessage(EventId = 307, Level = LogLevel.Warning, Message = "Could not find internal mapping for Snapcast Client {SnapcastId} during update.")]
    private partial void LogMappingNotFoundWarning(string snapcastId);

    [LoggerMessage(EventId = 308, Level = LogLevel.Information, Message = "Assigning Client (Internal ID: {InternalId}, Snapcast ID: {SnapcastId}) to Zone {ZoneId}.")]
    private partial void LogAssigningClientToZone(int internalId, string snapcastId, int zoneId);

    [LoggerMessage(EventId = 309, Level = LogLevel.Error, Message = "Failed to assign Client {InternalId} to Zone {ZoneId}.")]
    private partial void LogAssignClientError(int internalId, int zoneId, Exception? ex = null); // Optional exception


    public ClientManager(
        ISnapcastService snapcastService,
        IMediator mediator,
        ISnapcastStateRepository stateRepository,
        ILogger<ClientManager> logger /*, List<ClientConfig> clientConfigs */)
    {
        _snapcastService = snapcastService;
        _mediator = mediator;
        _stateRepository = stateRepository;
        _logger = logger;
        // Potentially load default zone assignments from ClientConfig here
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        LogInitializing();
        try
        {
            // Fetch initial full state to populate repository if SnapcastService doesn't do it
            // Alternatively, rely on SnapcastService populating the State Repository
            var initialClients = _stateRepository.GetAllClients(); // Get initial state
            int count = 0;
            await _stateLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                foreach (var client in initialClients)
                {
                    UpdateInternalClientState(client); // Update internal mappings/state
                    count++;
                }
            }
            finally { _stateLock.Release(); }

            LogDiscoveryComplete(count);
        }
        catch(Exception ex)
        {
            LogDiscoveryError(ex);
            // Decide if this is fatal? Maybe throw?
        }
    }

    // Example Notification Handler
    public async Task Handle(SnapcastClientConnectedNotification notification, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        LogHandlingClientConnected(notification.Client.Id, notification.Client.Config.Name);
        await _stateLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            UpdateInternalClientState(notification.Client); // Update state based on event

            // Re-assign to last known zone if needed (Option B logic)
            if (_lastKnownZoneAssignment.TryGetValue(notification.Client.Id, out int lastZoneId))
            {
                 var currentGroup = _stateRepository.GetAllGroups().FirstOrDefault(g => g.Clients.Any(c => c.Id == notification.Client.Id));
                 if(currentGroup == null) // Only assign if not already in a group
                 {
                      LogAssigningClientToZone( /* Get internal ID */ -1, notification.Client.Id, lastZoneId);
                      // Call AssignClientToZoneAsync (needs internal ID lookup first)
                      // var assignResult = await AssignClientToZoneAsync(internalId, lastZoneId).ConfigureAwait(false);
                      // if(assignResult.IsFailure) LogAssignClientError(...);
                 }
            }
        }
        finally { _stateLock.Release(); }
    }

    // Helper to update internal state and mappings (needs lock externally)
    private void UpdateInternalClientState(Sturd.SnapcastNet.Models.Client snapClient)
    {
         int internalId;
         if(!_snapcastIdToInternalId.TryGetValue(snapClient.Id, out internalId))
         {
              // Assign new internal ID (use Interlocked or ensure lock)
              internalId = _snapcastIdToInternalId.Count + 1; // Simplistic ID generation - NEEDS IMPROVEMENT
              _snapcastIdToInternalId.TryAdd(snapClient.Id, internalId);
         }

         // Create/Update SnapDog2 ClientState record (using mapping logic)
         var clientState = MapSnapClientToClientState(internalId, snapClient);
         _internalClientStates[internalId] = clientState;

         // Update last known zone if client is in a group
          var group = _stateRepository.GetAllGroups().FirstOrDefault(g => g.Clients.Any(c => c.Id == snapClient.Id));
          if(group != null && _zoneManager.TryGetZoneIdByGroupId(group.Id, out int zoneId)) { // Assume ZoneManager has TryGet method
               _lastKnownZoneAssignment[snapClient.Id] = zoneId;
               // Update ZoneId in _internalClientStates record if different
               if(clientState.ZoneId != zoneId) {
                    _internalClientStates[internalId] = clientState with { ZoneId = zoneId };
               }
          } else {
               // Client is not in a known group, remove last known assignment?
               // Or keep it for reconnection logic? Keep it for now.
               // Ensure ZoneId is null in internal state if not in a group
                if(clientState.ZoneId != null) {
                    _internalClientStates[internalId] = clientState with { ZoneId = null };
               }
          }

         LogClientUpdated(snapClient.Id, internalId);
    }

     // Mapping function - needs access to _zoneManager or similar for GroupID->ZoneID mapping
     private ClientState MapSnapClientToClientState(int internalId, Sturd.SnapcastNet.Models.Client snapClient)
     {
          // ... Mapping logic ...
          return new ClientState { Id = internalId, /* map other fields */ ZoneId = null /* Determine ZoneID */};
     }


     // ... other IClientManager methods ...
     // ... DisposeAsync ...
}
```

## 2.6. Disposal Pattern

Use `IAsyncDisposable`/`IDisposable`. Check state using `ObjectDisposedException.ThrowIf`.

```csharp
namespace SnapDog2.Infrastructure.Knx; // File-scoped namespace

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Knx.Falcon.Sdk;

/// <summary>
/// Service for interacting with the KNX bus. Implements IAsyncDisposable for proper cleanup.
/// Must be declared partial to support LoggerMessage source generation.
/// </summary>
public partial class KnxService : IAsyncDisposable // Partial for logging
{
    private bool _disposed = false; // Backing field for disposed state
    private readonly ILogger<KnxService> _logger;
    private KnxBus? _bus; // Example resource to dispose, nullable
    private readonly SemaphoreSlim _connectionLock = new SemaphoreSlim(1, 1);
    private readonly CancellationTokenSource _serviceStoppingCts = new CancellationTokenSource();
    private Timer? _discoveryRetryTimer;

    // Define logger messages using attributes.
    [LoggerMessage(9001, LogLevel.Debug, Message = "Checking disposal state for KnxService.")]
    private partial void LogDisposalCheck();
    [LoggerMessage(9002, LogLevel.Information, Message = "Disposing KnxService resources.")]
    private partial void LogDisposing();
    [LoggerMessage(9003, LogLevel.Error, Message = "Error during KnxService disposal.")]
    private partial void LogDisposeError(Exception ex);

    // ... Constructor ...

    /// <summary>
    /// Example public asynchronous method performing KNX operations.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task PublicMethodAsync(CancellationToken cancellationToken)
    {
        // LogDisposalCheck(); // Optional: Log the check if useful for debugging.
        ObjectDisposedException.ThrowIf(_disposed, this); // Check disposed status at method entry.

        // ... method implementation using _bus ...
        await Task.Delay(10, cancellationToken).ConfigureAwait(false); // Example async work
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
    /// </summary>
    /// <returns>A ValueTask representing the disposal operation.</returns>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return; // Prevent double disposal.
        LogDisposing(); // Log the start of disposal.
        _disposed = true; // Set flag early to prevent race conditions.

        // Signal cancellation to ongoing operations like connection retries
        _serviceStoppingCts.Cancel();

        // Dispose managed resources that implement IDisposable/IAsyncDisposable
        _discoveryRetryTimer?.Dispose();
        _connectionLock.Dispose();
        _serviceStoppingCts.Dispose();

        // Dispose the KnxBus resource asynchronously
        try
        {
            if (_bus != null)
            {
                 // Unhook event handlers carefully to avoid issues if already disconnected/disposed
                 try { _bus.ConnectionStateChanged -= OnConnectionStateChanged; } catch { /* Ignore potential errors during unsubscription */ }
                 try { _bus.GroupMessageReceived -= OnGroupValueReceived; } catch { /* Ignore */ }
                 try { _bus.GroupReadReceived -= OnGroupReadReceived; } catch { /* Ignore */ }

                 // Attempt to gracefully disconnect if possible
                 if (_bus.ConnectionState == BusConnectionState.Connected) {
                     try {
                          // Use a short timeout for disconnection during disposal
                          using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                          await _bus.DisconnectAsync(cts.Token).ConfigureAwait(false);
                     } catch (Exception disconnectEx) {
                          LogDebug("Exception during KNX bus disconnect on dispose: {Message}", disconnectEx.Message); // Log disconnect issues as debug
                     }
                 }
                 // Dispose the bus itself
                 await _bus.DisposeAsync().ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            LogDisposeError(ex); // Log errors during disposal but do not throw.
        }
        finally
        {
             _bus = null; // Allow garbage collection.
        }

        GC.SuppressFinalize(this); // Suppress finalization if IDisposable pattern is correctly implemented.
    }

    // Placeholder event handlers needed for Dispose cleanup
     private void OnConnectionStateChanged(object? sender, EventArgs e) { }
     private void OnGroupValueReceived(object? sender, Knx.Falcon.GroupEventArgs e) { }
     private void OnGroupReadReceived(object? sender, Knx.Falcon.GroupEventArgs e) { }

     // Example of another log message definition
     [LoggerMessage(9004, LogLevel.Debug, "Exception during KNX bus disconnect on dispose: {Message}")]
     private partial void LogDebug(string message, params object[] args);
}
```

## 2.7. StyleCop Enforcement Summary

Enforced via `stylecop.json` / build: Public API Docs (SA1600+), File Headers (SA1633), Usings inside namespace (SA1200), Member Order (SA1201+), Naming (SA13xx, SX1309), Spacing (SA10xx), Readability (SA11xx, SX1101), Layout/Braces (SA15xx), Maintainability (SA14xx).

## 2.8. StyleCop Configuration (`stylecop.json`)

```json
{
  "$schema": "https://raw.githubusercontent.com/DotNetAnalyzers/StyleCopAnalyzers/master/StyleCop.Analyzers/StyleCop.Analyzers/Settings/stylecop.schema.json",
  "settings": {
    "documentationRules": {
      "companyName": "Fabian Schmieder",
      "copyrightText": "// <copyright file=\"{filename}\" company=\"Fabian Schmieder\">\n// This file is part of SnapDog2.\n//\n// SnapDog2 is free software: you can redistribute it and/or modify\n// it under the terms of the GNU General Public License as published by\n// the Free Software Foundation, either version 3 of the License, or\n// (at your option) any later version.\n//\n// SnapDog2 is distributed in the hope that it will be useful,\n// but WITHOUT ANY WARRANTY; without even the implied warranty of\n// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the\n// GNU General Public License for more details.\n//\n// You should have received a copy of the GNU General Public License\n// along with SnapDog. If not, see https://www.gnu.org/licenses/.\n// </copyright>",
      "xmlHeader": false,
      "fileNamingConvention": "stylecop",
      "documentInternalElements": false
    },
    "orderingRules": {
      "usingDirectivesPlacement": "insideNamespace",
      "systemUsingDirectivesFirst": true
    },
    "namingRules": {
      "allowedHungarianPrefixes": [],
      "allowCommonHungarianPrefixes": false,
       "includeInferredTupleElementNames": true,
       "tupleElementNameCasing": "PascalCase"
    },
     "readabilityRules": {
       "allowBuiltInTypeAliases": false
     },
    "maintainabilityRules": {
        "topLevelTypes": ["class", "interface", "struct", "enum", "delegate", "record"]
    },
    "layoutRules": {
      "newlineAtEndOfFile": "require",
       "allowConsecutiveUsings": false
    },
     "indentation": {
        "indentationSize": 4,
        "tabSize": 4,
        "useTabs": false
    }
  }
}
```
