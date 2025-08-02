namespace SnapDog2.Server.Services.Abstractions;

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
