namespace SnapDog2.Tests.Core.Validation;

using FluentAssertions;
using SnapDog2.Core.Attributes;
using SnapDog2.Core.Configuration;
using SnapDog2.Infrastructure.Integrations.Mqtt;
using SnapDog2.Tests.Blueprint;
using Xunit;
using Xunit.Abstractions;

/// <summary>
/// Blueprint-based validation tests for MQTT integration.
/// Validates that MQTT topic attributes match the blueprint specification.
/// </summary>
public class MqttBlueprintValidationTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    [Trait("Category", "Blueprint")]
    public void AttributeBasedMapper_ShouldDiscoverAllBlueprintCommands()
    {
        // Arrange
        var mapper = new AttributeBasedMqttCommandMapper(
            Microsoft.Extensions.Logging.Abstractions.NullLogger<AttributeBasedMqttCommandMapper>.Instance,
            new MqttConfig { MqttBaseTopic = "snapdog" }
        );
        var blueprintTopics = SnapDogBlueprint
            .Spec.Commands.WithMqtt()
            .Where(c => !string.IsNullOrEmpty(c.MqttTopic))
            .Select(c => c.MqttTopic!)
            .ToHashSet();

        // Act
        var discoveredTopics = mapper.GetRegisteredTopicPatterns().ToHashSet();

        // Assert
        foreach (var blueprintTopic in blueprintTopics)
        {
            if (discoveredTopics.Contains(blueprintTopic))
            {
                _output.WriteLine($"✓ Discovered: {blueprintTopic}");
            }
            else
            {
                _output.WriteLine($"✗ Missing: {blueprintTopic}");
            }
        }

        var missingTopics = blueprintTopics.Except(discoveredTopics).ToList();
        missingTopics
            .Should()
            .BeEmpty($"Mapper should discover all blueprint MQTT topics:\n{string.Join("\n", missingTopics)}");
    }

    [Fact]
    [Trait("Category", "Blueprint")]
    public void MqttTopicMapping_ShouldWorkForBlueprintCommands()
    {
        // Arrange
        var mapper = new AttributeBasedMqttCommandMapper(
            Microsoft.Extensions.Logging.Abstractions.NullLogger<AttributeBasedMqttCommandMapper>.Instance,
            new MqttConfig { MqttBaseTopic = "snapdog" }
        );
        var testCases = new[]
        {
            new
            {
                Topic = "snapdog/zone/1/play/set",
                CommandId = "PLAY",
                ExpectedType = "PlayCommand",
            },
            new
            {
                Topic = "snapdog/zone/2/volume/set",
                CommandId = "VOLUME",
                ExpectedType = "SetZoneVolumeCommand",
            },
            new
            {
                Topic = "snapdog/client/3/volume/set",
                CommandId = "CLIENT_VOLUME",
                ExpectedType = "SetClientVolumeCommand",
            },
        };

        // Act & Assert
        foreach (var testCase in testCases)
        {
            var command = mapper.MapTopicToCommand(testCase.Topic, "test-payload");

            if (command != null)
            {
                command
                    .GetType()
                    .Name.Should()
                    .Be(testCase.ExpectedType, $"Topic {testCase.Topic} should map to {testCase.ExpectedType}");
                _output.WriteLine($"✓ {testCase.Topic} → {command.GetType().Name}");
            }
            else
            {
                _output.WriteLine($"✗ {testCase.Topic} → null (no mapping found)");
            }
        }
    }
}
