namespace SnapDog2.Tests.Unit.Core.Factories;

using FluentAssertions;
using SnapDog2.Core.Enums;
using SnapDog2.Server.Features.Clients.Commands.Volume;
using SnapDog2.Server.Features.Shared.Factories;
using SnapDog2.Server.Features.Zones.Commands.Track;
using Xunit;

/// <summary>
/// Unit tests for CommandFactory methods.
/// Tests the new factory methods added for complete MQTT/API alignment.
/// </summary>
public class CommandFactoryTests
{
    #region Track Seek Commands

    [Fact]
    public void CreateSeekPositionCommand_Should_CreateValidCommand()
    {
        // Arrange
        const int zoneIndex = 1;
        const long positionMs = 30000; // 30 seconds
        const CommandSource source = CommandSource.Api;

        // Act
        var command = CommandFactory.CreateSeekPositionCommand(zoneIndex, positionMs, source);

        // Assert
        command.Should().NotBeNull();
        command.ZoneIndex.Should().Be(zoneIndex);
        command.PositionMs.Should().Be(positionMs);
        command.Source.Should().Be(source);
    }

    [Fact]
    public void CreateSeekProgressCommand_Should_CreateValidCommand()
    {
        // Arrange
        const int zoneIndex = 2;
        const float progress = 0.75f; // 75%
        const CommandSource source = CommandSource.Mqtt;

        // Act
        var command = CommandFactory.CreateSeekProgressCommand(zoneIndex, progress, source);

        // Assert
        command.Should().NotBeNull();
        command.ZoneIndex.Should().Be(zoneIndex);
        command.Progress.Should().Be(progress);
        command.Source.Should().Be(source);
    }

    [Fact]
    public void CreatePlayTrackByIndexCommand_Should_CreateValidCommand()
    {
        // Arrange
        const int zoneIndex = 3;
        const int trackIndex = 5;
        const CommandSource source = CommandSource.Knx;

        // Act
        var command = CommandFactory.CreatePlayTrackByIndexCommand(zoneIndex, trackIndex, source);

        // Assert
        command.Should().NotBeNull();
        command.ZoneIndex.Should().Be(zoneIndex);
        command.TrackIndex.Should().Be(trackIndex);
        command.Source.Should().Be(source);
    }

    #endregion

    #region Client Volume Commands

    [Fact]
    public void CreateClientVolumeUpCommand_Should_CreateValidCommand()
    {
        // Arrange
        const int clientIndex = 1;
        const int step = 10;
        const CommandSource source = CommandSource.Api;

        // Act
        var command = CommandFactory.CreateClientVolumeUpCommand(clientIndex, step, source);

        // Assert
        command.Should().NotBeNull();
        command.ClientIndex.Should().Be(clientIndex);
        command.Step.Should().Be(step);
        command.Source.Should().Be(source);
    }

    [Fact]
    public void CreateClientVolumeUpCommand_Should_UseDefaultStep()
    {
        // Arrange
        const int clientIndex = 2;
        const CommandSource source = CommandSource.Internal;

        // Act
        var command = CommandFactory.CreateClientVolumeUpCommand(clientIndex, source: source);

        // Assert
        command.Should().NotBeNull();
        command.ClientIndex.Should().Be(clientIndex);
        command.Step.Should().Be(5); // Default step
        command.Source.Should().Be(source);
    }

    [Fact]
    public void CreateClientVolumeDownCommand_Should_CreateValidCommand()
    {
        // Arrange
        const int clientIndex = 3;
        const int step = 15;
        const CommandSource source = CommandSource.Mqtt;

        // Act
        var command = CommandFactory.CreateClientVolumeDownCommand(clientIndex, step, source);

        // Assert
        command.Should().NotBeNull();
        command.ClientIndex.Should().Be(clientIndex);
        command.Step.Should().Be(step);
        command.Source.Should().Be(source);
    }

    [Fact]
    public void CreateClientVolumeDownCommand_Should_UseDefaultStep()
    {
        // Arrange
        const int clientIndex = 4;
        const CommandSource source = CommandSource.Knx;

        // Act
        var command = CommandFactory.CreateClientVolumeDownCommand(clientIndex, source: source);

        // Assert
        command.Should().NotBeNull();
        command.ClientIndex.Should().Be(clientIndex);
        command.Step.Should().Be(5); // Default step
        command.Source.Should().Be(source);
    }

    #endregion

    #region MQTT Topic Parsing Tests

    [Theory]
    [InlineData("track/position/set", "30000", typeof(SeekPositionCommand))]
    [InlineData("track/progress/set", "0.75", typeof(SeekProgressCommand))]
    [InlineData("play/track", "5", typeof(PlayTrackByIndexCommand))]
    [InlineData("play/url", "http://example.com/stream.mp3", typeof(PlayUrlCommand))]
    public void CreateZoneCommandFromPayload_Should_CreateCorrectTrackCommands(
        string command,
        string payload,
        Type expectedType
    )
    {
        // Arrange
        const int zoneIndex = 1;
        const CommandSource source = CommandSource.Mqtt;

        // Act
        var result = CommandFactory.CreateZoneCommandFromPayload(zoneIndex, command, payload, source);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType(expectedType);
    }

    [Theory]
    [InlineData("volume/up", "", typeof(ClientVolumeUpCommand))]
    [InlineData("volume/down", "", typeof(ClientVolumeDownCommand))]
    [InlineData("volume", "+", typeof(ClientVolumeUpCommand))]
    [InlineData("volume", "-", typeof(ClientVolumeDownCommand))]
    public void CreateClientCommandFromPayload_Should_CreateCorrectVolumeCommands(
        string command,
        string payload,
        Type expectedType
    )
    {
        // Arrange
        const int clientIndex = 1;
        const CommandSource source = CommandSource.Mqtt;

        // Act
        var result = CommandFactory.CreateClientCommandFromPayload(clientIndex, command, payload, source);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType(expectedType);
    }

    #endregion
}
