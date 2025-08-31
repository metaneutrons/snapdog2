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
namespace SnapDog2.Domain.Services;

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SnapDog2.Domain.Abstractions;
using SnapDog2.Shared.Models;

/// <summary>
/// In-memory implementation of error tracking service.
/// Maintains a rolling window of recent system errors for monitoring and diagnostics.
/// </summary>
public partial class ErrorTrackingService : IErrorTrackingService
{
    private readonly ILogger<ErrorTrackingService> _logger;
    private readonly ConcurrentQueue<ErrorDetails> _errors = new();
    private readonly object _lock = new();
    private const int MaxErrorCount = 100; // Keep last 100 errors
    private const int MaxErrorAgeHours = 24; // Keep errors for 24 hours

    public ErrorTrackingService(ILogger<ErrorTrackingService> logger)
    {
        this._logger = logger;
    }

    /// <inheritdoc/>
    public void RecordError(ErrorDetails error)
    {
        if (error == null)
        {
            return;
        }

        lock (this._lock)
        {
            this._errors.Enqueue(error);
            this.CleanupOldErrors();
        }

        LogErrorRecorded(this._logger, error.Component ?? "Unknown", error.Context ?? "Unknown", error.Message);
    }

    /// <inheritdoc/>
    public void RecordException(Exception exception, string component, string? operation = null)
    {
        if (exception == null)
        {
            return;
        }

        var error = new ErrorDetails
        {
            TimestampUtc = DateTime.UtcNow,
            Level = 3, // Error level
            ErrorCode = exception.GetType().Name,
            Message = exception.Message,
            Context = operation != null ? $"Operation: {operation}, StackTrace: {exception.StackTrace}" : exception.StackTrace,
            Component = component
        };

        this.RecordError(error);
    }

    /// <inheritdoc/>
    public Task<ErrorDetails?> GetLatestErrorAsync()
    {
        lock (this._lock)
        {
            this.CleanupOldErrors();

            // Get the most recent error
            var errors = this._errors.ToArray();
            var latestError = errors.LastOrDefault();

            return Task.FromResult(latestError);
        }
    }

    /// <inheritdoc/>
    public Task<List<ErrorDetails>> GetRecentErrorsAsync(TimeSpan timeWindow)
    {
        lock (this._lock)
        {
            this.CleanupOldErrors();

            var cutoffTime = DateTime.UtcNow - timeWindow;
            var recentErrors = this._errors
                .Where(e => e.TimestampUtc >= cutoffTime)
                .OrderByDescending(e => e.TimestampUtc)
                .ToList();

            return Task.FromResult(recentErrors);
        }
    }

    /// <inheritdoc/>
    public void ClearErrors()
    {
        lock (this._lock)
        {
            this._errors.Clear();
        }

        LogErrorsCleared(this._logger);
    }

    private void CleanupOldErrors()
    {
        var cutoffTime = DateTime.UtcNow.AddHours(-MaxErrorAgeHours);
        var initialCount = this._errors.Count;

        // Remove old errors
        while (this._errors.TryPeek(out var oldestError) &&
               (oldestError.TimestampUtc < cutoffTime || this._errors.Count > MaxErrorCount))
        {
            this._errors.TryDequeue(out _);
        }

        var removedCount = initialCount - this._errors.Count;
        if (removedCount > 0)
        {
            LogOldErrorsRemoved(this._logger, removedCount, this._errors.Count);
        }
    }

    [LoggerMessage(
        EventId = 6300,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Error recorded in {Component}.{Operation}: {Message}"
    )]
    private static partial void LogErrorRecorded(ILogger logger, string component, string operation, string message);

    [LoggerMessage(
        EventId = 6301,
        Level = Microsoft.Extensions.Logging.LogLevel.Information,
        Message = "Error tracking cleared"
    )]
    private static partial void LogErrorsCleared(ILogger logger);

    [LoggerMessage(
        EventId = 6302,
        Level = Microsoft.Extensions.Logging.LogLevel.Debug,
        Message = "Removed {RemovedCount} old errors, {RemainingCount} errors remaining"
    )]
    private static partial void LogOldErrorsRemoved(ILogger logger, int removedCount, int remainingCount);
}
