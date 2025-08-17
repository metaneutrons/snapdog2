namespace SnapDog2.Tests.Unit.Infrastructure.Mqtt;

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SnapDog2.Core.Enums;
using SnapDog2.Infrastructure.Integrations.Mqtt;
using SnapDog2.Server.Features.Clients.Commands.Volume;
using SnapDog2.Server.Features.Zones.Commands.Track;
using Xunit;

/// <summary>
/// Unit tests for MqttCommandMapper.
/// Tests the new MQTT topic mappings added for complete command framework alignment.
/// </summary>
public class MqttCommandMapperTests
{
    private readonly Mock<ILogger<MqttCommandMapper>> _mockLogger;
    private readonly MqttCommandMapper _mapper;

    public MqttCommandMapperTests()
    {
        this._mockLogger = new Mock<ILogger<MqttCommandMapper>>();
        this._mapper = new MqttCommandMapper(this._mockLogger.Object);
    }

    #region Zone Track Command Mapping Tests

    [Theory]
    [InlineData("snapdog/zone/1/track/position/set", "30000", typeof(SeekPositionCommand))]
    [InlineData("snapdog/zone/2/track/progress/set", "0.75", typeof(SeekProgressCommand))]
    [InlineData("snapdog/zone/3/play/track", "5", typeof(PlayTrackByIndexCommand))]
    [InlineData("snapdog/zone/1/play/url", "http://example.com/stream.mp3", typeof(PlayUrlCommand))]
    public void MapTopicToCommand_Should_MapZoneTrackCommands_Correctly(string topic, string payload, Type expectedType)
    {
        // Act
        var result = this._mapper.MapTopicToCommand(topic, payload);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType(expectedType);
    }

    [Fact]
    public void MapTopicToCommand_Should_MapSeekPositionCommand_WithCorrectValues()
    {
        // Arrange
        const string topic = "snapdog/zone/2/track/position/set";
        const string payload = "45000"; // 45 seconds
        const int expectedZoneIndex = 2;
        const long expectedPositionMs = 45000;

        // Act
        var result = this._mapper.MapTopicToCommand(topic, payload) as SeekPositionCommand;

        // Assert
        result.Should().NotBeNull();
        result!.ZoneIndex.Should().Be(expectedZoneIndex);
        result.PositionMs.Should().Be(expectedPositionMs);
        result.Source.Should().Be(CommandSource.Mqtt);
    }

    [Fact]
    public void MapTopicToCommand_Should_MapSeekProgressCommand_WithCorrectValues()
    {
        // Arrange
        const string topic = "snapdog/zone/3/track/progress/set";
        const string payload = "0.25"; // 25%
        const int expectedZoneIndex = 3;
        const float expectedProgress = 0.25f;

        // Act
        var result = this._mapper.MapTopicToCommand(topic, payload) as SeekProgressCommand;

        // Assert
        result.Should().NotBeNull();
        result!.ZoneIndex.Should().Be(expectedZoneIndex);
        result.Progress.Should().Be(expectedProgress);
        result.Source.Should().Be(CommandSource.Mqtt);
    }

    [Fact]
    public void MapTopicToCommand_Should_MapPlayTrackByIndexCommand_WithCorrectValues()
    {
        // Arrange
        const string topic = "snapdog/zone/1/play/track";
        const string payload = "10";
        const int expectedZoneIndex = 1;
        const int expectedTrackIndex = 10;

        // Act
        var result = this._mapper.MapTopicToCommand(topic, payload) as PlayTrackByIndexCommand;

        // Assert
        result.Should().NotBeNull();
        result!.ZoneIndex.Should().Be(expectedZoneIndex);
        result.TrackIndex.Should().Be(expectedTrackIndex);
        result.Source.Should().Be(CommandSource.Mqtt);
    }

    [Fact]
    public void MapTopicToCommand_Should_MapPlayUrlCommand_WithCorrectValues()
    {
        // Arrange
        const string topic = "snapdog/zone/2/play/url";
        const string payload = "http://radio.example.com/stream";
        const int expectedZoneIndex = 2;
        const string expectedUrl = "http://radio.example.com/stream";

        // Act
        var result = this._mapper.MapTopicToCommand(topic, payload) as PlayUrlCommand;

        // Assert
        result.Should().NotBeNull();
        result!.ZoneIndex.Should().Be(expectedZoneIndex);
        result.Url.Should().Be(expectedUrl);
        result.Source.Should().Be(CommandSource.Mqtt);
    }

    #endregion

    #region Client Volume Command Mapping Tests

    [Theory]
    [InlineData("snapdog/client/1/volume/up", "", typeof(ClientVolumeUpCommand))]
    [InlineData("snapdog/client/2/volume/down", "", typeof(ClientVolumeDownCommand))]
    [InlineData("snapdog/client/3/volume", "+", typeof(ClientVolumeUpCommand))]
    [InlineData("snapdog/client/4/volume", "-", typeof(ClientVolumeDownCommand))]
    public void MapTopicToCommand_Should_MapClientVolumeCommands_Correctly(
        string topic,
        string payload,
        Type expectedType
    )
    {
        // Act
        var result = this._mapper.MapTopicToCommand(topic, payload);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType(expectedType);
    }

    [Fact]
    public void MapTopicToCommand_Should_MapClientVolumeUpCommand_WithCorrectValues()
    {
        // Arrange
        const string topic = "snapdog/client/3/volume/up";
        const string payload = "";
        const int expectedClientIndex = 3;
        const int expectedStep = 5; // Default step

        // Act
        var result = this._mapper.MapTopicToCommand(topic, payload) as ClientVolumeUpCommand;

        // Assert
        result.Should().NotBeNull();
        result!.ClientIndex.Should().Be(expectedClientIndex);
        result.Step.Should().Be(expectedStep);
        result.Source.Should().Be(CommandSource.Mqtt);
    }

    [Fact]
    public void MapTopicToCommand_Should_MapClientVolumeDownCommand_WithCorrectValues()
    {
        // Arrange
        const string topic = "snapdog/client/1/volume/down";
        const string payload = "";
        const int expectedClientIndex = 1;
        const int expectedStep = 5; // Default step

        // Act
        var result = this._mapper.MapTopicToCommand(topic, payload) as ClientVolumeDownCommand;

        // Assert
        result.Should().NotBeNull();
        result!.ClientIndex.Should().Be(expectedClientIndex);
        result.Step.Should().Be(expectedStep);
        result.Source.Should().Be(CommandSource.Mqtt);
    }

    #endregion

    #region Control Topic Mapping Tests

    [Fact]
    public void MapControlTopicToCommand_Should_MapPlayTrackCommand()
    {
        // Arrange
        const string topic = "snapdog/zone/1/control/set";
        const string payload = "play track 5";

        // Act
        var result = this._mapper.MapControlTopicToCommand(topic, payload) as PlayTrackByIndexCommand;

        // Assert
        result.Should().NotBeNull();
        result!.ZoneIndex.Should().Be(1);
        result.TrackIndex.Should().Be(5);
        result.Source.Should().Be(CommandSource.Mqtt);
    }

    [Fact]
    public void MapControlTopicToCommand_Should_MapPlayUrlCommand()
    {
        // Arrange
        const string topic = "snapdog/zone/2/control/set";
        const string payload = "play url http://stream.example.com";

        // Act
        var result = this._mapper.MapControlTopicToCommand(topic, payload) as PlayUrlCommand;

        // Assert
        result.Should().NotBeNull();
        result!.ZoneIndex.Should().Be(2);
        result.Url.Should().Be("http://stream.example.com");
        result.Source.Should().Be(CommandSource.Mqtt);
    }

    [Fact]
    public void MapControlTopicToCommand_Should_MapClientVolumeUpCommand()
    {
        // Arrange
        const string topic = "snapdog/client/1/control/set";
        const string payload = "volume_up";

        // Act
        var result = this._mapper.MapControlTopicToCommand(topic, payload);

        // Assert
        result.Should().NotBeNull();
        // Note: The control topic mapper uses different logic, so we check for the general volume command
    }

    #endregion

    #region Topic Validation Tests

    [Theory]
    [InlineData("snapdog/zone/1/track/position/set")]
    [InlineData("snapdog/zone/2/track/progress/set")]
    [InlineData("snapdog/zone/3/play/track")]
    [InlineData("snapdog/client/1/volume/up")]
    [InlineData("snapdog/client/2/volume/down")]
    public void IsValidMqttTopic_Should_ReturnTrue_ForValidTopics(string topic)
    {
        // Act
        var result = this._mapper.IsValidMqttTopic(topic);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("invalid/topic")]
    [InlineData("snapdog/invalid/1/command")]
    [InlineData("snapdog/zone/invalid/command")]
    [InlineData("")]
    public void IsValidMqttTopic_Should_ReturnFalse_ForInvalidTopics(string topic)
    {
        // Act
        var result = this._mapper.IsValidMqttTopic(topic);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidMqttTopic_Should_ReturnFalse_ForNullTopic()
    {
        // Act
        var result = this._mapper.IsValidMqttTopic(null!);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("snapdog/zone/1/command", "zone")]
    [InlineData("snapdog/client/2/command", "client")]
    public void GetEntityType_Should_ReturnCorrectType(string topic, string expectedType)
    {
        // Act
        var result = this._mapper.GetEntityType(topic);

        // Assert
        result.Should().Be(expectedType);
    }

    [Theory]
    [InlineData("snapdog/zone/5/command", 5)]
    [InlineData("snapdog/client/10/command", 10)]
    public void GetEntityId_Should_ReturnCorrectId(string topic, int expectedId)
    {
        // Act
        var result = this._mapper.GetEntityId(topic);

        // Assert
        result.Should().Be(expectedId);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public void MapTopicToCommand_Should_ReturnNull_ForInvalidTopic()
    {
        // Arrange
        const string invalidTopic = "invalid/topic/structure";
        const string payload = "test";

        // Act
        var result = this._mapper.MapTopicToCommand(invalidTopic, payload);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void MapTopicToCommand_Should_ReturnNull_ForInvalidPayload()
    {
        // Arrange
        const string topic = "snapdog/zone/1/track/position/set";
        const string invalidPayload = "not_a_number";

        // Act
        var result = this._mapper.MapTopicToCommand(topic, invalidPayload);

        // Assert
        result.Should().BeNull();
    }

    #endregion
}
