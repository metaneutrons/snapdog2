using Microsoft.Extensions.Logging;

namespace SnapDog2.Core.State;

/// <summary>
/// Factory for creating StateManager instances with proper configuration.
/// </summary>
public static class StateManagerFactory
{
    /// <summary>
    /// Creates a new StateManager instance with default configuration.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="initialState">The initial state. If null, creates an empty state.</param>
    /// <returns>A new <see cref="IStateManager"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when logger is null.</exception>
    public static IStateManager Create(ILogger<StateManager> logger, SnapDogState? initialState = null)
    {
        return new StateManager(logger, initialState);
    }

    /// <summary>
    /// Creates a new StateManager instance with custom configuration.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="initialState">The initial state. If null, creates an empty state.</param>
    /// <param name="maxHistorySize">Maximum number of state changes to keep in history.</param>
    /// <returns>A new <see cref="IStateManager"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when logger is null.</exception>
    public static IStateManager Create(ILogger<StateManager> logger, SnapDogState? initialState, int maxHistorySize)
    {
        return new StateManager(logger, initialState, maxHistorySize);
    }

    /// <summary>
    /// Creates a new StateManager instance with a specific system status.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="systemStatus">The initial system status.</param>
    /// <returns>A new <see cref="IStateManager"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when logger is null.</exception>
    public static IStateManager CreateWithStatus(ILogger<StateManager> logger, SystemStatus systemStatus)
    {
        var initialState = SnapDogState.CreateWithStatus(systemStatus);
        return new StateManager(logger, initialState);
    }
}
