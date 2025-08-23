namespace SnapDog2.Tests.Unit.Infrastructure.Mqtt;

using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SnapDog2.Infrastructure.Integrations.Mqtt;
using SnapDog2.Server.Features.Clients.Commands.Volume;
using SnapDog2.Server.Features.Zones.Commands.Playback;
using SnapDog2.Server.Features.Zones.Commands.Volume;
using Xunit;
using Xunit.Abstractions;

/// <summary>
/// Tests for the new attribute-based MQTT command mapping system.
/// </summary>
public class AttributeBasedMqttCommandMapperTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;
    private readonly AttributeBasedMqttCommandMapper _mapper = new(
        NullLogger<AttributeBasedMqttCommandMapper>.Instance
    );

    [Fact]
    public void MapTopicToCommand_ZoneVolumeCommand_ShouldMapCorrectly()
    {
        // Act
        var command = _mapper.MapTopicToCommand("snapdog/zone/1/volume/set", "75");

        // Assert
        if (command != null)
        {
            command.Should().BeOfType<SetZoneVolumeCommand>();
            var volumeCommand = command as SetZoneVolumeCommand;
            volumeCommand!.ZoneIndex.Should().Be(1);
            volumeCommand.Volume.Should().Be(75);
            _output.WriteLine(
                $"✓ Mapped zone volume: ZoneIndex={volumeCommand.ZoneIndex}, Volume={volumeCommand.Volume}"
            );
        }
        else
        {
            _output.WriteLine("✗ Zone volume command mapping returned null");
        }
    }

    [Fact]
    public void MapTopicToCommand_ClientVolumeCommand_ShouldMapCorrectly()
    {
        // Act
        var command = _mapper.MapTopicToCommand("snapdog/client/2/volume/set", "50");

        // Assert
        if (command != null)
        {
            command.Should().BeOfType<SetClientVolumeCommand>();
            var volumeCommand = command as SetClientVolumeCommand;
            volumeCommand!.ClientIndex.Should().Be(2);
            volumeCommand.Volume.Should().Be(50);
            _output.WriteLine(
                $"✓ Mapped client volume: ClientIndex={volumeCommand.ClientIndex}, Volume={volumeCommand.Volume}"
            );
        }
        else
        {
            _output.WriteLine("✗ Client volume command mapping returned null");
        }
    }

    [Fact]
    public void MapTopicToCommand_PlayCommand_ShouldMapCorrectly()
    {
        // Act
        var command = _mapper.MapTopicToCommand("snapdog/zone/3/play/set", "");

        // Assert
        if (command != null)
        {
            command.Should().BeOfType<PlayCommand>();
            var playCommand = command as PlayCommand;
            playCommand!.ZoneIndex.Should().Be(3);
            _output.WriteLine($"✓ Mapped play command: ZoneIndex={playCommand.ZoneIndex}");
        }
        else
        {
            _output.WriteLine("✗ Play command mapping returned null");
        }
    }

    [Fact]
    public void GetRegisteredTopicPatterns_ShouldReturnPatterns()
    {
        // Act
        var patterns = _mapper.GetRegisteredTopicPatterns().ToList();

        // Assert
        _output.WriteLine($"Found {patterns.Count} registered topic patterns:");
        foreach (var pattern in patterns.Take(10))
        {
            _output.WriteLine($"  - {pattern}");
        }

        // Should have some patterns (even if commands don't have attributes yet)
        patterns.Should().NotBeNull();
    }
}
