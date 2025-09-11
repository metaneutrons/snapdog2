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
namespace SnapDog2.TestKit.Base;

/// <summary>
/// Interface for scenario tests that can be discovered and executed via reflection.
/// </summary>
public interface IScenarioTest
{
    /// <summary>
    /// Gets the display name of the scenario test.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the emoji icon for the scenario test.
    /// </summary>
    string Icon { get; }

    /// <summary>
    /// Executes the scenario test.
    /// </summary>
    /// <returns>True if the test passed, false if it failed.</returns>
    Task<bool> ExecuteAsync();
}
