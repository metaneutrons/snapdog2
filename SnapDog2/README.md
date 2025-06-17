# SnapDog2 Phase 1 - Console Application Demo & Test Coverage

This document describes the Phase 1 implementation of SnapDog2, which includes a comprehensive console application demo and extensive test coverage for all core domain components.

## Overview

Phase 1 focuses on:

- ✅ Core domain entities and value objects
- ✅ Immutable state management with thread-safety
- ✅ Domain events infrastructure with MediatR
- ✅ Comprehensive validation system using FluentValidation
- ✅ Configuration system with EnvoyConfig
- ✅ Console application demonstration
- ✅ Extensive unit test coverage

## Architecture

### Core Components

#### Domain Entities

- **Zone**: Represents audio zones with client assignments and stream configuration
- **Client**: Snapcast clients with connection status and volume control
- **AudioStream**: Audio streams with codec and bitrate information
- **Playlist**: Music playlists with track management
- **Track**: Individual music tracks with metadata
- **RadioStation**: Internet radio stations

#### Value Objects

- **MacAddress**: Normalized MAC addresses with validation
- **IpAddress**: IP address wrapper with IPv4/IPv6 support
- **StreamUrl**: Validated URLs for audio streams

#### State Management

- **SnapDogState**: Immutable state container for all entities
- **StateManager**: Thread-safe state management with optimistic concurrency
- **StateExtensions**: Helper methods for state manipulation

#### Events Infrastructure

- **DomainEvent**: Base class for all domain events
- **InMemoryEventPublisher**: MediatR-based event publishing
- Specific events: ClientConnected, VolumeChanged, PlaylistUpdated, etc.

#### Configuration System

- **SnapDogConfiguration**: Main configuration container
- **EnvoyConfig**: Environment-based configuration loading
- **FluentValidation**: Comprehensive configuration validation

## Getting Started

### Prerequisites

- .NET 9.0 SDK
- Visual Studio 2022 or VS Code with C# extension

### Running the Demo

1. **Build the solution:**

   ```bash
   dotnet build
   ```

2. **Run the console demo:**

   ```bash
   cd SnapDog2
   dotnet run
   ```

3. **Run with specific configuration:**

   ```bash
   # Set environment variables
   export SNAPDOG_SYSTEM__APPLICATIONNAME="SnapDog2 Custom"
   export SNAPDOG_API__PORT=5001
   dotnet run
   ```

### Running Tests

1. **Run all tests:**

   ```bash
   dotnet test
   ```

2. **Run with code coverage:**

   ```bash
   dotnet test --collect:"XPlat Code Coverage"
   ```

3. **Run specific test category:**

   ```bash
   dotnet test --filter "Category=Unit"
   ```

## Demo Features

The console application demonstrates:

### 1. Configuration Demo

- Environment-based configuration loading
- Validation with detailed error reporting
- Custom type converters (KnxAddress)

### 2. Domain Entities Demo

- Entity creation with immutable patterns
- Business logic validation
- Relationship management (zones ↔ clients)

### 3. State Management Demo

- Thread-safe state operations
- Optimistic concurrency control
- State validation and consistency checks

### 4. Events Demo

- Domain event publishing
- Event correlation and tracing
- Batch event processing

### 5. Validation Demo

- Entity validation scenarios
- Value object validation
- Configuration validation

### 6. Multi-threading Demo

- Concurrent state updates
- Thread-safety verification
- Performance benchmarking

## Test Coverage

### Entity Tests

- **ZoneTests**: 25+ test scenarios covering creation, validation, and manipulation
- **ClientTests**: 30+ test scenarios for client lifecycle and operations
- **MacAddressTests**: 20+ test scenarios for value object behavior

### State Management Tests

- **StateManagerTests**: 25+ test scenarios for thread-safe operations
- Concurrency control testing
- Event handling verification

### Event System Tests

- **EventPublisherTests**: Event publishing and subscription patterns
- Correlation and tracing verification
- Error handling scenarios

### Configuration Tests

- **ConfigurationTests**: Environment loading and validation
- **KnxAddressTests**: Custom type converter testing

## Key Design Patterns

### 1. Immutable Domain Models

```csharp
public sealed record Zone
{
    public Zone WithAddedClient(string clientId) =>
        this with {
            ClientIds = ClientIds.Add(clientId),
            UpdatedAt = DateTime.UtcNow
        };
}
```

### 2. Thread-Safe State Management

```csharp
public SnapDogState UpdateState(Func<SnapDogState, SnapDogState> updateFunction)
{
    lock (_stateLock)
    {
        var newState = updateFunction(_currentState);
        ValidateState(newState);
        _currentState = newState;
        return newState;
    }
}
```

### 3. Value Objects with Validation

```csharp
public readonly struct MacAddress : IEquatable<MacAddress>
{
    public MacAddress(string macAddress)
    {
        if (!IsValid(macAddress))
            throw new ArgumentException($"Invalid MAC address: {macAddress}");
        Value = NormalizeMacAddress(macAddress);
    }
}
```

### 4. Domain Events

```csharp
public sealed record ClientConnectedEvent : DomainEvent
{
    public static ClientConnectedEvent Create(string clientId, string clientName, ...)
    {
        return new ClientConnectedEvent
        {
            ClientId = clientId,
            ClientName = clientName,
            OccurredAt = DateTime.UtcNow
        };
    }
}
```

## Configuration

### Environment Variables

```bash
# System Configuration
SNAPDOG_SYSTEM__APPLICATIONNAME="SnapDog2"
SNAPDOG_SYSTEM__VERSION="1.0.0"
SNAPDOG_SYSTEM__ENVIRONMENT="Development"

# API Configuration
SNAPDOG_API__PORT=5000
SNAPDOG_API__HTTPSENABLED=false

# Telemetry Configuration
SNAPDOG_TELEMETRY__ENABLED=true
SNAPDOG_TELEMETRY__SERVICENAME="snapdog2"
```

### Configuration File (appsettings.json)

```json
{
  "System": {
    "ApplicationName": "SnapDog2",
    "Version": "1.0.0",
    "Environment": "Development"
  },
  "Api": {
    "Port": 5000,
    "HttpsEnabled": false
  },
  "Zones": [
    {
      "Id": "living-room",
      "Name": "Living Room",
      "Description": "Main entertainment area"
    }
  ]
}
```

## Logging

The application uses structured logging with Serilog:

- Console output with colored formatting
- File logging with rolling intervals
- Structured log data for observability

## Performance Characteristics

### State Management

- ✅ Thread-safe operations with minimal locking
- ✅ Immutable state transitions
- ✅ O(1) entity lookups using dictionaries

### Memory Usage

- ✅ Immutable collections for efficient memory usage
- ✅ Structural sharing in record types
- ✅ Minimal allocations in hot paths

### Concurrency

- ✅ Lock-free reads from immutable state
- ✅ Optimistic concurrency control
- ✅ Event publishing with async patterns

## Error Handling

### Validation Errors

- Comprehensive validation at entity boundaries
- Detailed error messages with property paths
- Fail-fast validation for invalid operations

### State Consistency

- Referential integrity checks
- Automatic rollback on validation failures
- State consistency verification

### Exception Handling

- Structured exception handling throughout
- Logging of all errors with context
- Graceful degradation where possible

## Next Steps (Phase 2)

- Infrastructure layer implementation
- External service integrations (Snapcast, MQTT)
- Real-time communication with WebSockets
- Audio streaming protocol handlers
- Database persistence layer

## Contributing

1. Follow the established patterns for immutable domain models
2. Add comprehensive unit tests for new features
3. Use FluentValidation for all validation logic
4. Implement proper logging and telemetry
5. Maintain thread-safety in all shared state

## References

- [Domain-Driven Design](https://martinfowler.com/tags/domain%20driven%20design.html)
- [Immutable Domain Models](https://enterprisecraftsmanship.com/posts/immutable-domain-model/)
- [Event-Driven Architecture](https://martinfowler.com/articles/201701-event-driven.html)
- [.NET 9 Features](https://docs.microsoft.com/en-us/dotnet/core/whats-new/dotnet-9)
- [FluentValidation](https://fluentvalidation.net/)
- [MediatR](https://github.com/jbogard/MediatR)
