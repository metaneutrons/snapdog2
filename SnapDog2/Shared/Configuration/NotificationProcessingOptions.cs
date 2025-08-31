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
namespace SnapDog2.Shared.Configuration;

/// <summary>
/// Options for the background notification queue and processor.
/// </summary>
public sealed class NotificationProcessingOptions
{
    /// <summary>
    /// Maximum number of items allowed in the queue before producers back-pressure.
    /// </summary>
    public int MaxQueueCapacity { get; set; } = 1024;

    /// <summary>
    /// Degree of parallelism for processing notifications.
    /// </summary>
    public int MaxConcurrency { get; set; } = 2;

    /// <summary>
    /// Maximum retry attempts for a single notification before it is dead-lettered.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Base delay in milliseconds for exponential backoff between retries.
    /// </summary>
    public int RetryBaseDelayMs { get; set; } = 250;

    /// <summary>
    /// Maximum delay in milliseconds between retries.
    /// </summary>
    public int RetryMaxDelayMs { get; set; } = 5000;

    /// <summary>
    /// Graceful shutdown timeout in seconds to allow draining the queue.
    /// </summary>
    public int ShutdownTimeoutSeconds { get; set; } = 10;
}
