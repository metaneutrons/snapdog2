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
using SnapDog2.Shared.Configuration;

public class RadioStationConfigTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_ShouldInitializeWithNullValues()
    {
        // Act
        var config = new RadioStationConfig();

        // Assert
        config.Name.Should().BeNull();
        config.Url.Should().BeNull();
    }
}
