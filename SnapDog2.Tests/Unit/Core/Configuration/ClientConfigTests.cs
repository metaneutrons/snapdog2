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

public class ClientConfigTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_ShouldInitializeNestedProperties()
    {
        // Act
        var config = new ClientConfig();

        // Assert
        config.Knx.Should().NotBeNull();
        config.DefaultZone.Should().Be(1);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ClientKnxConfig_ShouldHaveDefaultValues()
    {
        // Act
        var config = new ClientKnxConfig();

        // Assert
        config.Enabled.Should().BeFalse();
        config.Volume.Should().BeNull();
        config.VolumeStatus.Should().BeNull();
        config.Mute.Should().BeNull();
        config.MuteStatus.Should().BeNull();
    }
}
