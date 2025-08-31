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

using SnapDog2.Shared.Models;

/// <summary>
/// Service for tracking and retrieving system errors.
/// </summary>
public interface IErrorTrackingService
{
    /// <summary>
    /// Records a system error.
    /// </summary>
    /// <param name="error">The error details to record.</param>
    void RecordError(ErrorDetails error);

    /// <summary>
    /// Records an exception as a system error.
    /// </summary>
    /// <param name="exception">The exception to record.</param>
    /// <param name="component">The component where the error occurred.</param>
    /// <param name="operation">The operation that was being performed.</param>
    void RecordException(Exception exception, string component, string? operation = null);

    /// <summary>
    /// Gets the most recent system error.
    /// </summary>
    /// <returns>The most recent error details, or null if no errors have been recorded.</returns>
    Task<ErrorDetails?> GetLatestErrorAsync();

    /// <summary>
    /// Gets recent system errors within the specified time window.
    /// </summary>
    /// <param name="timeWindow">The time window to look back for errors.</param>
    /// <returns>A list of recent errors within the time window.</returns>
    Task<List<ErrorDetails>> GetRecentErrorsAsync(TimeSpan timeWindow);

    /// <summary>
    /// Clears all recorded errors.
    /// </summary>
    void ClearErrors();
}
