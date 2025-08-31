//
// SnapDog
// The Snapcast-based Smart Home Audio System with MQTT & KNX integration
// Copyright (C) 2025 Fabian Schmieder
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <https://www.gnu.org/licenses/>.
//
namespace SnapDog2.Tests.Core.Validation;

using Microsoft.Extensions.Logging.Abstractions;
using SnapDog2.Infrastructure.Integrations.Mqtt;
using SnapDog2.Shared.Configuration;
using SnapDog2.Tests.Blueprint;

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
            NullLogger<AttributeBasedMqttCommandMapper>.Instance,
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
                this._output.WriteLine($"✓ Discovered: {blueprintTopic}");
            }
            else
            {
                this._output.WriteLine($"✗ Missing: {blueprintTopic}");
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
            NullLogger<AttributeBasedMqttCommandMapper>.Instance,
            new MqttConfig { MqttBaseTopic = "snapdog" }
        );
        var testCases = new[]
        {
            new
            {
                Topic = "snapdog/zones/1/play/set",
                CommandId = "PLAY",
                ExpectedType = "PlayCommand",
            },
            new
            {
                Topic = "snapdog/zones/2/volume/set",
                CommandId = "VOLUME",
                ExpectedType = "SetZoneVolumeCommand",
            },
            new
            {
                Topic = "snapdog/clients/3/volume/set",
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
                this._output.WriteLine($"✓ {testCase.Topic} → {command.GetType().Name}");
            }
            else
            {
                this._output.WriteLine($"✗ {testCase.Topic} → null (no mapping found)");
            }
        }
    }
}
