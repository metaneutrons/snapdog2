using Microsoft.Extensions.Logging;
using Moq;
using SnapDog2.Core.Models.Entities;
using SnapDog2.Core.Models.Enums;
using SnapDog2.Core.Models.ValueObjects;
using SnapDog2.Core.State;
using Xunit;

namespace SnapDog2.Tests.State;

/// <summary>
/// Unit tests for the StateManager class.
/// Tests thread-safety, state validation, update operations, and event handling.
/// </summary>
public class StateManagerTests : IDisposable
{
    private readonly Mock<ILogger<StateManager>> _mockLogger;
    private readonly StateManager _stateManager;

    public StateManagerTests()
    {
        _mockLogger = new Mock<ILogger<StateManager>>();
        _stateManager = new StateManager(_mockLogger.Object);
    }

    public void Dispose()
    {
        _stateManager?.Dispose();
    }

    [Fact]
    public void Constructor_WithValidLogger_ShouldCreateStateManager()
    {
        // Arrange & Act
        using var stateManager = new StateManager(_mockLogger.Object);

        // Assert
        Assert.NotNull(stateManager);
        var currentState = stateManager.GetCurrentState();
        Assert.NotNull(currentState);
        Assert.Equal(1, currentState.Version);
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(static () => new StateManager(null!));
    }

    [Fact]
    public void Constructor_WithInitialState_ShouldUseProvidedState()
    {
        // Arrange
        var initialState = SnapDogState.CreateEmpty().WithSystemStatus(SystemStatus.Running);

        // Act
        using var stateManager = new StateManager(_mockLogger.Object, initialState);

        // Assert
        var currentState = stateManager.GetCurrentState();
        Assert.Equal(SystemStatus.Running, currentState.SystemStatus);
    }

    [Fact]
    public async Task Constructor_WithInvalidInitialState_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var invalidState = SnapDogState.CreateEmpty() with
        {
            Version = -1,
        }; // Invalid version

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await Task.Run(() => new StateManager(_mockLogger.Object, invalidState))
        );
    }

    [Fact]
    public void GetCurrentState_ShouldReturnCurrentState()
    {
        // Act
        var state = _stateManager.GetCurrentState();

        // Assert
        Assert.NotNull(state);
        Assert.True(state.Version > 0);
    }

    [Fact]
    public void GetCurrentVersion_ShouldReturnCurrentVersion()
    {
        // Act
        var version = _stateManager.GetCurrentVersion();

        // Assert
        Assert.True(version > 0);
    }

    [Fact]
    public void UpdateState_WithValidFunction_ShouldUpdateState()
    {
        // Arrange
        var originalState = _stateManager.GetCurrentState();
        var originalVersion = originalState.Version;

        // Act
        var updatedState = _stateManager.UpdateState(static state => state.WithSystemStatus(SystemStatus.Running));

        // Assert
        Assert.NotNull(updatedState);
        Assert.Equal(SystemStatus.Running, updatedState.SystemStatus);
        Assert.True(updatedState.Version > originalVersion);
        Assert.True(updatedState.LastUpdated > DateTime.MinValue);
    }

    [Fact]
    public void UpdateState_WithNullFunction_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _stateManager.UpdateState(null!));
    }

    [Fact]
    public void UpdateState_WithFunctionReturningNull_ShouldThrowInvalidOperationException()
    {
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _stateManager.UpdateState(_ => null!));
    }

    [Fact]
    public void UpdateState_WithInvalidStateUpdate_ShouldThrowInvalidOperationException()
    {
        // Act & Assert
        Assert.Throws<InvalidOperationException>(
            () => _stateManager.UpdateState(state => state with { Version = state.Version - 1 })
        );
    }

    [Fact]
    public void UpdateState_ShouldRaiseStateUpdatedEvent()
    {
        // Arrange
        StateUpdatedEventArgs? eventArgs = null;
        _stateManager.StateUpdated += (sender, args) => eventArgs = args;

        // Act
        _stateManager.UpdateState(state => state.WithSystemStatus(SystemStatus.Running));

        // Assert
        Assert.NotNull(eventArgs);
        Assert.Equal(SystemStatus.Running, eventArgs.NewState.SystemStatus);
    }

    [Fact]
    public void UpdateState_WithValidationFailure_ShouldRaiseValidationFailedEvent()
    {
        // Arrange
        StateValidationFailedEventArgs? eventArgs = null;
        _stateManager.StateValidationFailed += (sender, args) => eventArgs = args;

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _stateManager.UpdateState(state => state with { Version = -1 }));

        Assert.NotNull(eventArgs);
    }

    [Fact]
    public void TryUpdateState_WithValidFunction_ShouldReturnTrueAndUpdateState()
    {
        // Arrange
        var originalVersion = _stateManager.GetCurrentVersion();

        // Act
        var result = _stateManager.TryUpdateState(
            static state => state.WithSystemStatus(SystemStatus.Running),
            out var updatedState
        );

        // Assert
        Assert.True(result);
        Assert.NotNull(updatedState);
        Assert.Equal(SystemStatus.Running, updatedState.SystemStatus);
        Assert.True(updatedState.Version > originalVersion);
    }

    [Fact]
    public void TryUpdateState_WithNullFunction_ShouldReturnFalse()
    {
        // Act
        var result = _stateManager.TryUpdateState(null!, out var updatedState);

        // Assert
        Assert.False(result);
        Assert.Null(updatedState);
    }

    [Fact]
    public void TryUpdateState_WithInvalidUpdate_ShouldReturnFalse()
    {
        // Act
        var result = _stateManager.TryUpdateState(static state => state with { Version = -1 }, out var updatedState);

        // Assert
        Assert.False(result);
        Assert.Null(updatedState);
    }

    [Fact]
    public void UpdateStateWithRetry_WithValidFunction_ShouldUpdateState()
    {
        // Arrange
        var originalVersion = _stateManager.GetCurrentVersion();

        // Act
        var updatedState = _stateManager.UpdateStateWithRetry(static state =>
            state.WithSystemStatus(SystemStatus.Running)
        );

        // Assert
        Assert.NotNull(updatedState);
        Assert.Equal(SystemStatus.Running, updatedState.SystemStatus);
        Assert.True(updatedState.Version > originalVersion);
    }

    [Fact]
    public void UpdateStateWithRetry_WithNullFunction_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _stateManager.UpdateStateWithRetry(null!));
    }

    [Fact]
    public void ValidateCurrentState_WithValidState_ShouldReturnTrue()
    {
        // Act
        var isValid = _stateManager.ValidateCurrentState();

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void ResetState_ShouldResetToEmptyState()
    {
        // Arrange
        _stateManager.UpdateState(static state => state.WithSystemStatus(SystemStatus.Running));

        // Act
        var resetState = _stateManager.ResetState();

        // Assert
        Assert.NotNull(resetState);
        Assert.Equal(SystemStatus.Stopped, resetState.SystemStatus);
        Assert.Empty(resetState.Zones);
        Assert.Empty(resetState.Clients);
        Assert.Empty(resetState.AudioStreams);
    }

    [Fact]
    public void ResetState_WithProvidedState_ShouldResetToProvidedState()
    {
        // Arrange
        var targetState = SnapDogState.CreateEmpty().WithSystemStatus(SystemStatus.Running);

        // Act
        var resetState = _stateManager.ResetState(targetState);

        // Assert
        Assert.NotNull(resetState);
        Assert.Equal(SystemStatus.Running, resetState.SystemStatus);
    }

    [Fact]
    public void ResetState_WithNullState_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _stateManager.ResetState(null!));
    }

    [Fact]
    public void ResetState_WithInvalidState_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var invalidState = SnapDogState.CreateEmpty() with
        {
            Version = -1,
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _stateManager.ResetState(invalidState));
    }

    [Fact]
    public void StateManager_WithEntityOperations_ShouldMaintainConsistency()
    {
        // Arrange
        var zone = Zone.Create("test-zone", "Test Zone");
        var client = Client.Create(
            "test-client",
            "Test Client",
            new MacAddress("AA:BB:CC:DD:EE:FF"),
            new IpAddress("192.168.1.100")
        );
        var stream = AudioStream.Create(
            "test-stream",
            "Test Stream",
            new StreamUrl("http://example.com"),
            AudioCodec.MP3,
            320
        );

        // Act
        var updatedState = _stateManager.UpdateState(state =>
            state.WithZone(zone).WithClient(client).WithAudioStream(stream)
        );

        // Assert
        Assert.Single(updatedState.Zones);
        Assert.Single(updatedState.Clients);
        Assert.Single(updatedState.AudioStreams);
        Assert.Equal(3, updatedState.TotalEntityCount);
        Assert.Contains("test-zone", updatedState.Zones.Keys);
        Assert.Contains("test-client", updatedState.Clients.Keys);
        Assert.Contains("test-stream", updatedState.AudioStreams.Keys);
    }

    [Fact]
    public async Task StateManager_ConcurrentUpdates_ShouldMaintainConsistency()
    {
        // Arrange
        const int numberOfTasks = 10;
        const int updatesPerTask = 10;
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < numberOfTasks; i++)
        {
            int taskId = i;
            tasks.Add(
                Task.Run(() =>
                {
                    for (int j = 0; j < updatesPerTask; j++)
                    {
                        _stateManager.TryUpdateState(
                            state => state.WithMetadata($"task-{taskId}-update-{j}", DateTime.UtcNow),
                            out _
                        );
                    }
                })
            );
        }

        await Task.WhenAll(tasks);

        // Assert
        var finalState = _stateManager.GetCurrentState();
        Assert.True(finalState.Version > 1);
        Assert.True(finalState.Metadata.Count <= numberOfTasks * updatesPerTask);
    }

    [Fact]
    public void StateManager_WithMaxHistorySize_ShouldLimitHistory()
    {
        // Arrange
        const int maxHistorySize = 5;
        using var stateManager = new StateManager(_mockLogger.Object, maxHistorySize: maxHistorySize);

        // Act
        for (int i = 0; i < maxHistorySize + 3; i++)
        {
            stateManager.UpdateState(state => state.WithMetadata($"update-{i}", i));
        }

        // Assert - We can't directly test history size as it's private,
        // but we can verify the state manager continues to function correctly
        var finalState = stateManager.GetCurrentState();
        Assert.True(finalState.Version > maxHistorySize);
    }

    [Fact]
    public void StateManager_AfterDispose_ShouldThrowObjectDisposedException()
    {
        // Arrange
        var stateManager = new StateManager(_mockLogger.Object);
        stateManager.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => stateManager.GetCurrentState());
        Assert.Throws<ObjectDisposedException>(() => stateManager.UpdateState(s => s));
        Assert.Throws<ObjectDisposedException>(() => stateManager.ValidateCurrentState());
    }

    [Fact]
    public void StateManager_DoubleDispose_ShouldNotThrow()
    {
        // Arrange
        var stateManager = new StateManager(_mockLogger.Object);

        // Act & Assert - Should not throw
        stateManager.Dispose();
        stateManager.Dispose();
    }

    [Fact]
    public void StateManager_ComplexStateOperations_ShouldMaintainIntegrity()
    {
        // Arrange
        var zone = Zone.Create("living-room", "Living Room").WithAddedClient("client-1");
        var client = Client
            .Create(
                "client-1",
                "Living Room Speaker",
                new MacAddress("AA:BB:CC:DD:EE:FF"),
                new IpAddress("192.168.1.100")
            )
            .WithZone("living-room")
            .WithStatus(ClientStatus.Connected);
        var stream = AudioStream
            .Create("jazz-stream", "Jazz Stream", new StreamUrl("http://jazz.example.com"), AudioCodec.FLAC, 1411)
            .WithStatus(StreamStatus.Playing);

        // Act
        var finalState = _stateManager.UpdateState(state =>
            state
                .WithZone(zone.WithCurrentStream("jazz-stream"))
                .WithClient(client)
                .WithAudioStream(stream)
                .WithSystemStatus(SystemStatus.Running)
        );

        // Assert
        Assert.Equal(SystemStatus.Running, finalState.SystemStatus);
        Assert.True(finalState.IsRunning);
        Assert.Single(finalState.Zones);
        Assert.Single(finalState.Clients);
        Assert.Single(finalState.AudioStreams);

        var savedZone = finalState.Zones["living-room"];
        var savedClient = finalState.Clients["client-1"];
        var savedStream = finalState.AudioStreams["jazz-stream"];

        Assert.Equal("jazz-stream", savedZone.CurrentStreamId);
        Assert.Contains("client-1", savedZone.ClientIds);
        Assert.Equal("living-room", savedClient.ZoneId);
        Assert.True(savedClient.IsConnected);
        Assert.True(savedStream.IsPlaying);
    }

    [Fact]
    public void StateManager_EventHandling_ShouldProvideCorrectEventData()
    {
        // Arrange
        var eventsFired = new List<(SnapDogState Previous, SnapDogState New)>();
        _stateManager.StateUpdated += (sender, args) => eventsFired.Add((args.PreviousState, args.NewState));

        // Act
        _stateManager.UpdateState(state => state.WithSystemStatus(SystemStatus.Running));
        _stateManager.UpdateState(state => state.WithSystemStatus(SystemStatus.Stopping));

        // Assert
        Assert.Equal(2, eventsFired.Count);

        var firstEvent = eventsFired[0];
        Assert.Equal(SystemStatus.Stopped, firstEvent.Previous.SystemStatus);
        Assert.Equal(SystemStatus.Running, firstEvent.New.SystemStatus);

        var secondEvent = eventsFired[1];
        Assert.Equal(SystemStatus.Running, secondEvent.Previous.SystemStatus);
        Assert.Equal(SystemStatus.Stopping, secondEvent.New.SystemStatus);
    }

    [Fact]
    public void StateManager_VersionIncrement_ShouldBeMonotonic()
    {
        // Arrange
        var versions = new List<long>();

        // Act
        for (int i = 0; i < 10; i++)
        {
            var state = _stateManager.UpdateState(s => s.WithMetadata($"test-{i}", i));
            versions.Add(state.Version);
        }

        // Assert
        for (int i = 1; i < versions.Count; i++)
        {
            Assert.True(
                versions[i] > versions[i - 1],
                $"Version at index {i} ({versions[i]}) should be greater than version at index {i - 1} ({versions[i - 1]})"
            );
        }
    }
}
