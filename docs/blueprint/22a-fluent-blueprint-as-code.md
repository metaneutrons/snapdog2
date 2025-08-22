# 26. a. Fluent Blueprint-as-Code

## 26.1. Overview

This blueprint defines a fluent DSL (Domain Specific Language) approach for maintaining the SnapDog2 system specification as executable code. This eliminates fragmented test configurations and provides a single source of truth for all commands, status, protocols, and implementation requirements.

## 26.2. Problem Statement

The current testing approach suffers from:
- **Fragmented Configuration**: KNX exclusions, HTTP methods, protocol mappings scattered across multiple files
- **DRY Violations**: Same information duplicated in different formats
- **Maintenance Burden**: Changes require editing multiple hard-to-debug files
- **Inconsistency Risk**: Easy to miss updates in one location
- **Poor Readability**: Verbose record syntax obscures the actual specification

## 26.3. Solution: Fluent Blueprint DSL

### 26.3.1. Core Concept

Transform the blueprint specification into a fluent, chainable API that reads like natural language while serving as executable documentation.

```csharp
public static class SnapDogBlueprint 
{
    public static readonly Blueprint Spec = Blueprint.Create()
        
        // Zone Playback Commands
        .Command("PLAY").Zone().RestApi().Mqtt().Post("/zones/{id}/play").Required()
        .Command("PAUSE").Zone().RestApi().Mqtt().Post("/zones/{id}/pause").Required()
        .Command("STOP").Zone().RestApi().Mqtt().Post("/zones/{id}/stop").Required()
        
        // Volume Control
        .Command("VOLUME").Zone().RestApi().Mqtt().Put("/zones/{id}/volume")
            .ExcludeKnx("Handled by dedicated KNX volume actuators").Required()
        .Command("VOLUME_UP").Zone().RestApi().Mqtt().Post("/zones/{id}/volume/up")
            .ExcludeKnx("Handled by dedicated KNX volume actuators").Required()
        .Command("VOLUME_DOWN").Zone().RestApi().Mqtt().Post("/zones/{id}/volume/down")
            .ExcludeKnx("Handled by dedicated KNX volume actuators").Required()
            
        // Mute Control
        .Command("MUTE").Zone().RestApi().Mqtt().Put("/zones/{id}/mute")
            .ExcludeKnx("Handled by dedicated KNX mute actuators").Required()
        .Command("MUTE_TOGGLE").Zone().RestApi().Mqtt().Post("/zones/{id}/mute/toggle")
            .ExcludeKnx("Toggle commands require state synchronization unsuitable for KNX").Required()
            
        // Track Navigation
        .Command("TRACK").Zone().RestApi().Mqtt().Put("/zones/{id}/track")
            .ExcludeKnx("Complex track navigation not suitable for building automation").Required()
        .Command("TRACK_NEXT").Zone().RestApi().Mqtt().Post("/zones/{id}/track/next")
            .ExcludeKnx("Complex track navigation not suitable for building automation").Required()
        .Command("TRACK_PREVIOUS").Zone().RestApi().Mqtt().Post("/zones/{id}/track/previous")
            .ExcludeKnx("Complex track navigation not suitable for building automation").Required()
            
        // Playlist Management
        .Command("PLAYLIST").Zone().RestApi().Mqtt().Put("/zones/{id}/playlist").Required()
        .Command("PLAYLIST_NEXT").Zone().RestApi().Mqtt().Post("/zones/{id}/playlist/next")
            .ExcludeKnx("Complex playlist navigation not suitable for building automation").Required()
        .Command("PLAYLIST_PREVIOUS").Zone().RestApi().Mqtt().Post("/zones/{id}/playlist/previous")
            .ExcludeKnx("Complex playlist navigation not suitable for building automation").Required()
        .Command("PLAYLIST_SHUFFLE").Zone().RestApi().Mqtt().Put("/zones/{id}/playlist/shuffle")
            .ExcludeKnx("Complex state management not suitable for KNX").Required()
        .Command("PLAYLIST_SHUFFLE_TOGGLE").Zone().RestApi().Mqtt().Post("/zones/{id}/playlist/shuffle/toggle")
            .ExcludeKnx("Toggle commands require state synchronization unsuitable for KNX").Required()
            
        // Client Commands
        .Command("CLIENT_VOLUME").Client().RestApi().Mqtt().Put("/clients/{id}/volume")
            .ExcludeKnx("Client-specific network settings not suitable for building automation").Required()
        .Command("CLIENT_VOLUME_UP").Client().RestApi().Mqtt().Post("/clients/{id}/volume/up")
            .ExcludeKnx("Client-specific network settings not suitable for building automation").Required()
        .Command("CLIENT_VOLUME_DOWN").Client().RestApi().Mqtt().Post("/clients/{id}/volume/down")
            .ExcludeKnx("Client-specific network settings not suitable for building automation").Required()
        .Command("CLIENT_MUTE").Client().RestApi().Mqtt().Put("/clients/{id}/mute")
            .ExcludeKnx("Client-specific network settings not suitable for building automation").Required()
        .Command("CLIENT_MUTE_TOGGLE").Client().RestApi().Mqtt().Post("/clients/{id}/mute/toggle")
            .ExcludeKnx("Client-specific network settings not suitable for building automation").Required()
        .Command("CLIENT_LATENCY").Client().RestApi().Mqtt().Put("/clients/{id}/latency")
            .ExcludeKnx("Network-specific setting not suitable for building automation").Required()
        .Command("CLIENT_NAME").Client().RestApi().Mqtt().Put("/clients/{id}/name")
            .ExcludeKnx("Client-specific network settings not suitable for building automation").Required()
        .Command("CLIENT_ZONE").Client().RestApi().Mqtt().Put("/clients/{id}/zone")
            .ExcludeKnx("Client-specific network settings not suitable for building automation").Required()
            
        // Control Commands
        .Command("CONTROL").Zone().RestApi().Mqtt().Post("/zones/{id}/control").Required()
        
        // === STATUS DEFINITIONS ===
        
        // System Status
        .Status("SYSTEM_STATUS").Global().RestApi().Mqtt().Get("/system/status")
            .ExcludeKnx("Read-only system information not actionable via KNX").Required()
        .Status("VERSION_INFO").Global().RestApi().Mqtt().Get("/system/version").Required()
        .Status("SERVER_STATS").Global().RestApi().Mqtt().Get("/system/stats").Required()
        .Status("ZONES_INFO").Global().RestApi().Mqtt().Get("/zones").Required()
        .Status("CLIENTS_INFO").Global().RestApi().Mqtt().Get("/clients")
            .ExcludeKnx("Read-only system information not actionable via KNX").Required()
            
        // Zone Status
        .Status("ZONE_STATE").Zone().RestApi().Mqtt().Get("/zones/{id}").Required()
        .Status("VOLUME_STATUS").Zone().RestApi().Mqtt().Get("/zones/{id}/volume").Required()
        .Status("MUTE_STATUS").Zone().RestApi().Mqtt().Get("/zones/{id}/mute").Required()
        .Status("PLAYBACK_STATE").Zone().RestApi().Mqtt().Get("/zones/{id}/playback").Required()
        
        // Track Status
        .Status("TRACK_STATUS").Zone().RestApi().Mqtt().Get("/zones/{id}/track").Required()
        .Status("TRACK_METADATA").Zone().RestApi().Mqtt().Get("/zones/{id}/track/metadata")
            .ExcludeKnx("Read-only metadata not actionable via KNX").Required()
        .Status("TRACK_POSITION_STATUS").Zone().RestApi().Mqtt().Get("/zones/{id}/track/position").Required()
        .Status("TRACK_PROGRESS_STATUS").Zone().RestApi().Mqtt().Get("/zones/{id}/track/progress").Required()
        .Status("TRACK_REPEAT_STATUS").Zone().RestApi().Mqtt().Get("/zones/{id}/track/repeat").Required()
        
        // Playlist Status
        .Status("PLAYLIST_STATUS").Zone().RestApi().Mqtt().Get("/zones/{id}/playlist").Required()
        .Status("PLAYLIST_INFO").Zone().RestApi().Mqtt().Get("/zones/{id}/playlist/info").Required()
        .Status("PLAYLIST_NAME_STATUS").Zone().RestApi().Mqtt().Get("/zones/{id}/playlist/name").RecentlyAdded().Required()
        .Status("PLAYLIST_COUNT_STATUS").Zone().RestApi().Mqtt().Get("/zones/{id}/playlist/count").RecentlyAdded().Required()
        .Status("PLAYLIST_SHUFFLE_STATUS").Zone().RestApi().Mqtt().Get("/zones/{id}/playlist/shuffle").Required()
        .Status("PLAYLIST_REPEAT_STATUS").Zone().RestApi().Mqtt().Get("/zones/{id}/playlist/repeat").Required()
        
        // Client Status
        .Status("CLIENT_STATE").Client().RestApi().Mqtt().Get("/clients/{id}").Required()
        .Status("CLIENT_VOLUME_STATUS").Client().RestApi().Mqtt().Get("/clients/{id}/volume").Required()
        .Status("CLIENT_MUTE_STATUS").Client().RestApi().Mqtt().Get("/clients/{id}/mute").Required()
        .Status("CLIENT_LATENCY_STATUS").Client().RestApi().Mqtt().Get("/clients/{id}/latency").Required()
        .Status("CLIENT_ZONE_STATUS").Client().RestApi().Mqtt().Get("/clients/{id}/zone").Required()
        .Status("CLIENT_NAME_STATUS").Client().RestApi().Mqtt().Get("/clients/{id}/name").RecentlyAdded().Required()
        .Status("CLIENT_CONNECTED").Client().RestApi().Mqtt().Get("/clients/{id}/connected").Required()
        
        // Protocol-Specific Status
        .Status("CONTROL_STATUS").Zone().MqttOnly().RecentlyAdded().Required() // MQTT-only control notifications
        
        // Command Response Status
        .Status("COMMAND_STATUS").Global().RestApi().Mqtt().Get("/system/commands/status").Required()
        .Status("COMMAND_ERROR").Global().RestApi().Mqtt().Get("/system/commands/errors").Required()
        .Status("SYSTEM_ERROR").Global().RestApi().Mqtt().Get("/system/errors").Required()
        
        .Build();
}
```

## 26.4. DSL API Design

### 26.4.1. Fluent Chain Structure

```csharp
Blueprint.Create()
    .Command(id) / .Status(id)     // Entry point
    .Category()                    // Zone() | Client() | Global() | Media()
    .Protocols()                   // RestApi() | Mqtt() | Knx() | MqttOnly()
    .HttpMethod(path)              // Get() | Post() | Put() | Delete()
    .Modifiers()                   // ExcludeKnx() | RecentlyAdded() | Optional()
    .Required() / .Optional()      // Implementation requirement
```

### 26.4.2. Method Categories

#### 26.4.2.1. Entry Points
- `Command(string id)` - Define a command
- `Status(string id)` - Define a status

#### 26.4.2.2. Categories
- `Zone()` - Zone-related feature
- `Client()` - Client-related feature  
- `Global()` - System-wide feature
- `Media()` - Media-related feature

#### 26.4.2.3. Protocols
- `RestApi()` - Available via REST API
- `Mqtt()` - Available via MQTT
- `Knx()` - Available via KNX (rarely used explicitly)
- `MqttOnly()` - MQTT exclusive (like CONTROL_STATUS)

#### 26.4.2.4. HTTP Methods (for REST API)
- `Get(string path)` - GET endpoint
- `Post(string path)` - POST endpoint  
- `Put(string path)` - PUT endpoint
- `Delete(string path)` - DELETE endpoint

#### 26.4.2.5. Modifiers
- `ExcludeKnx(string reason)` - Exclude from KNX with reasoning
- `RecentlyAdded()` - Mark as recently added (grace period)
- `Notes(string notes)` - Additional documentation

#### 26.4.2.6. Requirements
- `Required()` - Must be implemented
- `Optional()` - Implementation optional

## 26.5. Usage in Tests

### 26.5.1. Automatic Test Generation

```csharp
[Fact]
public void AllCommands_ShouldHaveRequiredImplementations()
{
    var missingImplementations = new List<string>();
    
    foreach (var command in SnapDogBlueprint.Spec.Commands.Required())
    {
        if (command.HasRestApi && !HasRestApiImplementation(command))
            missingImplementations.Add($"{command.Id} missing REST API");
            
        if (command.HasMqtt && !HasMqttImplementation(command))
            missingImplementations.Add($"{command.Id} missing MQTT");
    }
    
    missingImplementations.Should().BeEmpty();
}

[Fact] 
public void RestEndpoints_ShouldUseCorrectHttpMethods()
{
    var incorrectMethods = SnapDogBlueprint.Spec.Commands
        .WithRestApi()
        .Where(c => GetActualHttpMethod(c) != c.HttpMethod)
        .Select(c => $"{c.Id} uses {GetActualHttpMethod(c)} but should use {c.HttpMethod}");
        
    incorrectMethods.Should().BeEmpty();
}

[Fact]
public void KnxExclusions_ShouldBeDocumented()
{
    var undocumentedExclusions = GetActualKnxExclusions()
        .Except(SnapDogBlueprint.Spec.Commands.ExcludedFromKnx().Select(c => c.Id));
        
    undocumentedExclusions.Should().BeEmpty();
}
```

### 26.5.2. Query API

```csharp
// Get all REST API commands
var restCommands = Spec.Commands.WithRestApi();

// Get all MQTT-only status
var mqttOnlyStatus = Spec.Status.MqttOnly();

// Get all KNX exclusions with reasons
var knxExclusions = Spec.Commands.ExcludedFromKnx()
    .ToDictionary(c => c.Id, c => c.KnxExclusionReason);

// Get all POST endpoints
var postEndpoints = Spec.Commands.WithHttpMethod("POST");

// Get recently added features
var recentFeatures = Spec.All.RecentlyAdded();
```

## 26.6. Implementation Architecture

### 26.6.1. Core Classes

```csharp
public class Blueprint
{
    public CommandCollection Commands { get; }
    public StatusCollection Status { get; }
    public FeatureCollection All => Commands.Concat(Status);
    
    public static BlueprintBuilder Create() => new();
}

public class BlueprintBuilder
{
    public CommandBuilder Command(string id) => new(this, id);
    public StatusBuilder Status(string id) => new(this, id);
    public Blueprint Build() => new(commands, status);
}

public class CommandBuilder : FeatureBuilder<CommandBuilder>
{
    public CommandBuilder Get(string path) => HttpMethod("GET", path);
    public CommandBuilder Post(string path) => HttpMethod("POST", path);
    public CommandBuilder Put(string path) => HttpMethod("PUT", path);
    // ... etc
}
```

### 26.6.2. Collections with Query Methods

```csharp
public class CommandCollection : IEnumerable<CommandSpec>
{
    public CommandCollection WithRestApi() => Filter(c => c.HasRestApi);
    public CommandCollection WithMqtt() => Filter(c => c.HasMqtt);
    public CommandCollection ExcludedFromKnx() => Filter(c => c.IsExcludedFromKnx);
    public CommandCollection Required() => Filter(c => c.IsRequired);
    public CommandCollection RecentlyAdded() => Filter(c => c.IsRecentlyAdded);
    public CommandCollection WithHttpMethod(string method) => Filter(c => c.HttpMethod == method);
    // ... etc
}
```

## 26.7. Benefits

### 26.7.1. Single Source of Truth
- All specification in one fluent chain
- No duplication across multiple files
- Changes propagate automatically to all tests

### 26.7.2. DRY Principle
- Each piece of information defined exactly once
- Method chaining builds complete specification
- No manual synchronization required

### 26.7.3. Maintainability
- Add new feature: single fluent chain entry
- Modify existing: change one method call
- Remove feature: delete one line

### 26.7.4. Readability
- Reads like natural language documentation
- Self-documenting through method names
- Clear intent and relationships

### 26.7.5. Type Safety
- Fluent API prevents invalid combinations
- Compile-time validation of specification
- IntelliSense guides correct usage

### 26.7.6. Test Simplification
- Tests become simple queries against specification
- Automatic test generation from blueprint
- No hard-coded test data

## 26.8. Migration Strategy

1. **Create Blueprint DSL** - Implement fluent API classes
2. **Define Specification** - Convert current scattered config to fluent chain
3. **Update Tests** - Replace hard-coded data with blueprint queries
4. **Validate** - Ensure all existing tests pass with new approach
5. **Clean Up** - Remove old fragmented configuration files

## 26.9. Future Extensions

- **Code Generation** - Generate API controllers from blueprint
- **Documentation** - Auto-generate API docs from specification  
- **Validation** - Runtime validation against blueprint
- **Metrics** - Track implementation coverage
- **Versioning** - Blueprint evolution tracking

This approach transforms the blueprint from scattered configuration into executable, maintainable, and self-documenting code that serves as the single source of truth for the entire system specification.
