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
namespace SnapDog2.Tests.Unit.Core.Configuration;

using FluentAssertions;
using SnapDog2.Core.Configuration;

public class SnapDogConfigurationTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_ShouldInitializeAllProperties()
    {
        // Act
        var config = new SnapDogConfiguration();

        // Assert
        config.System.Should().NotBeNull();
        config.Telemetry.Should().NotBeNull();
        config.Api.Should().NotBeNull();
        config.Services.Should().NotBeNull();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void MqttBaseTopic_ShouldReturnConfiguredValue()
    {
        // Arrange
        var config = new SnapDogConfiguration();
        config.Services.Mqtt.MqttBaseTopic = "myapp";

        // Act & Assert
        config.MqttBaseTopic.Should().Be("myapp");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void MqttBaseTopic_ShouldHaveDefaultValue()
    {
        // Act
        var config = new SnapDogConfiguration();

        // Assert
        config.MqttBaseTopic.Should().Be("snapdog");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Category", "Unit")]
    public void SystemConfig_ShouldHaveDefaultValues()
    {
        // Act
        var config = new SystemConfig();

        // Assert
        config.LogLevel.Should().Be("Information");
        config.Environment.Should().Be("Development");
        config.HealthChecksEnabled.Should().BeTrue();
        config.HealthChecksTimeout.Should().Be(30);
        config.HealthChecksTags.Should().Be("ready,live");
        config.LogFile.Should().BeNull();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Category", "Unit")]
    public void TelemetryConfig_ShouldHaveDefaultValues()
    {
        // Act
        var config = new TelemetryConfig();

        // Assert
        config.Enabled.Should().BeFalse();
        config.ServiceName.Should().Be("SnapDog2");
        config.SamplingRate.Should().Be(1.0);
        config.Otlp.Should().NotBeNull();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Category", "Unit")]
    public void OtlpConfig_ShouldHaveDefaultValues()
    {
        // Act
        var config = new OtlpConfig();

        // Assert
        config.Endpoint.Should().Be("http://localhost:4317");
        config.Protocol.Should().Be("grpc");
        config.Headers.Should().BeNull();
        config.TimeoutSeconds.Should().Be(30);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Category", "Unit")]
    public void ApiConfig_ShouldHaveDefaultValues()
    {
        // Act
        var config = new ApiConfig();

        // Assert
        config.AuthEnabled.Should().BeTrue();
        config.ApiKeys.Should().NotBeNull();
        config.ApiKeys.Should().BeEmpty();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Category", "Unit")]
    public void ServicesConfig_ShouldHaveDefaultValues()
    {
        // Act
        var config = new ServicesConfig();

        // Assert
        config.Snapcast.Should().NotBeNull();
        config.Mqtt.Should().NotBeNull();
        config.Knx.Should().NotBeNull();
        config.Subsonic.Should().NotBeNull();
    }
}
