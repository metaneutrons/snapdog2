# 19. Integration and Implementation Summary

This document provides integration guidance and summarizes the complete Cortex.Mediator command framework implementation.

## 19.1. Infrastructure Adapter Integration

### 19.1.1. MQTT Service Integration

The MQTT service maps incoming topics to Cortex.Mediator commands and publishes status notifications as MQTT messages.

```csharp
// /Infrastructure/Mqtt/MqttCommandMapper.cs
namespace SnapDog2.Infrastructure.Mqtt;

using System;
using System.Threading.Tasks;
using Cortex.Mediator;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Enums;
using SnapDog2.Server.Features.Zones.Commands;
using SnapDog2.Server.Features.Clients.Commands;

/// <summary>
/// Maps MQTT topics and payloads to Cortex.Mediator commands.
/// </summary>
public partial class MqttCommandMapper
{
    private readonly IMediator _mediator;
    private readonly ILogger<MqttCommandMapper> _logger;

    [LoggerMessage(7001, LogLevel.Information, "Mapping MQTT command: {Topic} -> {Payload}")]
    private partial void LogMappingCommand(string topic, string payload);

    [LoggerMessage(7002, LogLevel.Warning, "Unknown MQTT topic: {Topic}")]
    private partial void LogUnknownTopic(string topic);

    public MqttCommandMapper(IMediator mediator, ILogger<MqttCommandMapper> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Maps an MQTT message to a command and sends it via the mediator.
    /// </summary>
    public async Task<bool> MapAndSendCommandAsync(string topic, string payload)
    {
        LogMappingCommand(topic, payload);

        try
        {
            var command = MapTopicToCommand(topic, payload);
            if (command != null)
            {
                await _mediator.Send(command).ConfigureAwait(false);
                return true;
            }

            LogUnknownTopic(topic);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error mapping MQTT command for topic {Topic}", topic);
            return false;
        }
    }

    private object? MapTopicToCommand(string topic, string payload)
    {
        // Parse topic structure: snapdog/zones/{zoneId}/volume/set
        var parts = topic.Split('/');

        if (parts.Length >= 4 && parts[0] == "snapdog")
        {
            if (parts[1] == "zones" && int.TryParse(parts[2], out var zoneId))
            {
                return MapZoneCommand(zoneId, parts[3..], payload);
            }
            else if (parts[1] == "clients" && int.TryParse(parts[2], out var clientId))
            {
                return MapClientCommand(clientId, parts[3..], payload);
            }
        }

        return null;
    }

    private object? MapZoneCommand(int zoneId, string[] commandParts, string payload)
    {
        return commandParts switch
        {
            ["volume", "set"] => new SetZoneVolumeCommand
            {
                ZoneId = zoneId,
                Volume = int.Parse(payload),
                Source = CommandSource.Mqtt
            },

            ["mute", "set"] => payload.ToLowerInvariant() switch
            {
                "toggle" => new ToggleZoneMuteCommand { ZoneId = zoneId, Source = CommandSource.Mqtt },
                _ => new SetZoneMuteCommand
                {
                    ZoneId = zoneId,
                    Enabled = ParseBooleanPayload(payload),
                    Source = CommandSource.Mqtt
                }
            },

            ["control", "set"] => MapControlCommand(zoneId, payload),

            ["track", "set"] => payload switch
            {
                "+" => new NextTrackCommand { ZoneId = zoneId, Source = CommandSource.Mqtt },
                "-" => new PreviousTrackCommand { ZoneId = zoneId, Source = CommandSource.Mqtt },
                _ when int.TryParse(payload, out var trackIndex) =>
                    new SetTrackCommand { ZoneId = zoneId, TrackIndex = trackIndex, Source = CommandSource.Mqtt },
                _ => null
            },

            ["playlist", "set"] => payload switch
            {
                "+" => new NextPlaylistCommand { ZoneId = zoneId, Source = CommandSource.Mqtt },
                "-" => new PreviousPlaylistCommand { ZoneId = zoneId, Source = CommandSource.Mqtt },
                _ when int.TryParse(payload, out var playlistIndex) =>
                    new SetPlaylistCommand { ZoneId = zoneId, PlaylistIndex = playlistIndex, Source = CommandSource.Mqtt },
                _ => new SetPlaylistCommand { ZoneId = zoneId, PlaylistId = payload, Source = CommandSource.Mqtt }
            },

            _ => null
        };
    }

    private object? MapControlCommand(int zoneId, string payload)
    {
        return payload.ToLowerInvariant() switch
        {
            "play" => new PlayCommand { ZoneId = zoneId, Source = CommandSource.Mqtt },
            "pause" => new PauseCommand { ZoneId = zoneId, Source = CommandSource.Mqtt },
            "stop" => new StopCommand { ZoneId = zoneId, Source = CommandSource.Mqtt },
            "next" or "track_next" => new NextTrackCommand { ZoneId = zoneId, Source = CommandSource.Mqtt },
            "previous" or "track_previous" => new PreviousTrackCommand { ZoneId = zoneId, Source = CommandSource.Mqtt },
            "mute_toggle" => new ToggleZoneMuteCommand { ZoneId = zoneId, Source = CommandSource.Mqtt },
            "shuffle_toggle" => new TogglePlaylistShuffleCommand { ZoneId = zoneId, Source = CommandSource.Mqtt },
            _ when payload.StartsWith("volume ") => ParseVolumeCommand(zoneId, payload),
            _ when payload.StartsWith("track ") => ParseTrackCommand(zoneId, payload),
            _ => null
        };
    }

    private object? MapClientCommand(int clientId, string[] commandParts, string payload)
    {
        return commandParts switch
        {
            ["volume", "set"] => new SetClientVolumeCommand
            {
                ClientId = clientId,
                Volume = int.Parse(payload),
                Source = CommandSource.Mqtt
            },

            ["mute", "set"] => payload.ToLowerInvariant() switch
            {
                "toggle" => new ToggleClientMuteCommand { ClientId = clientId, Source = CommandSource.Mqtt },
                _ => new SetClientMuteCommand
                {
                    ClientId = clientId,
                    Enabled = ParseBooleanPayload(payload),
                    Source = CommandSource.Mqtt
                }
            },

            ["zone", "set"] => new AssignClientToZoneCommand
            {
                ClientId = clientId,
                ZoneId = int.Parse(payload),
                Source = CommandSource.Mqtt
            },

            ["latency", "set"] => new SetClientLatencyCommand
            {
                ClientId = clientId,
                LatencyMs = int.Parse(payload),
                Source = CommandSource.Mqtt
            },

            _ => null
        };
    }

    private static bool ParseBooleanPayload(string payload)
    {
        return payload.ToLowerInvariant() switch
        {
            "true" or "1" or "on" => true,
            "false" or "0" or "off" => false,
            _ => throw new ArgumentException($"Invalid boolean payload: {payload}")
        };
    }

    // Additional helper methods...
}
```

### 19.1.2. KNX Service Integration

```csharp
// /Infrastructure/Knx/KnxCommandMapper.cs
namespace SnapDog2.Infrastructure.Knx;

using System;
using System.Threading.Tasks;
using Cortex.Mediator;
using Knx.Falcon;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Enums;
using SnapDog2.Server.Features.Zones.Commands;

/// <summary>
/// Maps KNX group addresses and values to Cortex.Mediator commands.
/// </summary>
public partial class KnxCommandMapper
{
    private readonly IMediator _mediator;
    private readonly IKnxGroupAddressConfig _config;
    private readonly ILogger<KnxCommandMapper> _logger;

    [LoggerMessage(8001, LogLevel.Information, "Mapping KNX command: {GroupAddress} -> {Value}")]
    private partial void LogMappingCommand(GroupAddress groupAddress, object value);

    public KnxCommandMapper(
        IMediator mediator,
        IKnxGroupAddressConfig config,
        ILogger<KnxCommandMapper> logger)
    {
        _mediator = mediator;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Maps a KNX group value to a command and sends it via the mediator.
    /// </summary>
    public async Task<bool> MapAndSendCommandAsync(GroupAddress groupAddress, object value)
    {
        LogMappingCommand(groupAddress, value);

        try
        {
            var command = MapGroupAddressToCommand(groupAddress, value);
            if (command != null)
            {
                await _mediator.Send(command).ConfigureAwait(false);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error mapping KNX command for GA {GroupAddress}", groupAddress);
            return false;
        }
    }

    private object? MapGroupAddressToCommand(GroupAddress groupAddress, object value)
    {
        // Look up the command mapping for this group address
        var mapping = _config.GetCommandMapping(groupAddress);
        if (mapping == null) return null;

        return mapping.CommandType switch
        {
            "VOLUME" => new SetZoneVolumeCommand
            {
                ZoneId = mapping.ZoneId,
                Volume = ConvertDpt5ToPercentage((byte)value),
                Source = CommandSource.Knx
            },

            "MUTE" => new SetZoneMuteCommand
            {
                ZoneId = mapping.ZoneId,
                Enabled = (bool)value,
                Source = CommandSource.Knx
            },

            "PLAY_PAUSE" => (bool)value
                ? new PlayCommand { ZoneId = mapping.ZoneId, Source = CommandSource.Knx }
                : new PauseCommand { ZoneId = mapping.ZoneId, Source = CommandSource.Knx },

            "TRACK" => new SetTrackCommand
            {
                ZoneId = mapping.ZoneId,
                TrackIndex = (byte)value, // 1-based from KNX
                Source = CommandSource.Knx
            },

            _ => null
        };
    }

    private static int ConvertDpt5ToPercentage(byte dptValue)
    {
        // DPT 5.001: 0-255 -> 0-100%
        return (int)Math.Round(dptValue / 255.0 * 100.0);
    }
}
```

### 19.1.3. API Controller Integration

```csharp
// /Api/Controllers/ZonesController.cs
namespace SnapDog2.Api.Controllers;

using System.Collections.Generic;
using System.Threading.Tasks;
using Cortex.Mediator;
using Microsoft.AspNetCore.Mvc;
using SnapDog2.Api.Models;
using SnapDog2.Core.Enums;
using SnapDog2.Core.Models;
using SnapDog2.Server.Features.Zones.Commands;
using SnapDog2.Server.Features.Zones.Queries;

/// <summary>
/// API controller for zone management.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ZonesController : ControllerBase
{
    private readonly IMediator _mediator;

    public ZonesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Gets all zones.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ZoneState>>>> GetAllZones()
    {
        var query = new GetAllZonesQuery();
        var result = await _mediator.Send(query);

        return result.IsSuccess
            ? Ok(ApiResponse<List<ZoneState>>.Success(result.Value))
            : BadRequest(ApiResponse<List<ZoneState>>.Error(result.ErrorMessage));
    }

    /// <summary>
    /// Gets a specific zone.
    /// </summary>
    [HttpGet("{zoneId:int}")]
    public async Task<ActionResult<ApiResponse<ZoneState>>> GetZone(int zoneId)
    {
        var query = new GetZoneStateQuery { ZoneId = zoneId };
        var result = await _mediator.Send(query);

        return result.IsSuccess
            ? Ok(ApiResponse<ZoneState>.Success(result.Value))
            : NotFound(ApiResponse<ZoneState>.Error(result.ErrorMessage));
    }

    /// <summary>
    /// Sets the volume for a zone.
    /// </summary>
    [HttpPost("{zoneId:int}/volume")]
    public async Task<ActionResult<ApiResponse<object>>> SetVolume(int zoneId, [FromBody] SetVolumeRequest request)
    {
        var command = new SetZoneVolumeCommand
        {
            ZoneId = zoneId,
            Volume = request.Volume,
            Source = CommandSource.Api
        };

        var result = await _mediator.Send(command);

        return result.IsSuccess
            ? Ok(ApiResponse<object>.Success(null))
            : BadRequest(ApiResponse<object>.Error(result.ErrorMessage));
    }

    /// <summary>
    /// Starts playback in a zone.
    /// </summary>
    [HttpPost("{zoneId:int}/play")]
    public async Task<ActionResult<ApiResponse<object>>> Play(int zoneId, [FromBody] PlayRequest? request = null)
    {
        var command = new PlayCommand
        {
            ZoneId = zoneId,
            TrackIndex = request?.TrackIndex,
            MediaUrl = request?.MediaUrl,
            Source = CommandSource.Api
        };

        var result = await _mediator.Send(command);

        return result.IsSuccess
            ? Ok(ApiResponse<object>.Success(null))
            : BadRequest(ApiResponse<object>.Error(result.ErrorMessage));
    }

    /// <summary>
    /// Pauses playback in a zone.
    /// </summary>
    [HttpPost("{zoneId:int}/pause")]
    public async Task<ActionResult<ApiResponse<object>>> Pause(int zoneId)
    {
        var command = new PauseCommand { ZoneId = zoneId, Source = CommandSource.Api };
        var result = await _mediator.Send(command);

        return result.IsSuccess
            ? Ok(ApiResponse<object>.Success(null))
            : BadRequest(ApiResponse<object>.Error(result.ErrorMessage));
    }
}
```

## 19.2. Dependency Injection Configuration

```csharp
// /Worker/DI/CortexMediatorConfiguration.cs
namespace SnapDog2.Worker.DI;

using System.Reflection;
using Cortex.Mediator;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SnapDog2.Server.Behaviors;

/// <summary>
/// Extension methods for configuring Cortex.Mediator services.
/// </summary>
public static class CortexMediatorConfiguration
{
    /// <summary>
    /// Adds Cortex.Mediator and related services to the service collection.
    /// </summary>
    public static IServiceCollection AddCommandProcessing(this IServiceCollection services)
    {
        var serverAssembly = typeof(LoggingBehavior<,>).Assembly;

        services.AddCortexMediator(cfg =>
        {
            // Register all handlers from the server assembly
            cfg.RegisterServicesFromAssembly(serverAssembly);

            // Register pipeline behaviors in order
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        });

        // Register FluentValidation validators
        services.AddValidatorsFromAssembly(serverAssembly, ServiceLifetime.Transient);

        return services;
    }
}
```

## 19.3. Testing Strategy

### 19.3.1. Unit Tests for Commands and Handlers

```csharp
// /Tests/Unit/Server/Features/Zones/Handlers/SetZoneVolumeCommandHandlerTests.cs
namespace SnapDog2.Tests.Unit.Server.Features.Zones.Handlers;

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Models;
using SnapDog2.Server.Features.Zones.Commands;
using SnapDog2.Server.Features.Zones.Handlers;
using Xunit;

public class SetZoneVolumeCommandHandlerTests
{
    private readonly Mock<IZoneManager> _zoneManagerMock;
    private readonly Mock<ILogger<SetZoneVolumeCommandHandler>> _loggerMock;
    private readonly SetZoneVolumeCommandHandler _handler;

    public SetZoneVolumeCommandHandlerTests()
    {
        _zoneManagerMock = new Mock<IZoneManager>();
        _loggerMock = new Mock<ILogger<SetZoneVolumeCommandHandler>>();
        _handler = new SetZoneVolumeCommandHandler(_zoneManagerMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccess()
    {
        // Arrange
        var command = new SetZoneVolumeCommand { ZoneId = 1, Volume = 75 };
        var mockZone = new Mock<IZoneService>();

        _zoneManagerMock
            .Setup(x => x.GetZoneAsync(1))
            .ReturnsAsync(Result<IZoneService>.Success(mockZone.Object));

        mockZone
            .Setup(x => x.SetVolumeAsync(75))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        _zoneManagerMock.Verify(x => x.GetZoneAsync(1), Times.Once);
        mockZone.Verify(x => x.SetVolumeAsync(75), Times.Once);
    }

    [Fact]
    public async Task Handle_ZoneNotFound_ReturnsFailure()
    {
        // Arrange
        var command = new SetZoneVolumeCommand { ZoneId = 999, Volume = 75 };

        _zoneManagerMock
            .Setup(x => x.GetZoneAsync(999))
            .ReturnsAsync(Result<IZoneService>.Failure("Zone not found"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Zone not found", result.ErrorMessage);
    }
}
```

### 19.3.2. Integration Tests

```csharp
// /Tests/Integration/Server/Features/ZoneCommandIntegrationTests.cs
namespace SnapDog2.Tests.Integration.Server.Features;

using System.Threading.Tasks;
using Cortex.Mediator;
using Microsoft.Extensions.DependencyInjection;
using SnapDog2.Core.Enums;
using SnapDog2.Server.Features.Zones.Commands;
using SnapDog2.Tests.Integration.Fixtures;
using Xunit;

public class ZoneCommandIntegrationTests : IClassFixture<TestServerFixture>
{
    private readonly TestServerFixture _fixture;
    private readonly IMediator _mediator;

    public ZoneCommandIntegrationTests(TestServerFixture fixture)
    {
        _fixture = fixture;
        _mediator = _fixture.Services.GetRequiredService<IMediator>();
    }

    [Fact]
    public async Task SetZoneVolumeCommand_ValidZone_UpdatesVolume()
    {
        // Arrange
        var command = new SetZoneVolumeCommand
        {
            ZoneId = 1,
            Volume = 80,
            Source = CommandSource.Api
        };

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsSuccess);

        // Verify the volume was actually set
        var zoneState = await _fixture.GetZoneStateAsync(1);
        Assert.Equal(80, zoneState.Volume);
    }
}
```

## 19.4. Overview

The complete Cortex.Mediator command framework implementation includes:

1. **Commands (40+ classes)**:
   - Zone commands: Play, Pause, Stop, Volume, Mute, Track, Playlist controls
   - Client commands: Volume, Mute, Latency, Zone assignment
   - Global commands: System control and status

2. **Queries (15+ classes)**:
   - Zone state queries: Individual and bulk zone information
   - Client state queries: Individual and bulk client information
   - Playlist and track information queries

3. **Notifications (25+ classes)**:
   - Specific notifications: Volume changed, track changed, etc.
   - Generic status notifications for infrastructure adapters

4. **Handlers (55+ classes)**:
   - Command handlers for all business logic
   - Query handlers for data retrieval
   - Notification handlers for cross-cutting concerns

5. **Validators (20+ classes)**:
   - FluentValidation validators for all commands
   - Input validation and business rule enforcement

### 19.4.1. Key Benefits

1. **Type Safety**: Strong typing throughout the command pipeline
2. **Separation of Concerns**: Clear separation between commands, queries, and notifications
3. **Testability**: Easy to unit test individual handlers and integration test the pipeline
4. **Extensibility**: Easy to add new commands, queries, and handlers
5. **Protocol Agnostic**: Same commands work for API, MQTT, KNX, and other protocols
6. **Validation**: Automatic validation of all commands before execution
7. **Observability**: Built-in logging, metrics, and tracing support

### 19.4.2. Implementation Plan

1. **Implementation**: Start implementing the Core abstractions (IZoneManager, IClientManager)
2. **Infrastructure**: Build the concrete infrastructure services (SnapcastService, MqttService, KnxService)
3. **Testing**: Create comprehensive test suites for all components
4. **Configuration**: Implement the configuration system for MQTT topics and KNX group addresses
5. **Documentation**: Create API documentation and usage examples

This implementation provides a solid foundation for the entire SnapDog2 application, with clear patterns and strong architectural boundaries that will support future development and maintenance.
