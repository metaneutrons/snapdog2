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
using SnapDog2.Domain.Abstractions;
using SnapDog2.Shared.Models;

/// <summary>
/// In-memory implementation of error tracking service.
/// Maintains a rolling window of recent system errors for monitoring and diagnostics.
/// </summary>
public partial class ErrorTrackingService(ILogger<ErrorTrackingService> logger) : IErrorTrackingService
{
    private readonly ConcurrentQueue<ErrorDetails> _errors = new();
    private readonly object _lock = new();
    private const int MaxErrorCount = 100; // Keep last 100 errors
    private const int MaxErrorAgeHours = 24; // Keep errors for 24 hours

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

        LogErrorRecorded(error.Component ?? "Unknown", error.Context ?? "Unknown", error.Message);
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

        LogErrorsCleared();
    }

    // LoggerMessage definitions for high-performance logging
    [LoggerMessage(EventId = 114000, Level = LogLevel.Warning, Message = "ErrorRecorded: {Component} - {Context} - {Message}")]
    private partial void LogErrorRecorded(string component, string context, string message);

    [LoggerMessage(EventId = 114001, Level = LogLevel.Information, Message = "ErrorsCleared")]
    private partial void LogErrorsCleared();

    [LoggerMessage(EventId = 114002, Level = LogLevel.Debug, Message = "OldErrorsRemoved: {RemovedCount} errors, {RemainingCount} remaining")]
    private partial void LogOldErrorsRemoved(int removedCount, int remainingCount);

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
            LogOldErrorsRemoved(removedCount, this._errors.Count);
        }
    }

}
