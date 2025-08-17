namespace SnapDog2.Core.Abstractions;

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
