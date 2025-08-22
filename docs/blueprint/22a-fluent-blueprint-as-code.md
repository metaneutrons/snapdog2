# 26. a. Fluent Blueprint-as-Code

## 26.1. Overview

A fluent DSL approach for maintaining the SnapDog2 system specification as executable code, providing a single source of truth for all commands, status, protocols, and implementation requirements.

## 26.2. Problem

Current testing suffers from:

- **Fragmented Configuration**: KNX exclusions, HTTP methods, protocol mappings scattered across multiple files
- **DRY Violations**: Same information duplicated in different formats
- **Maintenance Burden**: Changes require editing multiple hard-to-debug files
- **Poor Readability**: Verbose record syntax obscures the specification

## 26.3. Solution: Fluent Blueprint DSL

### 26.3.1. Core Example

```csharp
public static class SnapDogBlueprint
{
    public static readonly Blueprint Spec = Blueprint.Define()

        // Zone Commands - API + MQTT (KNX not mentioned = not included)
        .Command("PLAY")
            .Zone().Api().Mqtt().Post("/zones/{id}/play")
            .Description("Start playback in a zone")

        .Command("VOLUME")
            .Zone().Api().Mqtt().Put("/zones/{id}/volume")
            .Description("Set zone volume level")
            .Exclude(Protocol.Knx, "Handled by dedicated KNX actuators")

        // Client Commands - explicitly excluded from KNX for clarity
        .Command("CLIENT_LATENCY")
            .Client().Api().Mqtt().Put("/clients/{id}/latency")
            .Description("Set client audio latency")
            .Exclude(Protocol.Knx, "Network-specific setting")

        // Global Status - API + MQTT
        .Status("SYSTEM_STATUS")
            .Global().Api().Mqtt().Get("/system/status")
            .Description("Overall system health")

        // MQTT-only Status - just specify MQTT
        .Status("CONTROL_STATUS")
            .Zone().Mqtt()
            .Description("Control command execution status")
            .RecentlyAdded()

        // KNX-enabled Commands - explicitly include all protocols
        .Command("STOP")
            .Zone().Api().Mqtt().Knx()
            .Description("Basic stop command suitable for building automation")

        // Optional implementation
        .Command("ADVANCED_EQ")
            .Zone().Api().Mqtt()
            .Description("Advanced equalizer settings")
            .Optional()

        .Build();
}
```

## 26.4. DSL API

### 26.4.1. Fluent Chain Structure

```
Blueprint.Define()
    .Command(id) / .Status(id)     // Entry point
    .Category()                    // Zone() | Client() | Global()
    .Protocols()                   // Api() | Mqtt() | Knx()
    .HttpMethod(path)              // Get() | Post() | Put() | Delete()
    .Documentation()               // Description()
    .Modifiers()                   // Exclude() | RecentlyAdded() | Optional()
```

### 26.4.2. Methods

**Entry Points**

- `Command(id)` - Define command
- `Status(id)` - Define status

**Categories**

- `Zone()` - Zone features
- `Client()` - Client features
- `Global()` - System features

**Protocols** (combinable)

- `Api()` - REST API available
- `Mqtt()` - MQTT available
- `Knx()` - KNX available

**HTTP Methods**

- `Get(path)` - Query endpoint
- `Post(path)` - Action endpoint
- `Put(path)` - Update endpoint

**Documentation**

- `Description(text)` - Feature description

**Modifiers**

- `Exclude(protocol, reason)` - Explicitly exclude protocol with reason
- `RecentlyAdded()` - Grace period feature
- `Optional()` - Implementation optional (default: required)

## 26.5. Design Principles

### 26.5.1. Explicit Inclusion

- Only specify protocols that ARE supported
- No protocol mentioned = not supported
- Clear and unambiguous

### 26.5.2. Explicit Exclusion for Clarity

- Use `Exclude(Protocol.Knx, reason)` when you want to be explicit about why something is excluded
- Helpful for documentation and understanding design decisions

### 26.5.3. Required by Default

- All features required unless marked `Optional()`
- Simpler and cleaner than marking everything `Required()`

## 26.6. Usage in Tests

### 26.6.1. Automatic Validation

```csharp
[Fact]
public void AllCommands_ShouldHaveRequiredImplementations()
{
    var missing = SnapDogBlueprint.Spec.Commands
        .Required()  // Gets all non-Optional commands
        .Where(c => !IsImplemented(c))
        .Select(c => c.Id);

    missing.Should().BeEmpty();
}

[Fact]
public void RestEndpoints_ShouldUseCorrectHttpMethods()
{
    var incorrect = SnapDogBlueprint.Spec.Commands
        .WithApi()
        .Where(c => GetActualMethod(c) != c.HttpMethod);

    incorrect.Should().BeEmpty();
}

[Fact]
public void KnxExclusions_ShouldBeDocumented()
{
    var undocumented = GetActualKnxExclusions()
        .Except(SnapDogBlueprint.Spec.ExcludedFrom(Protocol.Knx));

    undocumented.Should().BeEmpty();
}
```

### 26.6.2. Query API

```csharp
// Get all API commands
var apiCommands = Spec.Commands.WithApi();

// Get MQTT-only status (has MQTT but not API)
var mqttOnly = Spec.Status.WithMqtt().WithoutApi();

// Get KNX exclusions with reasons
var exclusions = Spec.ExcludedFrom(Protocol.Knx)
    .ToDictionary(f => f.Id, f => f.ExclusionReason);

// Get POST endpoints
var postEndpoints = Spec.Commands.WithMethod("POST");

// Get optional features
var optional = Spec.All.Optional();

// Get recent features
var recent = Spec.All.RecentlyAdded();
```

## 26.7. Benefits

## 26.8. Benefits

- Single Source of Truth
  - All specification in one fluent chain
  - No duplication across multiple files
  - Changes propagate automatically to all tests
- DRY Principle
  - Each piece of information defined exactly once
  - Method chaining builds complete specification
  - No manual synchronization required
- Maintainability
  - Add new feature: single fluent chain entry
  - Modify existing: change one method call
  - Remove feature: delete one line
- Readability
  - Reads like natural language documentation
  - Self-documenting through method names
  - Clear intent and relationships
- Type Safety
  - Fluent API prevents invalid combinations
  - Compile-time validation of specification
  - IntelliSense guides correct usage
- Test Simplification
  - Tests become simple queries against specification
  - Automatic test generation from blueprint
  - No hard-coded test data

## 26.9. Implementation Strategy

1. **Create DSL Classes** - Implement fluent API
2. **Define Specification** - Convert scattered config to fluent chain
3. **Update Tests** - Replace hard-coded data with blueprint queries
4. **Validate** - Ensure existing tests pass
5. **Clean Up** - Remove fragmented files

## 26.10. Future Extensions

- **Code Generation** - Generate controllers from blueprint
- **Documentation** - Auto-generate API docs
- **Runtime Validation** - Validate against blueprint
- **Coverage Metrics** - Track implementation progress
