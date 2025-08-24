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

using FluentAssertions;
using SnapDog2.Tests.Blueprint;
using SnapDog2.Tests.Helpers;
using Xunit;

/// <summary>
/// Simplified blueprint validation tests.
/// Focuses on the most important validation: API endpoint coverage.
/// Uses static analysis to compare blueprint specification with actual controller implementations.
/// </summary>
public class BlueprintTests
{
    [Fact]
    [Trait("Category", "Blueprint")]
    public void Blueprint_Commands_ShouldHaveMatchingApiEndpoints()
    {
        // Act
        var (missingCommands, extraEndpoints) = StaticApiAnalyzer.CompareCommandImplementation();

        // Assert
        missingCommands
            .Should()
            .BeEmpty($"Missing API implementations for required commands: {string.Join(", ", missingCommands)}");

        extraEndpoints
            .Should()
            .BeEmpty($"Orphaned API endpoints not defined in blueprint: {string.Join(", ", extraEndpoints)}");
    }

    [Fact]
    [Trait("Category", "Blueprint")]
    public void Blueprint_Status_ShouldHaveMatchingApiEndpoints()
    {
        // Act
        var (missingStatus, extraEndpoints) = StaticApiAnalyzer.CompareStatusImplementation();

        // Assert
        missingStatus
            .Should()
            .BeEmpty($"Missing API implementations for required status: {string.Join(", ", missingStatus)}");

        extraEndpoints
            .Should()
            .BeEmpty($"Orphaned API endpoints not defined in blueprint: {string.Join(", ", extraEndpoints)}");
    }

    [Fact]
    [Trait("Category", "Blueprint")]
    public void Blueprint_ShouldBeWellFormed()
    {
        // Arrange & Act
        var commands = SnapDogBlueprint.Spec.Commands.ToList();
        var status = SnapDogBlueprint.Spec.Status.ToList();

        // Assert
        commands.Should().NotBeEmpty("Blueprint should define commands");
        status.Should().NotBeEmpty("Blueprint should define status");

        // Ensure all API commands have paths and methods
        var apiCommands = commands.Where(c => c.HasApi).ToList();
        foreach (var cmd in apiCommands)
        {
            cmd.ApiPath.Should().NotBeNullOrEmpty($"Command {cmd.Id} should have an API path");
            cmd.HttpMethod.Should().NotBeNullOrEmpty($"Command {cmd.Id} should have an HTTP method");
        }

        // Ensure all API status have paths and methods
        var apiStatus = status.Where(s => s.HasApi).ToList();
        foreach (var stat in apiStatus)
        {
            stat.ApiPath.Should().NotBeNullOrEmpty($"Status {stat.Id} should have an API path");
            stat.HttpMethod.Should().NotBeNullOrEmpty($"Status {stat.Id} should have an HTTP method");
        }

        // Ensure all MQTT commands have topic patterns
        var mqttCommands = commands.Where(c => c.HasMqtt && !c.IsExcludedFrom(Protocol.Mqtt)).ToList();
        foreach (var cmd in mqttCommands)
        {
            cmd.MqttTopic.Should().NotBeNullOrEmpty($"Command {cmd.Id} declares MQTT support but has no topic pattern");
        }

        // Ensure all MQTT status have topic patterns
        var mqttStatus = status.Where(s => s.HasMqtt && !s.IsExcludedFrom(Protocol.Mqtt)).ToList();
        foreach (var stat in mqttStatus)
        {
            stat.MqttTopic.Should()
                .NotBeNullOrEmpty($"Status {stat.Id} declares MQTT support but has no topic pattern");
        }
    }
}
