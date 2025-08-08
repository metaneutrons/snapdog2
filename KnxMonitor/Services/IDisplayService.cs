using KnxMonitor.Models;

namespace KnxMonitor.Services;

/// <summary>
/// Interface for the display service that handles visual output.
/// </summary>
public interface IDisplayService : IDisposable
{
    /// <summary>
    /// Starts the display service.
    /// </summary>
    /// <param name="monitorService">KNX monitor service to display data from.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StartAsync(IKnxMonitorService monitorService, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the display service.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the connection status display.
    /// </summary>
    /// <param name="status">Connection status.</param>
    /// <param name="isConnected">Whether the connection is active.</param>
    void UpdateConnectionStatus(string status, bool isConnected);

    /// <summary>
    /// Displays a KNX message.
    /// </summary>
    /// <param name="message">Message to display.</param>
    void DisplayMessage(KnxMessage message);
}
