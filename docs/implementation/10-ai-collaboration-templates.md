# AI Collaboration Templates

## Overview

This document provides comprehensive AI collaboration templates designed specifically for SnapDog development. These templates ensure consistent, high-quality AI assistance while maintaining architectural integrity and code standards.

## Master Project Context Template

### Complete SnapDog Context

```
SNAPDOG PROJECT CONTEXT

System Overview:
SnapDog is a production-ready, multi-room audio streaming system that integrates with Snapcast servers, KNX building automation, MQTT smart home systems, and Subsonic music servers.

Architecture (Layered):
- Worker: Entry point, composition root, hosted services
- API: REST controllers, authentication, OpenAPI documentation
- Server: Business logic, MediatR CQRS, domain events
- Infrastructure: External services, repositories, fault tolerance
- Core: Domain models, abstractions, configuration

Key Technologies:
- .NET 9, ASP.NET Core, MediatR, FluentValidation
- Snapcast (Sturd.SnapcastNet), KNX (Knx.Falcon.Sdk)
- MQTT (MQTTnet), Subsonic (SubSonicMedia)
- LibVLC for audio processing, Docker for deployment
- OpenTelemetry for observability, Polly for resilience

Configuration:
- Environment variable based using EnvoyConfig
- Supports nested client/radio configurations
- Validation with clear error messages

Quality Standards:
- 90%+ test coverage, TDD approach
- Clean architecture with proper layer separation
- Real-time audio processing requirements
- Production-ready observability and security

Current Phase: [SPECIFY CURRENT PHASE]
Current Task: [SPECIFY CURRENT TASK]
```

## Phase-Specific Context Templates

### Phase 0: Foundation Context

```
PHASE 0 CONTEXT - Foundation & AI Setup

Objectives:
- Establish project structure following blueprint architecture
- Implement EnvoyConfig-based configuration system
- Setup TDD infrastructure with xUnit, Moq, FluentAssertions
- Create basic console application with configuration validation

Key Constraints:
- Follow blueprint folder structure exactly
- Use EnvoyConfig for all configuration loading
- Achieve 90%+ test coverage
- Create working console app that validates configuration

Technologies This Phase:
- EnvoyConfig for configuration
- Serilog for structured logging
- Microsoft.Extensions.Hosting for application lifecycle
- xUnit testing framework

Deliverable:
Console application that loads and validates configuration from environment variables.
```

### Phase 1: Domain Context

```
PHASE 1 CONTEXT - Core Domain & Configuration

Objectives:
- Implement all domain entities (AudioStream, Client, Zone, etc.)
- Create value objects and enumerations
- Implement state management with immutable records
- Complete configuration system integration

Key Constraints:
- Follow DDD principles with rich domain models
- Use record types for immutability
- Implement proper value object validation
- Maintain clean domain boundaries

Domain Entities:
- AudioStream: Represents a Snapcast audio stream
- Client: Snapcast client with connection state
- Zone: Logical audio zone with client assignments
- Playlist: Music playlist management
- RadioStation: Internet radio configuration

Blueprint References:
- Document 04: Core Components & State Management
- Document 10: Configuration System
```

## Domain-Specific Templates

### Audio Streaming Context

```
AUDIO STREAMING DOMAIN CONTEXT

Real-Time Constraints:
- Audio latency must be < 10ms for real-time processing
- Buffer management for continuous audio streaming
- Memory efficiency for large audio buffers
- Thread-safe operations for concurrent audio processing

Key Concepts:
- Snapcast Groups (1:1 mapping with SnapDog Zones)
- Audio Sinks (FIFO pipes for LibVLC)
- Client synchronization across multiple rooms
- Audio codec handling (FLAC, Opus, PCM)

Performance Requirements:
- Support for 10+ concurrent audio streams
- Memory usage optimization for audio buffers
- CPU efficiency for real-time processing
- Network bandwidth optimization

Blueprint Reference: Document 14 - Zone to Client Mapping Strategy
```

### Protocol Integration Context

```
PROTOCOL INTEGRATION CONTEXT

KNX Protocol:
- DPT (Datapoint Type) mappings for commands and status
- Group Address configuration for device communication
- Binary protocol handling with proper validation
- Real-time command processing requirements

MQTT Protocol:
- Topic structure: SNAPDOG/[ZONE]/[COMMAND]/[ACTION]
- QoS levels for different message types
- Last Will Testament (LWT) for system status
- Retained messages for state persistence

Snapcast RPC:
- JSON-RPC 2.0 protocol over TCP/WebSocket
- Real-time state synchronization
- Command acknowledgment and error handling
- Connection resilience with automatic reconnection

Blueprint References:
- Document 09: Command Framework
- Document 12: Infrastructure Services Implementation
```

## Code Generation Templates

### Domain Entity Template

```
DOMAIN ENTITY GENERATION

Context: [Include relevant domain context]

Requirements:
- Use C# record types for immutability
- Include proper validation in constructors
- Implement value equality semantics
- Add XML documentation for public members
- Follow blueprint naming conventions

Template Structure:
```csharp
namespace SnapDog.Core.Models;

/// <summary>
/// [Entity description and purpose]
/// </summary>
/// <param name="[Properties]">[Property descriptions]</param>
public record [EntityName](
    [Property declarations with validation]
)
{
    // Additional methods and validation
    // Static factory methods if needed
    // Business logic methods
}
```

Validation Requirements:

- Null checks for reference types
- Range validation for numeric types
- String length and format validation
- Business rule validation

Example Usage:
[Provide specific example for current entity]

```

### MediatR Handler Template
```

MEDIATR HANDLER GENERATION

Context: [Include CQRS context and specific command/query details]

Requirements:

- Implement IRequestHandler<TRequest, TResponse>
- Include comprehensive error handling
- Add structured logging with correlation IDs
- Implement proper validation
- Follow async/await patterns

Handler Structure:

```csharp
namespace SnapDog.Server.Features.[FeatureArea];

/// <summary>
/// Handles [operation description]
/// </summary>
public class [HandlerName] : IRequestHandler<[RequestType], Result<[ResponseType]>>
{
    private readonly [Dependencies] _dependencies;
    private readonly ILogger<[HandlerName]> _logger;

    public [HandlerName]([Constructor parameters])
    {
        // Constructor implementation with null checks
    }

    public async Task<Result<[ResponseType]>> Handle([RequestType] request, CancellationToken cancellationToken)
    {
        // Implementation with logging, validation, and error handling
    }
}
```

Patterns:

- Use Result<T> pattern for error handling
- Log entry/exit with correlation IDs
- Validate inputs with FluentValidation
- Handle cancellation tokens properly

```

### Test Generation Template
```

TEST GENERATION

Context: [Include testing context and component under test]

Requirements:

- Follow AAA pattern (Arrange, Act, Assert)
- Use descriptive test names: MethodName_StateUnderTest_ExpectedBehavior
- Include edge cases and error scenarios
- Use FluentAssertions for readable assertions
- Mock external dependencies with Moq

Test Structure:

```csharp
[TestClass]
public class [ComponentName]Tests
{
    private readonly Mock<[Dependency]> _mock[Dependency];
    private readonly [ComponentName] _sut; // System Under Test

    public [ComponentName]Tests()
    {
        // Setup mocks and system under test
    }

    [TestMethod]
    public async Task [MethodName]_[StateUnderTest]_[ExpectedBehavior]()
    {
        // Arrange
        [Setup test data and expectations]

        // Act
        var result = await _sut.[MethodName]([parameters]);

        // Assert
        result.Should().[Assertions];
        _mock[Dependency].Verify([Verification], Times.[Frequency]);
    }
}
```

Test Categories:

- Unit tests for business logic
- Integration tests for external dependencies
- Performance tests for real-time requirements

```

## Quality Assurance Templates

### Architecture Compliance Template
```

ARCHITECTURE COMPLIANCE VALIDATION

Context: [Current implementation context]

Validation Checklist:
□ Layer dependencies follow blueprint (Core → Server → Infrastructure/API → Worker)
□ No circular dependencies between layers
□ Abstractions defined in Core layer
□ Implementations in appropriate layers
□ Proper use of dependency injection
□ Configuration follows EnvoyConfig patterns

Code Review Focus:

- Namespace organization matches folder structure
- Public APIs properly documented
- Error handling follows Result<T> pattern
- Async/await used correctly
- Resource disposal properly implemented

Blueprint Compliance:

- Check against Document 03: System Architecture
- Validate coding standards per Document 02
- Verify testing approach per Document 18

```

### Performance Validation Template
```

PERFORMANCE VALIDATION

Context: [Audio streaming performance requirements]

Real-Time Requirements:

- Audio processing latency: < 10ms
- API response time: < 100ms
- Memory usage: Efficient buffer management
- CPU usage: Optimized for concurrent processing

Testing Approach:

- Benchmark critical audio processing paths
- Load testing for concurrent streams
- Memory profiling for buffer management
- Latency measurement for real-time operations

Performance Monitoring:

- OpenTelemetry metrics collection
- Custom performance counters
- Memory allocation tracking
- Network bandwidth monitoring

```

## AI Prompting Best Practices

### Effective Prompting Patterns

#### Progressive Complexity
```

1. Start Simple: Begin with basic structure
2. Add Features: Incrementally add complexity
3. Refine Quality: Focus on error handling, logging
4. Optimize: Performance and memory optimization

```

#### Context Layering
```

1. Project Context: Overall system understanding
2. Phase Context: Current development phase
3. Task Context: Specific implementation task
4. Technical Context: Relevant technical constraints

```

#### Validation Loops
```

1. Generate: Create initial implementation
2. Review: Check against architecture and standards
3. Test: Ensure comprehensive test coverage
4. Refine: Improve based on feedback

```

### Common AI Collaboration Scenarios

#### Starting New Component
```

Template: [Project Context] + [Phase Context] + [Component Requirements]
Focus: Architecture compliance, proper patterns
Validation: Blueprint alignment, coding standards

```

#### Implementing Complex Logic
```

Template: [Domain Context] + [Technical Constraints] + [Specific Requirements]
Focus: Business rules, error handling, performance
Validation: Test coverage, real-time requirements

```

#### Integration Development
```

Template: [Protocol Context] + [External Service Requirements] + [Error Scenarios]
Focus: Fault tolerance, proper abstraction, testing
Validation: Integration tests, error handling

```

#### Refactoring Existing Code
```

Template: [Current Implementation] + [Improvement Goals] + [Constraints]
Focus: Maintaining functionality, improving quality
Validation: Regression tests, performance impact

```

## Template Usage Guidelines

### When to Use Each Template
- **Master Context**: Every AI collaboration session
- **Phase Context**: When working within specific development phase
- **Domain Context**: For domain-specific implementation
- **Code Templates**: For generating specific code artifacts
- **Quality Templates**: For validation and review processes

### Customization Guidelines
- Add specific requirements to base templates
- Include current implementation context
- Reference relevant blueprint documents
- Specify current constraints and goals

### Validation Process
- Verify AI output against templates
- Check architectural compliance
- Validate test coverage and quality
- Ensure documentation completeness

These templates provide a systematic approach to AI collaboration, ensuring consistent quality and architectural integrity throughout SnapDog development.
