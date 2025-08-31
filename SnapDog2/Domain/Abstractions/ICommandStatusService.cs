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
namespace SnapDog2.Domain.Abstractions;

using System.Threading.Tasks;

/// <summary>
/// Service for tracking command processing status and errors.
/// </summary>
public interface ICommandStatusService
{
    /// <summary>
    /// Gets the current command processing status.
    /// </summary>
    /// <returns>Status string: "idle", "processing", or "error"</returns>
    Task<string> GetStatusAsync();

    /// <summary>
    /// Gets recent command error messages.
    /// </summary>
    /// <returns>Array of recent error messages</returns>
    Task<string[]> GetRecentErrorsAsync();

    /// <summary>
    /// Records that command processing has started.
    /// </summary>
    void SetProcessing();

    /// <summary>
    /// Records that command processing has completed successfully.
    /// </summary>
    void SetIdle();

    /// <summary>
    /// Records a command processing error.
    /// </summary>
    /// <param name="errorMessage">The error message</param>
    void RecordError(string errorMessage);
}
