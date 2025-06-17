using SnapDog2.Core.Models.Entities;

namespace SnapDog2.Core.State;

/// <summary>
/// Defines the contract for managing the immutable application state.
/// Provides thread-safe operations for state access and updates.
/// </summary>
public interface IStateManager : IDisposable
{
    /// <summary>
    /// Gets the current state as an immutable snapshot.
    /// </summary>
    /// <returns>The current <see cref="SnapDogState"/> instance.</returns>
    SnapDogState GetCurrentState();

    /// <summary>
    /// Updates the state using the provided update function.
    /// </summary>
    /// <param name="updateFunction">A function that takes the current state and returns a new state.</param>
    /// <returns>The updated <see cref="SnapDogState"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when updateFunction is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when state validation fails.</exception>
    SnapDogState UpdateState(Func<SnapDogState, SnapDogState> updateFunction);

    /// <summary>
    /// Updates the state using the provided update function with a retry mechanism for concurrency conflicts.
    /// </summary>
    /// <param name="updateFunction">A function that takes the current state and returns a new state.</param>
    /// <param name="maxRetries">Maximum number of retry attempts for concurrency conflicts.</param>
    /// <returns>The updated <see cref="SnapDogState"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when updateFunction is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when state validation fails or max retries exceeded.</exception>
    SnapDogState UpdateStateWithRetry(Func<SnapDogState, SnapDogState> updateFunction, int maxRetries = 3);

    /// <summary>
    /// Attempts to update the state using the provided update function.
    /// </summary>
    /// <param name="updateFunction">A function that takes the current state and returns a new state.</param>
    /// <param name="updatedState">The updated state if successful; otherwise, null.</param>
    /// <returns>True if the update was successful; otherwise, false.</returns>
    bool TryUpdateState(Func<SnapDogState, SnapDogState> updateFunction, out SnapDogState? updatedState);

    /// <summary>
    /// Validates the current state for consistency.
    /// </summary>
    /// <returns>True if the current state is valid; otherwise, false.</returns>
    bool ValidateCurrentState();

    /// <summary>
    /// Resets the state to an empty initial state.
    /// </summary>
    /// <returns>The reset <see cref="SnapDogState"/> instance.</returns>
    SnapDogState ResetState();

    /// <summary>
    /// Resets the state to the provided initial state.
    /// </summary>
    /// <param name="initialState">The initial state to reset to.</param>
    /// <returns>The reset <see cref="SnapDogState"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when initialState is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when initial state validation fails.</exception>
    SnapDogState ResetState(SnapDogState initialState);

    /// <summary>
    /// Gets the current state version for optimistic concurrency control.
    /// </summary>
    long GetCurrentVersion();

    /// <summary>
    /// Event raised when the state is updated.
    /// </summary>
    event EventHandler<StateUpdatedEventArgs>? StateUpdated;

    /// <summary>
    /// Event raised when a state update fails validation.
    /// </summary>
    event EventHandler<StateValidationFailedEventArgs>? StateValidationFailed;
}

/// <summary>
/// Provides data for the StateUpdated event.
/// </summary>
public sealed class StateUpdatedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the previous state before the update.
    /// </summary>
    public SnapDogState PreviousState { get; }

    /// <summary>
    /// Gets the new state after the update.
    /// </summary>
    public SnapDogState NewState { get; }

    /// <summary>
    /// Gets the timestamp when the state was updated.
    /// </summary>
    public DateTime UpdatedAt { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="StateUpdatedEventArgs"/> class.
    /// </summary>
    /// <param name="previousState">The previous state.</param>
    /// <param name="newState">The new state.</param>
    public StateUpdatedEventArgs(SnapDogState previousState, SnapDogState newState)
    {
        PreviousState = previousState ?? throw new ArgumentNullException(nameof(previousState));
        NewState = newState ?? throw new ArgumentNullException(nameof(newState));
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Provides data for the StateValidationFailed event.
/// </summary>
public sealed class StateValidationFailedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the state that failed validation.
    /// </summary>
    public SnapDogState FailedState { get; }

    /// <summary>
    /// Gets the validation error message.
    /// </summary>
    public string ErrorMessage { get; }

    /// <summary>
    /// Gets the exception that caused the validation failure, if any.
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>
    /// Gets the timestamp when the validation failed.
    /// </summary>
    public DateTime FailedAt { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="StateValidationFailedEventArgs"/> class.
    /// </summary>
    /// <param name="failedState">The state that failed validation.</param>
    /// <param name="errorMessage">The validation error message.</param>
    /// <param name="exception">The exception that caused the failure, if any.</param>
    public StateValidationFailedEventArgs(SnapDogState failedState, string errorMessage, Exception? exception = null)
    {
        FailedState = failedState ?? throw new ArgumentNullException(nameof(failedState));
        ErrorMessage = errorMessage ?? throw new ArgumentNullException(nameof(errorMessage));
        Exception = exception;
        FailedAt = DateTime.UtcNow;
    }
}
