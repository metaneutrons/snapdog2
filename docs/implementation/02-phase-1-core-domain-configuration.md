# Phase 1: Core Domain & Configuration

## Overview

Phase 1 implements the complete domain model, value objects, and configuration system. This phase establishes the rich domain foundation that all subsequent layers will build upon.

**Deliverable**: Domain-rich console application with complete configuration system and state management.

## Objectives

### Primary Goals

- [ ] Implement all domain entities (AudioStream, Client, Zone, Playlist, RadioStation)
- [ ] Create comprehensive value objects and enumerations
- [ ] Implement immutable state management with records
- [ ] Complete EnvoyConfig integration for all configuration scenarios
- [ ] Establish domain events foundation
- [ ] Implement validation patterns with FluentValidation

### Success Criteria

- All domain entities implemented with proper business rules
- Configuration system handles all blueprint scenarios
- State management follows immutable patterns
- Domain events infrastructure in place
- 95%+ test coverage for all domain logic
- Console application demonstrates complete configuration loading

## Prerequisites

### Dependencies

- Phase 0 successfully completed with all quality gates passed
- Project structure established
- Testing infrastructure operational
- AI collaboration templates validated

### Knowledge Requirements

- Domain-Driven Design (DDD) principles
- Immutable record types in C#
- State management patterns
- FluentValidation library
- EnvoyConfig advanced scenarios

## Implementation Steps

### Step 1: Domain Entity Implementation

#### 1.1 Core Domain Models

Create the fundamental domain entities following DDD principles:

**AudioStream Entity:**

```csharp
namespace SnapDog.Core.Models;

/// <summary>
/// Represents an audio stream in the SnapDog system with codec and quality settings.
/// </summary>
/// <param name="Name">Human-readable name for the stream</param>
/// <param name="Codec">Audio codec used for encoding</param>
/// <param name="SampleRate">Sample rate in Hz (e.g., 44100, 48000)</param>
/// <param name="BitDepth">Bit depth for audio samples (e.g., 16, 24)</param>
/// <param name="Channels">Number of audio channels (1=mono, 2=stereo)</param>
public record AudioStream(
    string Name,
    AudioCodec Codec,
    int SampleRate,
    int BitDepth,
    int Channels)
{
    public int Id { get; init; }
    public StreamStatus Status { get; private set; } = StreamStatus.Stopped;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? LastStartedAt { get; private set; }
    public string? SnapcastSinkName { get; init; }

    // Constructor validation
    public AudioStream(string Name, AudioCodec Codec, int SampleRate, int BitDepth, int Channels) : this(Name, Codec, SampleRate, BitDepth, Channels)
    {
        if (string.IsNullOrWhiteSpace(Name))
            throw new ArgumentException("Stream name cannot be empty", nameof(Name));

        if (SampleRate <= 0)
            throw new ArgumentException("Sample rate must be positive", nameof(SampleRate));

        if (BitDepth <= 0)
            throw new ArgumentException("Bit depth must be positive", nameof(BitDepth));

        if (Channels <= 0)
            throw new ArgumentException("Channels must be positive", nameof(Channels));
    }

    /// <summary>
    /// Starts the audio stream if currently stopped.
    /// </summary>
    /// <returns>Success result or failure with error message</returns>
    public Result Start()
    {
        if (Status == StreamStatus.Active)
            return Result.Failure("Stream is already active");

        Status = StreamStatus.Active;
        LastStartedAt = DateTime.UtcNow;
        return Result.Success();
    }

    /// <summary>
    /// Stops the audio stream if currently active.
    /// </summary>
    /// <returns>Success result or failure with error message</returns>
    public Result Stop()
    {
        if (Status == StreamStatus.Stopped)
            return Result.Failure("Stream is already stopped");

        Status = StreamStatus.Stopped;
        return Result.Success();
    }
}
```

#### 1.2 AI Collaboration: Domain Entity Creation

**AI Prompt Template:**

```
SNAPDOG DOMAIN ENTITY IMPLEMENTATION

Context: SnapDog multi-room audio streaming system, Phase 1 - Core Domain implementation

Entity: [EntityName]
Business Rules: [Specific business rules from blueprint]

Requirements:
- Use C# record types for immutability
- Include proper validation in constructor
- Implement business methods for state transitions
- Follow blueprint naming conventions from Document 02
- Include XML documentation for all public members
- Use Result<T> pattern for operations that can fail

Domain Rules:
- Audio streams have lifecycle: Stopped -> Active -> Stopped
- Clients can be connected/disconnected with latency tracking
- Zones contain multiple clients and map 1:1 to Snapcast groups
- Playlists contain tracks with position tracking
- Radio stations have URL validation and metadata

Blueprint References:
- Document 04: Core Components & State Management
- Document 20: Glossary for domain terminology

Generate complete entity implementation with:
1. Immutable record definition
2. Constructor validation
3. Business methods for state changes
4. Proper error handling
5. XML documentation
```

#### 1.3 Implement All Domain Entities

Create complete implementations for:

- **Client**: Snapcast client with connection state and latency
- **Zone**: Audio zone with client assignments and volume control
- **Playlist**: Music playlist with track management
- **RadioStation**: Internet radio with URL and metadata
- **Track**: Individual music track with metadata

### Step 2: Value Objects and Enumerations

#### 2.1 Create Value Objects

```csharp
namespace SnapDog.Core.ValueObjects;

/// <summary>
/// Represents a MAC address for network device identification.
/// </summary>
public record MacAddress
{
    public string Value { get; }

    public MacAddress(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("MAC address cannot be empty", nameof(value));

        if (!IsValidMacAddress(value))
            throw new ArgumentException("Invalid MAC address format", nameof(value));

        Value = value.ToUpperInvariant();
    }

    private static bool IsValidMacAddress(string mac)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(
            mac, @"^([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})$");
    }

    public static implicit operator string(MacAddress macAddress) => macAddress.Value;
    public static explicit operator MacAddress(string value) => new(value);
}

/// <summary>
/// Represents an IP address with validation.
/// </summary>
public record IpAddress
{
    public string Value { get; }

    public IpAddress(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("IP address cannot be empty", nameof(value));

        if (!System.Net.IPAddress.TryParse(value, out _))
            throw new ArgumentException("Invalid IP address format", nameof(value));

        Value = value;
    }

    public static implicit operator string(IpAddress ipAddress) => ipAddress.Value;
    public static explicit operator IpAddress(string value) => new(value);
}
```

#### 2.2 Domain Enumerations

```csharp
namespace SnapDog.Core.Enums;

/// <summary>
/// Supported audio codecs for streaming.
/// </summary>
public enum AudioCodec
{
    /// <summary>Free Lossless Audio Codec - high quality, larger files</summary>
    FLAC,
    /// <summary>Opus codec - efficient, low latency</summary>
    Opus,
    /// <summary>Pulse Code Modulation - uncompressed</summary>
    PCM,
    /// <summary>Ogg Vorbis - open standard, good compression</summary>
    OggVorbis
}

/// <summary>
/// Current status of an audio stream.
/// </summary>
public enum StreamStatus
{
    /// <summary>Stream is not currently playing</summary>
    Stopped,
    /// <summary>Stream is actively playing</summary>
    Active,
    /// <summary>Stream is temporarily paused</summary>
    Paused,
    /// <summary>Stream encountered an error</summary>
    Error
}

/// <summary>
/// Connection status of a Snapcast client.
/// </summary>
public enum ClientStatus
{
    /// <summary>Client is connected and available</summary>
    Connected,
    /// <summary>Client is disconnected</summary>
    Disconnected,
    /// <summary>Client connection is unknown or uncertain</summary>
    Unknown
}
```

### Step 3: State Management Implementation

#### 3.1 Immutable State Patterns

```csharp
namespace SnapDog.Core.State;

/// <summary>
/// Immutable state representation for the SnapDog system.
/// </summary>
/// <param name="Streams">All audio streams in the system</param>
/// <param name="Clients">All connected clients</param>
/// <param name="Zones">All audio zones</param>
/// <param name="Playlists">All available playlists</param>
/// <param name="LastUpdated">Timestamp of last state update</param>
public record SnapDogState(
    IReadOnlyList<AudioStream> Streams,
    IReadOnlyList<Client> Clients,
    IReadOnlyList<Zone> Zones,
    IReadOnlyList<Playlist> Playlists,
    DateTime LastUpdated)
{
    public static SnapDogState Empty => new(
        Array.Empty<AudioStream>(),
        Array.Empty<Client>(),
        Array.Empty<Zone>(),
        Array.Empty<Playlist>(),
        DateTime.UtcNow);

    /// <summary>
    /// Creates a new state with an updated stream.
    /// </summary>
    public SnapDogState WithUpdatedStream(AudioStream updatedStream)
    {
        var updatedStreams = Streams
            .Where(s => s.Id != updatedStream.Id)
            .Append(updatedStream)
            .ToList();

        return this with
        {
            Streams = updatedStreams,
            LastUpdated = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a new state with an added client.
    /// </summary>
    public SnapDogState WithAddedClient(Client newClient)
    {
        var updatedClients = Clients.Append(newClient).ToList();
        return this with
        {
            Clients = updatedClients,
            LastUpdated = DateTime.UtcNow
        };
    }
}
```

#### 3.2 State Manager Service

```csharp
namespace SnapDog.Core.Services;

/// <summary>
/// Manages immutable state for the SnapDog system with thread-safe operations.
/// </summary>
public interface IStateManager
{
    /// <summary>Gets the current system state</summary>
    SnapDogState CurrentState { get; }

    /// <summary>Updates state and notifies subscribers</summary>
    Task UpdateStateAsync(Func<SnapDogState, SnapDogState> updateFunction);

    /// <summary>Subscribes to state changes</summary>
    IDisposable Subscribe(Action<SnapDogState> onStateChanged);
}

public class StateManager : IStateManager
{
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly List<Action<SnapDogState>> _subscribers = new();
    private SnapDogState _currentState = SnapDogState.Empty;

    public SnapDogState CurrentState
    {
        get
        {
            _lock.EnterReadLock();
            try
            {
                return _currentState;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }

    public async Task UpdateStateAsync(Func<SnapDogState, SnapDogState> updateFunction)
    {
        SnapDogState newState;
        List<Action<SnapDogState>> currentSubscribers;

        _lock.EnterWriteLock();
        try
        {
            newState = updateFunction(_currentState);
            _currentState = newState;
            currentSubscribers = new List<Action<SnapDogState>>(_subscribers);
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        // Notify subscribers outside of lock
        await Task.Run(() =>
        {
            foreach (var subscriber in currentSubscribers)
            {
                try
                {
                    subscriber(newState);
                }
                catch
                {
                    // Log error but don't fail state update
                }
            }
        });
    }

    public IDisposable Subscribe(Action<SnapDogState> onStateChanged)
    {
        _lock.EnterWriteLock();
        try
        {
            _subscribers.Add(onStateChanged);
            return new Unsubscriber(() =>
            {
                _lock.EnterWriteLock();
                try
                {
                    _subscribers.Remove(onStateChanged);
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            });
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    private class Unsubscriber : IDisposable
    {
        private readonly Action _unsubscribe;
        private bool _disposed;

        public Unsubscriber(Action unsubscribe)
        {
            _unsubscribe = unsubscribe;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _unsubscribe();
                _disposed = true;
            }
        }
    }
}
```

### Step 4: Complete Configuration System

#### 4.1 Advanced EnvoyConfig Integration

```csharp
namespace SnapDog.Core.Configuration;

/// <summary>
/// Complete SnapDog configuration with all subsystem settings.
/// </summary>
public class SnapDogConfiguration
{
    [Env(Key = "SNAPDOG_ENVIRONMENT", Default = "Development")]
    public string Environment { get; set; } = "Development";

    [Env(Key = "SNAPDOG_LOG_LEVEL", Default = "Information")]
    public string LogLevel { get; set; } = "Information";

    [Env(Key = "SNAPDOG_API_PORT", Default = 5000)]
    public int ApiPort { get; set; } = 5000;

    // Snapcast server configuration
    [Env(Key = "SNAPCAST_SERVER_HOST", Default = "localhost")]
    public string SnapcastServerHost { get; set; } = "localhost";

    [Env(Key = "SNAPCAST_SERVER_PORT", Default = 1705)]
    public int SnapcastServerPort { get; set; } = 1705;

    [Env(Key = "SNAPCAST_CONNECTION_TIMEOUT", Default = 5000)]
    public int SnapcastConnectionTimeoutMs { get; set; } = 5000;

    // MQTT configuration
    [Env(Key = "MQTT_BROKER_HOST")]
    public string? MqttBrokerHost { get; set; }

    [Env(Key = "MQTT_BROKER_PORT", Default = 1883)]
    public int MqttBrokerPort { get; set; } = 1883;

    [Env(Key = "MQTT_USERNAME")]
    public string? MqttUsername { get; set; }

    [Env(Key = "MQTT_PASSWORD")]
    public string? MqttPassword { get; set; }

    [Env(Key = "MQTT_BASE_TOPIC", Default = "SNAPDOG")]
    public string MqttBaseTopic { get; set; } = "SNAPDOG";

    // KNX configuration
    [Env(Key = "KNX_ENABLED", Default = false)]
    public bool KnxEnabled { get; set; }

    [Env(Key = "KNX_CONNECTION_TYPE", Default = "IP")]
    public string KnxConnectionType { get; set; } = "IP";

    [Env(Key = "KNX_GATEWAY_HOST")]
    public string? KnxGatewayHost { get; set; }

    [Env(Key = "KNX_GATEWAY_PORT", Default = 3671)]
    public int KnxGatewayPort { get; set; } = 3671;

    // Observability configuration
    [Env(Key = "TELEMETRY_ENABLED", Default = true)]
    public bool TelemetryEnabled { get; set; } = true;

    [Env(Key = "TELEMETRY_SERVICE_NAME", Default = "snapdog")]
    public string TelemetryServiceName { get; set; } = "snapdog";

    [Env(Key = "JAEGER_ENDPOINT")]
    public string? JaegerEndpoint { get; set; }

    [Env(Key = "PROMETHEUS_ENABLED", Default = true)]
    public bool PrometheusEnabled { get; set; } = true;

    [Env(Key = "PROMETHEUS_PORT", Default = 9090)]
    public int PrometheusPort { get; set; } = 9090;

    // Client configurations
    [Env(NestedListPrefix = "SNAPDOG_CLIENT_", NestedListSuffix = "_")]
    public List<ClientConfiguration> Clients { get; set; } = new();

    // Radio station configurations
    [Env(NestedListPrefix = "SNAPDOG_RADIO_", NestedListSuffix = "_")]
    public List<RadioStationConfiguration> RadioStations { get; set; } = new();

    // Zone configurations
    [Env(NestedListPrefix = "SNAPDOG_ZONE_", NestedListSuffix = "_")]
    public List<ZoneConfiguration> Zones { get; set; } = new();
}

/// <summary>
/// Configuration for individual Snapcast clients.
/// </summary>
public class ClientConfiguration
{
    [Env(Key = "NAME")]
    public string Name { get; set; } = string.Empty;

    [Env(Key = "MAC")]
    public string MacAddress { get; set; } = string.Empty;

    [Env(Key = "DESCRIPTION")]
    public string? Description { get; set; }

    [Env(Key = "ZONE_ID")]
    public int? ZoneId { get; set; }

    // MQTT configuration for this client
    [Env(Key = "MQTT_VOLUME_TOPIC")]
    public string? MqttVolumeTopic { get; set; }

    [Env(Key = "MQTT_MUTE_TOPIC")]
    public string? MqttMuteTopic { get; set; }

    // KNX configuration for this client
    [Env(Key = "KNX_ENABLED", Default = false)]
    public bool KnxEnabled { get; set; }

    [Env(Key = "KNX_VOLUME_GA")]
    public string? KnxVolumeGroupAddress { get; set; }

    [Env(Key = "KNX_MUTE_GA")]
    public string? KnxMuteGroupAddress { get; set; }

    [Env(Key = "KNX_STATUS_GA")]
    public string? KnxStatusGroupAddress { get; set; }
}
```

#### 4.2 Configuration Validation

```csharp
namespace SnapDog.Core.Validation;

/// <summary>
/// Validates SnapDog configuration for completeness and correctness.
/// </summary>
public class ConfigurationValidator : AbstractValidator<SnapDogConfiguration>
{
    public ConfigurationValidator()
    {
        RuleFor(x => x.Environment)
            .NotEmpty()
            .WithMessage("Environment must be specified");

        RuleFor(x => x.LogLevel)
            .Must(BeValidLogLevel)
            .WithMessage("Log level must be one of: Trace, Debug, Information, Warning, Error, Critical");

        RuleFor(x => x.ApiPort)
            .InclusiveBetween(1024, 65535)
            .WithMessage("API port must be between 1024 and 65535");

        RuleFor(x => x.SnapcastServerHost)
            .NotEmpty()
            .WithMessage("Snapcast server host must be specified");

        RuleFor(x => x.SnapcastServerPort)
            .InclusiveBetween(1, 65535)
            .WithMessage("Snapcast server port must be between 1 and 65535");

        // MQTT validation
        When(x => !string.IsNullOrEmpty(x.MqttBrokerHost), () =>
        {
            RuleFor(x => x.MqttBrokerPort)
                .InclusiveBetween(1, 65535)
                .WithMessage("MQTT broker port must be between 1 and 65535");

            RuleFor(x => x.MqttBaseTopic())
                .NotEmpty()
                .WithMessage("MQTT base topic must be specified when MQTT is configured");
        });

        // KNX validation
        When(x => x.KnxEnabled, () =>
        {
            RuleFor(x => x.KnxGatewayHost)
                .NotEmpty()
                .WithMessage("KNX gateway host must be specified when KNX is enabled");

            RuleFor(x => x.KnxGatewayPort)
                .InclusiveBetween(1, 65535)
                .WithMessage("KNX gateway port must be between 1 and 65535");
        });

        // Client validation
        RuleForEach(x => x.Clients)
            .SetValidator(new ClientConfigurationValidator());

        // Radio station validation
        RuleForEach(x => x.RadioStations)
            .SetValidator(new RadioStationConfigurationValidator());
    }

    private static bool BeValidLogLevel(string logLevel)
    {
        var validLevels = new[] { "Trace", "Debug", "Information", "Warning", "Error", "Critical" };
        return validLevels.Contains(logLevel, StringComparer.OrdinalIgnoreCase);
    }
}

public class ClientConfigurationValidator : AbstractValidator<ClientConfiguration>
{
    public ClientConfigurationValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Client name is required");

        RuleFor(x => x.MacAddress)
            .NotEmpty()
            .Must(BeValidMacAddress)
            .WithMessage("Valid MAC address is required");

        When(x => x.KnxEnabled, () =>
        {
            RuleFor(x => x.KnxVolumeGroupAddress)
                .NotEmpty()
                .Must(BeValidKnxGroupAddress)
                .WithMessage("Valid KNX group address required when KNX is enabled");
        });
    }

    private static bool BeValidMacAddress(string mac)
    {
        if (string.IsNullOrWhiteSpace(mac)) return false;
        return System.Text.RegularExpressions.Regex.IsMatch(
            mac, @"^([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})$");
    }

    private static bool BeValidKnxGroupAddress(string? ga)
    {
        if (string.IsNullOrWhiteSpace(ga)) return false;
        return System.Text.RegularExpressions.Regex.IsMatch(
            ga, @"^\d{1,2}/\d{1,2}/\d{1,3}$");
    }
}
```

### Step 5: Domain Events Foundation

#### 5.1 Domain Event Infrastructure

```csharp
namespace SnapDog.Core.Events;

/// <summary>
/// Base interface for all domain events.
/// </summary>
public interface IDomainEvent
{
    /// <summary>Unique identifier for this event occurrence</summary>
    Guid EventId { get; }

    /// <summary>When the event occurred</summary>
    DateTime OccurredAt { get; }

    /// <summary>Version for event schema evolution</summary>
    int Version { get; }
}

/// <summary>
/// Base implementation of domain events.
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public virtual int Version => 1;
}

/// <summary>
/// Event published when an audio stream is started.
/// </summary>
/// <param name="StreamId">ID of the stream that was started</param>
/// <param name="StreamName">Name of the stream</param>
/// <param name="StartedBy">User or system that started the stream</param>
public record StreamStartedEvent(
    int StreamId,
    string StreamName,
    string StartedBy) : DomainEvent;

/// <summary>
/// Event published when a client connects to the system.
/// </summary>
/// <param name="ClientId">ID of the connected client</param>
/// <param name="ClientName">Name of the client</param>
/// <param name="MacAddress">MAC address of the client</param>
/// <param name="IpAddress">IP address of the client</param>
public record ClientConnectedEvent(
    int ClientId,
    string ClientName,
    string MacAddress,
    string IpAddress) : DomainEvent;

/// <summary>
/// Event published when zone configuration changes.
/// </summary>
/// <param name="ZoneId">ID of the affected zone</param>
/// <param name="ZoneName">Name of the zone</param>
/// <param name="ChangeType">Type of change (ClientAdded, ClientRemoved, VolumeChanged, etc.)</param>
/// <param name="Details">Additional details about the change</param>
public record ZoneConfigurationChangedEvent(
    int ZoneId,
    string ZoneName,
    string ChangeType,
    Dictionary<string, object> Details) : DomainEvent;
```

### Step 6: Testing Implementation

#### 6.1 Domain Entity Tests

```csharp
[TestClass]
public class AudioStreamTests
{
    [TestMethod]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Arrange
        var name = "Test Stream";
        var codec = AudioCodec.FLAC;
        var sampleRate = 44100;
        var bitDepth = 16;
        var channels = 2;

        // Act
        var stream = new AudioStream(name, codec, sampleRate, bitDepth, channels);

        // Assert
        stream.Name.Should().Be(name);
        stream.Codec.Should().Be(codec);
        stream.SampleRate.Should().Be(sampleRate);
        stream.BitDepth.Should().Be(bitDepth);
        stream.Channels.Should().Be(channels);
        stream.Status.Should().Be(StreamStatus.Stopped);
        stream.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [TestMethod]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow(null)]
    public void Constructor_WithInvalidName_ShouldThrowArgumentException(string invalidName)
    {
        // Act & Assert
        var action = () => new AudioStream(invalidName, AudioCodec.FLAC, 44100, 16, 2);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Stream name cannot be empty*");
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    [DataRow(-44100)]
    public void Constructor_WithInvalidSampleRate_ShouldThrowArgumentException(int invalidSampleRate)
    {
        // Act & Assert
        var action = () => new AudioStream("Test", AudioCodec.FLAC, invalidSampleRate, 16, 2);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Sample rate must be positive*");
    }

    [TestMethod]
    public void Start_WhenStopped_ShouldChangeStatusToActiveAndSetTimestamp()
    {
        // Arrange
        var stream = CreateTestAudioStream();
        var beforeStart = DateTime.UtcNow;

        // Act
        var result = stream.Start();

        // Assert
        result.Should().BeSuccessful();
        stream.Status.Should().Be(StreamStatus.Active);
        stream.LastStartedAt.Should().NotBeNull();
        stream.LastStartedAt.Should().BeOnOrAfter(beforeStart);
    }

    [TestMethod]
    public void Start_WhenAlreadyActive_ShouldReturnFailure()
    {
        // Arrange
        var stream = CreateTestAudioStream();
        stream.Start(); // First start

        // Act
        var result = stream.Start(); // Second start attempt

        // Assert
        result.Should().BeFailure();
        result.Error.Should().Contain("already active");
        stream.Status.Should().Be(StreamStatus.Active); // Status unchanged
    }

    [TestMethod]
    public void Stop_WhenActive_ShouldChangeStatusToStopped()
    {
        // Arrange
        var stream = CreateTestAudioStream();
        stream.Start();

        // Act
        var result = stream.Stop();

        // Assert
        result.Should().BeSuccessful();
        stream.Status.Should().Be(StreamStatus.Stopped);
    }

    [TestMethod]
    public void Stop_WhenAlreadyStopped_ShouldReturnFailure()
    {
        // Arrange
        var stream = CreateTestAudioStream();

        // Act
        var result = stream.Stop();

        // Assert
        result.Should().BeFailure();
        result.Error.Should().Contain("already stopped");
        stream.Status.Should().Be(StreamStatus.Stopped);
    }

    private static AudioStream CreateTestAudioStream()
    {
        return new AudioStream("Test Stream", AudioCodec.FLAC, 44100, 16, 2);
    }
}
```

#### 6.2 Configuration Tests

```csharp
[TestClass]
public class ConfigurationValidationTests
{
    private readonly ConfigurationValidator _validator;

    public ConfigurationValidationTests()
    {
        _validator = new ConfigurationValidator();
    }

    [TestMethod]
    public void Validate_WithValidConfiguration_ShouldPass()
    {
        // Arrange
        var config = CreateValidConfiguration();

        // Act
        var result = _validator.Validate(config);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [TestMethod]
    public void Validate_WithEmptyEnvironment_ShouldFail()
    {
        // Arrange
        var config = CreateValidConfiguration();
        config.Environment = "";

        // Act
        var result = _validator.Validate(config);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(config.Environment));
    }

    [TestMethod]
    public void Validate_WithInvalidLogLevel_ShouldFail()
    {
        // Arrange
        var config = CreateValidConfiguration();
        config.LogLevel = "InvalidLevel";

        // Act
        var result = _validator.Validate(config);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(config.LogLevel) &&
            e.ErrorMessage.Contains("must be one of"));
    }

    [TestMethod]
    public void Validate_WithKnxEnabledButNoGatewayHost_ShouldFail()
    {
        // Arrange
        var config = CreateValidConfiguration();
        config.KnxEnabled = true;
        config.KnxGatewayHost = null;

        // Act
        var result = _validator.Validate(config);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(config.KnxGatewayHost) &&
            e.ErrorMessage.Contains("must be specified when KNX is enabled"));
    }

    private static SnapDogConfiguration CreateValidConfiguration()
    {
        return new SnapDogConfiguration
        {
            Environment = "Development",
            LogLevel = "Information",
            ApiPort = 5000,
            SnapcastServerHost = "localhost",
            SnapcastServerPort = 1705,
            MqttBaseTopic = "SNAPDOG"
        };
    }
}
```

### Step 7: Console Application Enhancement

#### 7.1 Enhanced Program.cs

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using SnapDog.Core.Configuration;
using SnapDog.Core.Services;
using SnapDog.Core.Validation;
using EnvoyConfig;

namespace SnapDog.Worker;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Setup Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .CreateLogger();

        try
        {
            Log.Information("Starting SnapDog Phase 1 - Core Domain & Configuration");

            // Load configuration
            var configuration = EnvoyConfigLoader.Load<SnapDogConfiguration>();

            // Validate configuration
            var validator = new ConfigurationValidator();
            var validationResult = validator.Validate(configuration);

            if (!validationResult.IsValid)
            {
                Log.Error("Configuration validation failed:");
                foreach (var error in validationResult.Errors)
                {
                    Log.Error("  {PropertyName}: {ErrorMessage}", error.PropertyName, error.ErrorMessage);
                }
                return 1;
            }

            Log.Information("Configuration loaded and validated successfully");

            // Build and run application
            var host = CreateHostBuilder(args, configuration).Build();

            // Demonstrate domain functionality
            await DemonstrateDomainFunctionality(host.Services);

            Log.Information("Phase 1 demonstration completed successfully");
            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static IHostBuilder CreateHostBuilder(string[] args, SnapDogConfiguration configuration)
    {
        return Host.CreateDefaultBuilder(args)
            .UseSerilog()
            .ConfigureServices((context, services) =>
            {
                // Register configuration
                services.AddSingleton(configuration);

                // Register core services
                services.AddSingleton<IStateManager, StateManager>();

                // Register validators
                services.AddTransient<ConfigurationValidator>();

                // Register hosted service for demonstration
                services.AddHostedService<Phase1DemonstrationService>();
            });
    }

    private static async Task DemonstrateDomainFunctionality(IServiceProvider services)
    {
        var stateManager = services.GetRequiredService<IStateManager>();
        var configuration = services.GetRequiredService<SnapDogConfiguration>();

        Log.Information("=== Domain Functionality Demonstration ===");

        // Demonstrate domain entity creation
        Log.Information("Creating audio streams...");
        var stream1 = new AudioStream("Living Room", AudioCodec.FLAC, 44100, 16, 2);
        var stream2 = new AudioStream("Kitchen", AudioCodec.Opus, 48000, 16, 2);

        Log.Information("Created streams: {Stream1}, {Stream2}", stream1.Name, stream2.Name);

        // Demonstrate state management
        Log.Information("Updating system state...");
        await stateManager.UpdateStateAsync(state =>
            state.WithAddedStream(stream1).WithAddedStream(stream2));

        var currentState = stateManager.CurrentState;
        Log.Information("Current state has {StreamCount} streams", currentState.Streams.Count);

        // Demonstrate stream operations
        Log.Information("Starting stream: {StreamName}", stream1.Name);
        var startResult = stream1.Start();
        if (startResult.IsSuccess)
        {
            Log.Information("Stream started successfully");
            await stateManager.UpdateStateAsync(state => state.WithUpdatedStream(stream1));
        }

        // Demonstrate configuration loading
        Log.Information("=== Configuration Summary ===");
        Log.Information("Environment: {Environment}", configuration.Environment);
        Log.Information("API Port: {Port}", configuration.ApiPort);
        Log.Information("Snapcast Server: {Host}:{Port}",
            configuration.SnapcastServerHost, configuration.SnapcastServerPort);
        Log.Information("Configured Clients: {ClientCount}", configuration.Clients.Count);
        Log.Information("Configured Radio Stations: {RadioCount}", configuration.RadioStations.Count);
        Log.Information("KNX Enabled: {KnxEnabled}", configuration.KnxEnabled);
        Log.Information("MQTT Configured: {MqttConfigured}", !string.IsNullOrEmpty(configuration.MqttBrokerHost));

        Log.Information("=== Phase 1 Implementation Complete ===");
    }
}

/// <summary>
/// Hosted service for Phase 1 demonstration.
/// </summary>
public class Phase1DemonstrationService : BackgroundService
{
    private readonly IStateManager _stateManager;
    private readonly ILogger<Phase1DemonstrationService> _logger;

    public Phase1DemonstrationService(
        IStateManager stateManager,
        ILogger<Phase1DemonstrationService> logger)
    {
        _stateManager = stateManager;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Subscribe to state changes
        using var subscription = _stateManager.Subscribe(OnStateChanged);

        _logger.LogInformation("Phase 1 demonstration service started");

        // Keep service running until cancellation
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private void OnStateChanged(SnapDogState newState)
    {
        _logger.LogInformation("State updated: {StreamCount} streams, {ClientCount} clients, {ZoneCount} zones",
            newState.Streams.Count, newState.Clients.Count, newState.Zones.Count);
    }
}
```

## Expected Deliverable

### Working Console Application Output

```
[15:30:45 INF] Starting SnapDog Phase 1 - Core Domain & Configuration
[15:30:45 INF] Configuration loaded and validated successfully
[15:30:45 INF] === Domain Functionality Demonstration ===
[15:30:45 INF] Creating audio streams...
[15:30:45 INF] Created streams: Living Room, Kitchen
[15:30:45 INF] Updating system state...
[15:30:45 INF] State updated: 2 streams, 0 clients, 0 zones
[15:30:45 INF] Current state has 2 streams
[15:30:45 INF] Starting stream: Living Room
[15:30:45 INF] Stream started successfully
[15:30:45 INF] State updated: 2 streams, 0 clients, 0 zones
[15:30:45 INF] === Configuration Summary ===
[15:30:45 INF] Environment: Development
[15:30:45 INF] API Port: 5000
[15:30:45 INF] Snapcast Server: localhost:1705
[15:30:45 INF] Configured Clients: 0
[15:30:45 INF] Configured Radio Stations: 0
[15:30:45 INF] KNX Enabled: False
[15:30:45 INF] MQTT Configured: False
[15:30:45 INF] === Phase 1 Implementation Complete ===
[15:30:45 INF] Phase 1 demonstration service started
```

### Test Results

```
Phase 1 Test Results:
===================
Domain Entity Tests: 25/25 passed
Value Object Tests: 12/12 passed
State Management Tests: 15/15 passed
Configuration Tests: 20/20 passed
Validation Tests: 18/18 passed

Total Tests: 90/90 passed
Code Coverage: 96%
```

## Quality Gates

### Code Quality Checklist

- [ ] All domain entities implemented with proper business rules
- [ ] Value objects validate input correctly
- [ ] State management is thread-safe and immutable
- [ ] Configuration system handles all blueprint scenarios
- [ ] Domain events infrastructure ready for Phase 3
- [ ] 95%+ test coverage achieved
- [ ] All tests passing with meaningful assertions

### Architecture Validation

- [ ] Domain model follows DDD principles
- [ ] Core layer has no external dependencies
- [ ] Proper separation between entities and value objects
- [ ] Configuration system extensible for future phases
- [ ] State management supports concurrent access

### AI Collaboration Validation

- [ ] AI templates produce consistent, quality domain code
- [ ] Generated tests cover all business rules
- [ ] Domain complexity handled effectively by AI assistance
- [ ] Configuration scenarios properly implemented with AI help

## Next Steps

Upon successful completion of Phase 1:

1. **Validate all deliverables** against success criteria
2. **Update AI context templates** with Phase 1 implementation details
3. **Prepare for Phase 2** by reviewing infrastructure requirements
4. **Begin Phase 2** with confidence in rich domain foundation

Phase 1 establishes the core domain foundation that all subsequent phases will build upon, ensuring a solid architectural base for the complete SnapDog system.
