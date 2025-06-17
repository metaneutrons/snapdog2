using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Models.Entities;

namespace SnapDog2.Core.State;

/// <summary>
/// Thread-safe implementation of the state manager for SnapDog2 application state.
/// Manages immutable state updates with atomic operations and validation.
/// </summary>
public sealed class StateManager : IStateManager, IDisposable
{
    private readonly ILogger<StateManager> _logger;
    private readonly ReaderWriterLockSlim _stateLock;
    private readonly ConcurrentQueue<StateChangeRecord> _changeHistory;
    private readonly int _maxHistorySize;
    private SnapDogState _currentState;
    private volatile bool _disposed;

    /// <summary>
    /// Event raised when the state is updated.
    /// </summary>
    public event EventHandler<StateUpdatedEventArgs>? StateUpdated;

    /// <summary>
    /// Event raised when a state update fails validation.
    /// </summary>
    public event EventHandler<StateValidationFailedEventArgs>? StateValidationFailed;

    /// <summary>
    /// Initializes a new instance of the <see cref="StateManager"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="initialState">The initial state. If null, creates an empty state.</param>
    /// <param name="maxHistorySize">Maximum number of state changes to keep in history.</param>
    /// <exception cref="ArgumentNullException">Thrown when logger is null.</exception>
    public StateManager(ILogger<StateManager> logger, SnapDogState? initialState = null, int maxHistorySize = 100)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _stateLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        _changeHistory = new ConcurrentQueue<StateChangeRecord>();
        _maxHistorySize = Math.Max(1, maxHistorySize);
        _currentState = initialState ?? SnapDogState.CreateEmpty();

        _logger.LogInformation("StateManager initialized with version {Version}", _currentState.Version);

        // Validate initial state
        if (!_currentState.IsValid())
        {
            var message = "Initial state failed validation";
            _logger.LogError(message);
            throw new InvalidOperationException(message);
        }
    }

    /// <summary>
    /// Gets the current state as an immutable snapshot.
    /// </summary>
    /// <returns>The current <see cref="SnapDogState"/> instance.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the state manager has been disposed.</exception>
    public SnapDogState GetCurrentState()
    {
        ThrowIfDisposed();

        _stateLock.EnterReadLock();
        try
        {
            return _currentState;
        }
        finally
        {
            _stateLock.ExitReadLock();
        }
    }

    /// <summary>
    /// Updates the state using the provided update function.
    /// </summary>
    /// <param name="updateFunction">A function that takes the current state and returns a new state.</param>
    /// <returns>The updated <see cref="SnapDogState"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when updateFunction is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when state validation fails.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the state manager has been disposed.</exception>
    public SnapDogState UpdateState(Func<SnapDogState, SnapDogState> updateFunction)
    {
        if (updateFunction == null)
        {
            throw new ArgumentNullException(nameof(updateFunction));
        }

        ThrowIfDisposed();

        _stateLock.EnterWriteLock();
        try
        {
            var previousState = _currentState;
            var newState = updateFunction(previousState);

            if (newState == null)
            {
                throw new InvalidOperationException("Update function returned null state");
            }

            // Validate the new state
            if (!ValidateState(newState, previousState))
            {
                var message = "State update failed validation";
                _logger.LogError(message);

                var eventArgs = new StateValidationFailedEventArgs(newState, message);
                StateValidationFailed?.Invoke(this, eventArgs);

                throw new InvalidOperationException(message);
            }

            // Update the current state
            _currentState = newState;

            // Record the change
            RecordStateChange(previousState, newState);

            _logger.LogDebug(
                "State updated from version {PreviousVersion} to {NewVersion}",
                previousState.Version,
                newState.Version
            );

            // Raise the state updated event
            var stateUpdatedArgs = new StateUpdatedEventArgs(previousState, newState);
            StateUpdated?.Invoke(this, stateUpdatedArgs);

            return newState;
        }
        catch (Exception ex) when (!(ex is ArgumentNullException || ex is InvalidOperationException))
        {
            _logger.LogError(ex, "Unexpected error during state update");
            throw new InvalidOperationException("State update failed due to unexpected error", ex);
        }
        finally
        {
            _stateLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Updates the state using the provided update function with a retry mechanism for concurrency conflicts.
    /// </summary>
    /// <param name="updateFunction">A function that takes the current state and returns a new state.</param>
    /// <param name="maxRetries">Maximum number of retry attempts for concurrency conflicts.</param>
    /// <returns>The updated <see cref="SnapDogState"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when updateFunction is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when state validation fails or max retries exceeded.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the state manager has been disposed.</exception>
    public SnapDogState UpdateStateWithRetry(Func<SnapDogState, SnapDogState> updateFunction, int maxRetries = 3)
    {
        if (updateFunction == null)
        {
            throw new ArgumentNullException(nameof(updateFunction));
        }

        ThrowIfDisposed();

        var attempts = 0;
        var maxAttempts = Math.Max(1, maxRetries) + 1;

        while (attempts < maxAttempts)
        {
            try
            {
                return UpdateState(updateFunction);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("validation") && attempts < maxAttempts - 1)
            {
                attempts++;
                _logger.LogWarning(
                    "State update attempt {Attempt} failed, retrying. Error: {Error}",
                    attempts,
                    ex.Message
                );

                // Brief delay before retry to reduce contention
                Thread.Sleep(TimeSpan.FromMilliseconds(10 * attempts));
            }
        }

        var message = $"State update failed after {maxAttempts} attempts";
        _logger.LogError(message);
        throw new InvalidOperationException(message);
    }

    /// <summary>
    /// Attempts to update the state using the provided update function.
    /// </summary>
    /// <param name="updateFunction">A function that takes the current state and returns a new state.</param>
    /// <param name="updatedState">The updated state if successful; otherwise, null.</param>
    /// <returns>True if the update was successful; otherwise, false.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the state manager has been disposed.</exception>
    public bool TryUpdateState(Func<SnapDogState, SnapDogState> updateFunction, out SnapDogState? updatedState)
    {
        updatedState = null;

        if (updateFunction == null)
        {
            return false;
        }

        try
        {
            ThrowIfDisposed();
            updatedState = UpdateState(updateFunction);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "State update attempt failed");
            return false;
        }
    }

    /// <summary>
    /// Validates the current state for consistency.
    /// </summary>
    /// <returns>True if the current state is valid; otherwise, false.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the state manager has been disposed.</exception>
    public bool ValidateCurrentState()
    {
        ThrowIfDisposed();

        _stateLock.EnterReadLock();
        try
        {
            return _currentState.IsValid();
        }
        finally
        {
            _stateLock.ExitReadLock();
        }
    }

    /// <summary>
    /// Resets the state to an empty initial state.
    /// </summary>
    /// <returns>The reset <see cref="SnapDogState"/> instance.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the state manager has been disposed.</exception>
    public SnapDogState ResetState()
    {
        ThrowIfDisposed();

        return UpdateState(_ => SnapDogState.CreateEmpty());
    }

    /// <summary>
    /// Resets the state to the provided initial state.
    /// </summary>
    /// <param name="initialState">The initial state to reset to.</param>
    /// <returns>The reset <see cref="SnapDogState"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when initialState is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when initial state validation fails.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the state manager has been disposed.</exception>
    public SnapDogState ResetState(SnapDogState initialState)
    {
        if (initialState == null)
        {
            throw new ArgumentNullException(nameof(initialState));
        }

        ThrowIfDisposed();

        if (!initialState.IsValid())
        {
            throw new InvalidOperationException("Initial state failed validation");
        }

        return UpdateState(_ => initialState);
    }

    /// <summary>
    /// Gets the current state version for optimistic concurrency control.
    /// </summary>
    /// <returns>The current state version.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the state manager has been disposed.</exception>
    public long GetCurrentVersion()
    {
        ThrowIfDisposed();

        _stateLock.EnterReadLock();
        try
        {
            return _currentState.Version;
        }
        finally
        {
            _stateLock.ExitReadLock();
        }
    }

    /// <summary>
    /// Validates a new state against the previous state.
    /// </summary>
    /// <param name="newState">The new state to validate.</param>
    /// <param name="previousState">The previous state for comparison.</param>
    /// <returns>True if the new state is valid; otherwise, false.</returns>
    private bool ValidateState(SnapDogState newState, SnapDogState previousState)
    {
        try
        {
            // Basic state validation
            if (!newState.IsValid())
            {
                _logger.LogWarning("New state failed basic validation");
                return false;
            }

            // Version should be incremented
            if (newState.Version <= previousState.Version)
            {
                _logger.LogWarning(
                    "New state version {NewVersion} is not greater than previous version {PreviousVersion}",
                    newState.Version,
                    previousState.Version
                );
                return false;
            }

            // LastUpdated should be recent
            if (newState.LastUpdated < previousState.LastUpdated)
            {
                _logger.LogWarning(
                    "New state LastUpdated {NewLastUpdated} is before previous LastUpdated {PreviousLastUpdated}",
                    newState.LastUpdated,
                    previousState.LastUpdated
                );
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during state validation");
            return false;
        }
    }

    /// <summary>
    /// Records a state change in the history.
    /// </summary>
    /// <param name="previousState">The previous state.</param>
    /// <param name="newState">The new state.</param>
    private void RecordStateChange(SnapDogState previousState, SnapDogState newState)
    {
        var record = new StateChangeRecord(previousState, newState);
        _changeHistory.Enqueue(record);

        // Maintain history size limit
        while (_changeHistory.Count > _maxHistorySize)
        {
            _changeHistory.TryDequeue(out _);
        }
    }

    /// <summary>
    /// Throws an ObjectDisposedException if the state manager has been disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when the state manager has been disposed.</exception>
    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(StateManager));
        }
    }

    /// <summary>
    /// Disposes the state manager resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _stateLock.EnterWriteLock();
        try
        {
            _disposed = true;
            _logger.LogInformation("StateManager disposed");
        }
        finally
        {
            _stateLock.ExitWriteLock();
            _stateLock.Dispose();
        }
    }
}

/// <summary>
/// Represents a record of a state change for history tracking.
/// </summary>
internal sealed record StateChangeRecord
{
    /// <summary>
    /// Gets the previous state.
    /// </summary>
    public SnapDogState PreviousState { get; }

    /// <summary>
    /// Gets the new state.
    /// </summary>
    public SnapDogState NewState { get; }

    /// <summary>
    /// Gets the timestamp when the change occurred.
    /// </summary>
    public DateTime ChangedAt { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="StateChangeRecord"/> class.
    /// </summary>
    /// <param name="previousState">The previous state.</param>
    /// <param name="newState">The new state.</param>
    public StateChangeRecord(SnapDogState previousState, SnapDogState newState)
    {
        PreviousState = previousState ?? throw new ArgumentNullException(nameof(previousState));
        NewState = newState ?? throw new ArgumentNullException(nameof(newState));
        ChangedAt = DateTime.UtcNow;
    }
}
