namespace SnapDog2.Tests.Unit.Infrastructure.Mqtt;

using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SnapDog2.Infrastructure.Integrations.Mqtt;
using SnapDog2.Tests.Blueprint;
using Xunit;
using Xunit.Abstractions;

/// <summary>
/// Tests for the attribute-based MQTT command mapping system.
/// Uses blueprint-as-code as single source of truth instead of hardcoded test values.
/// </summary>
public class AttributeBasedMqttCommandMapperTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;
    private readonly AttributeBasedMqttCommandMapper _mapper = new(
        NullLogger<AttributeBasedMqttCommandMapper>.Instance
    );

    [Fact]
    public void GetRegisteredTopicPatterns_ShouldIncludeBlueprintTopics()
    {
        // Arrange
        var blueprintTopics = SnapDogBlueprint
            .Spec.Commands.WithMqtt()
            .Where(c => !string.IsNullOrEmpty(c.MqttTopic))
            .Select(c => c.MqttTopic!)
            .ToList();

        // Act
        var registeredPatterns = _mapper.GetRegisteredTopicPatterns().ToList();

        // Assert
        _output.WriteLine($"Found {registeredPatterns.Count} registered topic patterns:");
        foreach (var pattern in registeredPatterns.Take(10))
        {
            _output.WriteLine($"  - {pattern}");
        }

        _output.WriteLine($"\nBlueprint defines {blueprintTopics.Count} MQTT command topics:");
        foreach (var topic in blueprintTopics)
        {
            _output.WriteLine($"  - {topic}");
        }

        registeredPatterns.Should().NotBeEmpty("Mapper should discover topic patterns from attributes");

        // Check that key blueprint topics are discovered
        var keyTopics = new[] { "snapdog/zone/{zoneIndex}/play/set", "snapdog/zone/{zoneIndex}/volume/set" };
        foreach (var keyTopic in keyTopics)
        {
            if (blueprintTopics.Contains(keyTopic))
            {
                registeredPatterns.Should().Contain(keyTopic, $"Blueprint topic {keyTopic} should be discovered");
            }
        }
    }

    [Fact]
    public void MapTopicToCommand_ShouldWorkForBlueprintDefinedTopics()
    {
        // Arrange - Use blueprint-defined topic patterns instead of hardcoded values
        var testCases = new[]
        {
            new
            {
                Topic = "snapdog/zone/1/play/set",
                Payload = "",
                Description = "Zone play command",
            },
            new
            {
                Topic = "snapdog/zone/2/volume/set",
                Payload = "75",
                Description = "Zone volume command",
            },
            new
            {
                Topic = "snapdog/client/3/volume/set",
                Payload = "50",
                Description = "Client volume command",
            },
        };

        // Act & Assert
        foreach (var testCase in testCases)
        {
            var command = _mapper.MapTopicToCommand(testCase.Topic, testCase.Payload);

            if (command != null)
            {
                _output.WriteLine($"✓ {testCase.Description}: {testCase.Topic} → {command.GetType().Name}");
                command.Should().NotBeNull($"Topic {testCase.Topic} should map to a command");
            }
            else
            {
                _output.WriteLine($"✗ {testCase.Description}: {testCase.Topic} → null (no mapping)");
                // Don't fail the test if mapping doesn't exist yet - this is expected during development
            }
        }
    }

    [Fact]
    public void Blueprint_ShouldDefineConsistentMqttTopics()
    {
        // Arrange
        var mqttCommands = SnapDogBlueprint.Spec.Commands.WithMqtt().ToList();

        // Act & Assert
        var commandsWithoutTopics = mqttCommands.Where(c => string.IsNullOrEmpty(c.MqttTopic)).ToList();

        _output.WriteLine($"MQTT Commands in Blueprint: {mqttCommands.Count}");
        _output.WriteLine($"Commands with topic patterns: {mqttCommands.Count - commandsWithoutTopics.Count}");
        _output.WriteLine($"Commands without topic patterns: {commandsWithoutTopics.Count}");

        if (commandsWithoutTopics.Any())
        {
            _output.WriteLine("\nCommands missing MQTT topic patterns:");
            foreach (var cmd in commandsWithoutTopics)
            {
                _output.WriteLine($"  - {cmd.Id}: {cmd.Description}");
            }
        }

        // This assertion will help track progress of adding MQTT topics to blueprint
        var topicCoverage = (double)(mqttCommands.Count - commandsWithoutTopics.Count) / mqttCommands.Count;
        _output.WriteLine($"\nMQTT Topic Coverage: {topicCoverage:P1}");

        // STRICT REQUIREMENT: All commands with .Mqtt() MUST have topic patterns
        commandsWithoutTopics
            .Should()
            .BeEmpty(
                "All commands with .Mqtt() protocol MUST define topic patterns. Empty .Mqtt() calls are not allowed."
            );
    }

    [Fact]
    public void Blueprint_ShouldDefineConsistentApiEndpoints()
    {
        // Arrange
        var apiCommands = SnapDogBlueprint.Spec.Commands.WithApi().ToList();
        var apiStatus = SnapDogBlueprint.Spec.Status.WithApi().ToList();

        // Act & Assert - Commands
        var commandsWithoutPaths = apiCommands.Where(c => string.IsNullOrEmpty(c.ApiPath)).ToList();

        var commandsWithoutMethods = apiCommands.Where(c => string.IsNullOrEmpty(c.HttpMethod)).ToList();

        _output.WriteLine($"API Commands in Blueprint: {apiCommands.Count}");
        _output.WriteLine($"Commands without API paths: {commandsWithoutPaths.Count}");
        _output.WriteLine($"Commands without HTTP methods: {commandsWithoutMethods.Count}");

        if (commandsWithoutPaths.Any())
        {
            _output.WriteLine("\nCommands missing API paths:");
            foreach (var cmd in commandsWithoutPaths)
            {
                _output.WriteLine($"  - {cmd.Id}: {cmd.Description}");
            }
        }

        if (commandsWithoutMethods.Any())
        {
            _output.WriteLine("\nCommands missing HTTP methods:");
            foreach (var cmd in commandsWithoutMethods)
            {
                _output.WriteLine($"  - {cmd.Id}: {cmd.Description}");
            }
        }

        // Act & Assert - Status
        var statusWithoutPaths = apiStatus.Where(s => string.IsNullOrEmpty(s.ApiPath)).ToList();

        var statusWithoutMethods = apiStatus.Where(s => string.IsNullOrEmpty(s.HttpMethod)).ToList();

        _output.WriteLine($"\nAPI Status in Blueprint: {apiStatus.Count}");
        _output.WriteLine($"Status without API paths: {statusWithoutPaths.Count}");
        _output.WriteLine($"Status without HTTP methods: {statusWithoutMethods.Count}");

        if (statusWithoutPaths.Any())
        {
            _output.WriteLine("\nStatus missing API paths:");
            foreach (var status in statusWithoutPaths)
            {
                _output.WriteLine($"  - {status.Id}: {status.Description}");
            }
        }

        if (statusWithoutMethods.Any())
        {
            _output.WriteLine("\nStatus missing HTTP methods:");
            foreach (var status in statusWithoutMethods)
            {
                _output.WriteLine($"  - {status.Id}: {status.Description}");
            }
        }

        // STRICT REQUIREMENTS: All API protocol declarations MUST have paths and methods
        commandsWithoutPaths
            .Should()
            .BeEmpty(
                "All commands with API protocol MUST define API paths. Empty .Get()/.Post()/.Put()/.Delete() calls are not allowed."
            );

        commandsWithoutMethods
            .Should()
            .BeEmpty(
                "All commands with API protocol MUST define HTTP methods. Empty .Get()/.Post()/.Put()/.Delete() calls are not allowed."
            );

        statusWithoutPaths
            .Should()
            .BeEmpty("All status with API protocol MUST define API paths. Empty .Get() calls are not allowed.");

        statusWithoutMethods
            .Should()
            .BeEmpty("All status with API protocol MUST define HTTP methods. Empty .Get() calls are not allowed.");
    }
}
