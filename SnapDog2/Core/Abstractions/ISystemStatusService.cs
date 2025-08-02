namespace SnapDog2.Core.Abstractions;

using SnapDog2.Core.Models;

/// <summary>
/// Service for managing system status information.
/// </summary>
public interface ISystemStatusService
{
    /// <summary>
    /// Gets the current system status.
    /// </summary>
    /// <returns>The current system status.</returns>
    Task<SystemStatus> GetCurrentStatusAsync();

    /// <summary>
    /// Gets the current version information.
    /// </summary>
    /// <returns>The version details.</returns>
    Task<VersionDetails> GetVersionInfoAsync();

    /// <summary>
    /// Gets the current server performance statistics.
    /// </summary>
    /// <returns>The server statistics.</returns>
    Task<ServerStats> GetServerStatsAsync();
}
