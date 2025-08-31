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
namespace SnapDog2.Infrastructure.Services;

using System.Collections.Concurrent;
using SnapDog2.Domain.Abstractions;

/// <summary>
/// Simple in-memory implementation of command status tracking.
/// </summary>
public class CommandStatusService : ICommandStatusService
{
    private readonly ConcurrentQueue<(DateTime Timestamp, string Message)> _recentErrors = new();
    private volatile string _currentStatus = "idle";
    private readonly object _statusLock = new();

    /// <summary>
    /// Gets the current command processing status.
    /// </summary>
    /// <returns>Status string: "idle", "processing", or "error"</returns>
    public Task<string> GetStatusAsync()
    {
        return Task.FromResult(this._currentStatus);
    }

    /// <summary>
    /// Gets recent command error messages (last 50 errors from past 24 hours).
    /// </summary>
    /// <returns>Array of recent error messages</returns>
    public Task<string[]> GetRecentErrorsAsync()
    {
        var cutoff = DateTime.UtcNow.AddHours(-24);

        // Clean up old errors and get recent ones
        var recentErrors = new List<string>();
        var tempQueue = new ConcurrentQueue<(DateTime, string)>();

        while (this._recentErrors.TryDequeue(out var error))
        {
            if (error.Timestamp >= cutoff)
            {
                tempQueue.Enqueue(error);
                recentErrors.Add($"[{error.Timestamp:yyyy-MM-dd HH:mm:ss}] {error.Message}");
            }
        }

        // Put back the recent errors
        while (tempQueue.TryDequeue(out var error))
        {
            this._recentErrors.Enqueue(error);
        }

        return Task.FromResult(recentErrors.TakeLast(50).ToArray());
    }

    /// <summary>
    /// Records that command processing has started.
    /// </summary>
    public void SetProcessing()
    {
        lock (this._statusLock)
        {
            this._currentStatus = "processing";
        }
    }

    /// <summary>
    /// Records that command processing has completed successfully.
    /// </summary>
    public void SetIdle()
    {
        lock (this._statusLock)
        {
            this._currentStatus = "idle";
        }
    }

    /// <summary>
    /// Records a command processing error.
    /// </summary>
    /// <param name="errorMessage">The error message</param>
    public void RecordError(string errorMessage)
    {
        lock (this._statusLock)
        {
            this._currentStatus = "error";
        }

        this._recentErrors.Enqueue((DateTime.UtcNow, errorMessage));

        // Keep only last 100 errors to prevent memory issues
        while (this._recentErrors.Count > 100)
        {
            this._recentErrors.TryDequeue(out _);
        }
    }
}
