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
namespace SnapDog2.Shared.Models;

using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Marker interface for Result types, useful for Cortex.Mediator behaviors and constraints.
/// </summary>
public interface IResult
{
    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    bool IsFailure { get; }

    /// <summary>
    /// Gets the error message if the operation failed. Returns null if successful.
    /// </summary>
    string? ErrorMessage { get; }

    /// <summary>
    /// Gets the exception that caused the operation to fail, if available. Returns null otherwise.
    /// </summary>
    Exception? Exception { get; }
}

/// <summary>
/// Represents the outcome of an operation that does not return a value.
/// Use static factory methods Success() and Failure() to create instances.
/// </summary>
public class Result : IResult
{
    /// <inheritdoc />
    public bool IsSuccess { get; }

    /// <inheritdoc />
    public bool IsFailure => !this.IsSuccess;

    /// <inheritdoc />
    public string? ErrorMessage { get; }

    /// <inheritdoc />
    public Exception? Exception { get; }

    /// <summary>
    /// Protected constructor to enforce usage of factory methods.
    /// </summary>
    internal protected Result(bool isSuccess, string? errorMessage, Exception? exception)
    {
        // Validate consistency: Success implies no error, Failure requires an error.
        if (isSuccess && (!string.IsNullOrEmpty(errorMessage) || exception != null))
        {
            throw new InvalidOperationException(
                "Assertion failed: A successful result cannot have an error message or exception."
            );
        }

        if (!isSuccess && string.IsNullOrEmpty(errorMessage) && exception == null)
        {
            throw new InvalidOperationException(
                "Assertion failed: A failed result requires an error message or an exception."
            );
        }

        this.IsSuccess = isSuccess;
        this.ErrorMessage = errorMessage;
        this.Exception = exception;
    }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <returns>A successful Result instance.</returns>
    public static Result Success()
    {
        return new Result(true, null, null);
    }

    /// <summary>
    /// Creates a failed result with the specified error message.
    /// </summary>
    /// <param name="errorMessage">The error message describing the failure.</param>
    /// <returns>A failed Result instance.</returns>
    public static Result Failure(string errorMessage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);
        return new Result(false, errorMessage, null);
    }

    /// <summary>
    /// Creates a failed result from the specified exception.
    /// The exception message is used as the ErrorMessage.
    /// </summary>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <returns>A failed Result instance.</returns>
    public static Result Failure(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return new Result(false, exception.Message, exception);
    }
}

/// <summary>
/// Represents the result of an operation that returns a value.
/// Use static factory methods Success(T value) and Failure() to create instances.
/// </summary>
/// <typeparam name="T">The type of the value returned by the operation.</typeparam>
public class Result<T> : Result
{
    private readonly T? _value;

    /// <summary>
    /// Gets the value returned by the operation if successful.
    /// Accessing this property on a failed result will throw an InvalidOperationException.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if accessed when IsFailure is true.</exception>
    [MaybeNull]
    public T Value
    {
        get
        {
            if (this.IsFailure)
            {
                throw new InvalidOperationException("Cannot access the value of a failed result.");
            }

            return this._value!;
        }
    }

    /// <summary>
    /// Protected constructor to enforce usage of factory methods.
    /// </summary>
    internal protected Result(bool isSuccess, T? value, string? errorMessage, Exception? exception)
        : base(isSuccess, errorMessage, exception)
    {
        this._value = value;
    }

    /// <summary>
    /// Creates a successful result with the specified value.
    /// </summary>
    /// <param name="value">The value returned by the operation.</param>
    /// <returns>A successful Result instance containing the value.</returns>
    public static Result<T> Success(T value)
    {
        return new Result<T>(true, value, null, null);
    }

    /// <summary>
    /// Creates a failed result with the specified error message.
    /// The Value property will return the default value for type T.
    /// </summary>
    /// <param name="errorMessage">The error message describing the failure.</param>
    /// <returns>A failed Result instance.</returns>
    public new static Result<T> Failure(string errorMessage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);
        return new Result<T>(false, default, errorMessage, null);
    }

    /// <summary>
    /// Creates a failed result from the specified exception.
    /// The exception message is used as the ErrorMessage.
    /// The Value property will return the default value for type T.
    /// </summary>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <returns>A failed Result instance.</returns>
    public new static Result<T> Failure(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return new Result<T>(false, default, exception.Message, exception);
    }
}
