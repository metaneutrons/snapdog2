using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Models.Entities;
using SnapDog2.Core.Models.Enums;
using SnapDog2.Core.Models.ValueObjects;
using SnapDog2.Core.State;

namespace SnapDog2.Core.Demo;

/// <summary>
/// Demonstrates state management capabilities including thread-safe operations,
/// state transitions, validation, and performance characteristics.
/// </summary>
public class StateManagementDemo
{
    private readonly ILogger<StateManagementDemo> _logger;
    private readonly IStateManager _stateManager;

    public StateManagementDemo(ILogger<StateManagementDemo> logger, IStateManager stateManager)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
    }

    /// <summary>
    /// Runs the complete state management demonstration.
    /// </summary>
    public async Task RunDemoAsync()
    {
        _logger.LogInformation("=== State Management Demo ===");

        try
        {
            await DemonstrateBasicStateOperationsAsync();
            await DemonstrateStateValidationAsync();
            await DemonstrateStateTransitionsAsync();
            await DemonstrateOptimisticConcurrencyAsync();
            await DemonstrateStateExtensionsAsync();

            _logger.LogInformation("State management demo completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "State management demo failed");
            throw;
        }
    }

    /// <summary>
    /// Demonstrates basic state operations: get, update, reset.
    /// </summary>
    private async Task DemonstrateBasicStateOperationsAsync()
    {
        _logger.LogInformation("--- Basic State Operations Demo ---");

        // Get initial state
        var initialState = _stateManager.GetCurrentState();
        _logger.LogInformation(
            "Initial state: Version {Version}, Entities {EntityCount}, Status {Status}",
            initialState.Version,
            initialState.TotalEntityCount,
            initialState.SystemStatus
        );

        // Add some entities
        var updatedState = _stateManager.UpdateState(static state =>
        {
            var zone = Zone.Create("demo-zone", "Demo Zone", "Demo zone for testing");
            var client = Client.Create(
                "demo-client",
                "Demo Client",
                new MacAddress("AA:BB:CC:DD:EE:FF"),
                new IpAddress("192.168.1.100"),
                ClientStatus.Connected,
                75
            );
            var stream = AudioStream.Create(
                "demo-stream",
                "Demo Stream",
                new StreamUrl("http://demo.example.com"),
                AudioCodec.MP3,
                320
            );

            return state
                .WithZone(zone)
                .WithClient(client)
                .WithAudioStream(stream)
                .WithSystemStatus(SystemStatus.Running);
        });

        _logger.LogInformation(
            "Updated state: Version {Version}, Entities {EntityCount}, Status {Status}",
            updatedState.Version,
            updatedState.TotalEntityCount,
            updatedState.SystemStatus
        );

        // Validate state
        var isValid = _stateManager.ValidateCurrentState();
        _logger.LogInformation("State is valid: {IsValid}", isValid);

        await Task.Delay(100);
    }

    /// <summary>
    /// Demonstrates state validation scenarios.
    /// </summary>
    private async Task DemonstrateStateValidationAsync()
    {
        _logger.LogInformation("--- State Validation Demo ---");

        // Test valid state update
        var validUpdate = _stateManager.TryUpdateState(
            static state =>
            {
                var zone = Zone.Create("valid-zone", "Valid Zone");
                var client = Client.Create(
                    "valid-client",
                    "Valid Client",
                    new MacAddress("BB:CC:DD:EE:FF:AA"),
                    new IpAddress("192.168.1.101"),
                    ClientStatus.Connected,
                    50
                );

                return state.WithZone(zone.WithAddedClient("valid-client")).WithClient(client.WithZone("valid-zone"));
            },
            out var validState
        );

        _logger.LogInformation(
            "Valid state update succeeded: {Success}, New version: {Version}",
            validUpdate,
            validState?.Version
        );

        // Test state consistency validation
        var currentState = _stateManager.GetCurrentState();
        var isConsistent = currentState.IsValid();
        _logger.LogInformation("State consistency check: {IsConsistent}", isConsistent);

        await Task.Delay(100);
    }

    /// <summary>
    /// Demonstrates various state transitions and entity relationships.
    /// </summary>
    private async Task DemonstrateStateTransitionsAsync()
    {
        _logger.LogInformation("--- State Transitions Demo ---");

        // Simulate client connection sequence
        _stateManager.UpdateState(static state =>
        {
            var client = state.Clients.Values.FirstOrDefault();
            if (client != null)
            {
                var disconnectedClient = client.WithStatus(ClientStatus.Disconnected);
                return state.WithClient(disconnectedClient);
            }
            return state;
        });

        _logger.LogInformation("Client disconnected");

        await Task.Delay(500);

        _stateManager.UpdateState(static state =>
        {
            var client = state.Clients.Values.FirstOrDefault();
            if (client != null)
            {
                var reconnectedClient = client.WithStatus(ClientStatus.Connected);
                return state.WithClient(reconnectedClient);
            }
            return state;
        });

        _logger.LogInformation("Client reconnected");

        // Simulate stream status changes
        _stateManager.UpdateState(static state =>
        {
            var stream = state.AudioStreams.Values.FirstOrDefault();
            if (stream != null)
            {
                var playingStream = stream.WithStatus(StreamStatus.Playing);
                return state.WithAudioStream(playingStream);
            }
            return state;
        });

        _logger.LogInformation("Stream started playing");

        await Task.Delay(1000);

        _stateManager.UpdateState(static state =>
        {
            var stream = state.AudioStreams.Values.FirstOrDefault();
            if (stream != null)
            {
                var stoppedStream = stream.WithStatus(StreamStatus.Stopped);
                return state.WithAudioStream(stoppedStream);
            }
            return state;
        });

        _logger.LogInformation("Stream stopped");

        await Task.Delay(100);
    }

    /// <summary>
    /// Demonstrates optimistic concurrency control and retry mechanisms.
    /// </summary>
    private async Task DemonstrateOptimisticConcurrencyAsync()
    {
        _logger.LogInformation("--- Optimistic Concurrency Demo ---");

        // Test retry mechanism
        var retryResult = _stateManager.UpdateStateWithRetry(
            static state =>
            {
                // Simulate a potentially conflicting update
                var zone = state.Zones.Values.FirstOrDefault();
                if (zone != null)
                {
                    var updatedZone = zone.WithVolumeSettings(
                        defaultVolume: Random.Shared.Next(20, 80),
                        minVolume: 0,
                        maxVolume: 100
                    );
                    return state.WithZone(updatedZone);
                }
                return state;
            },
            maxRetries: 3
        );

        _logger.LogInformation("Retry update completed: Version {Version}", retryResult.Version);

        // Get current version for demonstration
        var currentVersion = _stateManager.GetCurrentVersion();
        _logger.LogInformation("Current state version: {Version}", currentVersion);

        await Task.Delay(100);
    }

    /// <summary>
    /// Demonstrates state extension methods and helper functions.
    /// </summary>
    private async Task DemonstrateStateExtensionsAsync()
    {
        _logger.LogInformation("--- State Extensions Demo ---");

        var currentState = _stateManager.GetCurrentState();

        // Demonstrate state query capabilities
        _logger.LogInformation("State statistics:");
        _logger.LogInformation("  - Total entities: {Count}", currentState.TotalEntityCount);
        _logger.LogInformation("  - Audio streams: {Count}", currentState.AudioStreams.Count);
        _logger.LogInformation("  - Clients: {Count}", currentState.Clients.Count);
        _logger.LogInformation("  - Zones: {Count}", currentState.Zones.Count);
        _logger.LogInformation("  - System running: {IsRunning}", currentState.IsRunning);
        _logger.LogInformation("  - Has errors: {HasError}", currentState.HasError);

        // Demonstrate metadata usage
        _stateManager.UpdateState(static state =>
            state
                .WithMetadata("demo-timestamp", DateTime.UtcNow)
                .WithMetadata("demo-operation", "state-extensions-demo")
        );

        var stateWithMetadata = _stateManager.GetCurrentState();
        _logger.LogInformation("State metadata count: {Count}", stateWithMetadata.Metadata.Count);

        await Task.Delay(100);
    }

    /// <summary>
    /// Simulates concurrent state updates for multi-threading demo.
    /// </summary>
    public async Task SimulateConcurrentUpdatesAsync(int workerId, CancellationToken cancellationToken)
    {
        var updateCount = 0;
        while (!cancellationToken.IsCancellationRequested && updateCount < 10)
        {
            try
            {
                var success = _stateManager.TryUpdateState(
                    state =>
                    {
                        // Simulate random updates by different workers
                        var metadata = state.Metadata.SetItem($"worker-{workerId}-update", updateCount);
                        return state with { Metadata = metadata, Version = state.Version + 1 };
                    },
                    out _
                );

                if (success)
                {
                    updateCount++;
                    _logger.LogDebug("Worker {WorkerId}: Update {Count} successful", workerId, updateCount);
                }
                else
                {
                    _logger.LogDebug("Worker {WorkerId}: Update {Count} failed", workerId, updateCount);
                }

                await Task.Delay(Random.Shared.Next(50, 200), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Worker {WorkerId}: Error during update {Count}", workerId, updateCount);
            }
        }

        _logger.LogInformation("Worker {WorkerId} completed {Count} updates", workerId, updateCount);
    }

    /// <summary>
    /// Benchmarks state update performance.
    /// </summary>
    public async Task BenchmarkStateUpdatesAsync()
    {
        // Quick state update for performance testing
        _stateManager.TryUpdateState(
            static state => state.WithMetadata($"benchmark-{Guid.NewGuid()}", DateTime.UtcNow.Ticks),
            out _
        );

        await Task.Delay(1); // Minimal delay to simulate async work
    }

    /// <summary>
    /// Demonstrates error scenarios in state management.
    /// </summary>
    public async Task DemonstrateErrorScenariosAsync()
    {
        _logger.LogInformation("--- Error Scenarios Demo ---");

        // Test null update function
        try
        {
            _stateManager.UpdateState(null!);
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogInformation("Caught expected ArgumentNullException: {Message}", ex.Message);
        }

        // Test update function that returns null
        try
        {
            _stateManager.UpdateState(static _ => null!);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogInformation("Caught expected InvalidOperationException: {Message}", ex.Message);
        }

        // Test invalid state reset
        try
        {
            _stateManager.ResetState(null!);
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogInformation("Caught expected ArgumentNullException for reset: {Message}", ex.Message);
        }

        await Task.Delay(100);
    }
}
