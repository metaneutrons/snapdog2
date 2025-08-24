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
namespace SnapDog2.Server.Features.Global.Services.Abstractions;

using SnapDog2.Core.Models;

/// <summary>
/// Service for managing and publishing global system status.
/// </summary>
public interface IGlobalStatusService
{
    /// <summary>
    /// Publishes the current system status.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task PublishSystemStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes system error information.
    /// </summary>
    /// <param name="errorDetails">The error details to publish.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task PublishErrorStatusAsync(ErrorDetails errorDetails, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes the current version information.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task PublishVersionInfoAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes the current server statistics.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task PublishServerStatsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts periodic publishing of system status and server stats.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StartPeriodicPublishingAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops periodic publishing.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StopPeriodicPublishingAsync();
}
