using System.Diagnostics.CodeAnalysis;

namespace SnapDog2.Core.Common;

/// <summary>
/// Represents the result of an operation that can either succeed or fail.
/// Provides a way to handle errors without throwing exceptions.
/// </summary>
public readonly struct Result : IEquatable<Result>
{
    private readonly bool _isSuccess;
    private readonly string? _error;

    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess => _isSuccess;

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool IsFailure => !_isSuccess;

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string? Error => _error;

    /// <summary>
    /// Initializes a new instance of the <see cref="Result"/> struct.
    /// </summary>
    /// <param name="isSuccess">Indicates whether the operation was successful.</param>
    /// <param name="error">The error message if the operation failed.</param>
    private Result(bool isSuccess, string? error)
    {
        _isSuccess = isSuccess;
        _error = error;
    }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <returns>A successful <see cref="Result"/>.</returns>
    public static Result Success() => new(true, null);

    /// <summary>
    /// Creates a failed result with an error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed <see cref="Result"/>.</returns>
    public static Result Failure(string error) => new(false, error);

    /// <summary>
    /// Creates a failed result with multiple error messages.
    /// </summary>
    /// <param name="errors">The collection of error messages.</param>
    /// <returns>A failed <see cref="Result"/>.</returns>
    public static Result Failure(IEnumerable<string> errors) => new(false, string.Join("; ", errors));

    /// <summary>
    /// Combines multiple results into a single result.
    /// </summary>
    /// <param name="results">The results to combine.</param>
    /// <returns>A successful result if all results are successful; otherwise, a failed result with combined errors.</returns>
    public static Result Combine(params Result[] results)
    {
        var failures = results.Where(static r => r.IsFailure).ToArray();
        if (failures.Length == 0)
        {
            return Success();
        }

        var errors = failures.Select(static f => f.Error).Where(static e => !string.IsNullOrEmpty(e));
        return Failure(errors!);
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current result.
    /// </summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns>True if the objects are equal; otherwise, false.</returns>
    public override bool Equals(object? obj) => obj is Result other && Equals(other);

    /// <summary>
    /// Determines whether the specified result is equal to the current result.
    /// </summary>
    /// <param name="other">The result to compare.</param>
    /// <returns>True if the results are equal; otherwise, false.</returns>
    public bool Equals(Result other) =>
        _isSuccess == other._isSuccess && string.Equals(_error, other._error, StringComparison.Ordinal);

    /// <summary>
    /// Returns the hash code for the current result.
    /// </summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode() => HashCode.Combine(_isSuccess, _error);

    /// <summary>
    /// Determines whether two results are equal.
    /// </summary>
    /// <param name="left">The first result.</param>
    /// <param name="right">The second result.</param>
    /// <returns>True if the results are equal; otherwise, false.</returns>
    public static bool operator ==(Result left, Result right) => left.Equals(right);

    /// <summary>
    /// Determines whether two results are not equal.
    /// </summary>
    /// <param name="left">The first result.</param>
    /// <param name="right">The second result.</param>
    /// <returns>True if the results are not equal; otherwise, false.</returns>
    public static bool operator !=(Result left, Result right) => !left.Equals(right);

    /// <summary>
    /// Returns a string representation of the result.
    /// </summary>
    /// <returns>A string representation of the result.</returns>
    public override string ToString() => IsSuccess ? "Success" : $"Failure: {Error}";
}

/// <summary>
/// Represents the result of an operation that can either succeed with a value or fail.
/// </summary>
/// <typeparam name="T">The type of the value returned on success.</typeparam>
public readonly struct Result<T> : IEquatable<Result<T>>
{
    private readonly bool _isSuccess;
    private readonly T? _value;
    private readonly string? _error;

    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess => _isSuccess;

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool IsFailure => !_isSuccess;

    /// <summary>
    /// Gets the value if the operation was successful.
    /// </summary>
    public T? Value => _value;

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string? Error => _error;

    /// <summary>
    /// Initializes a new instance of the <see cref="Result{T}"/> struct.
    /// </summary>
    /// <param name="isSuccess">Indicates whether the operation was successful.</param>
    /// <param name="value">The value if the operation was successful.</param>
    /// <param name="error">The error message if the operation failed.</param>
    private Result(bool isSuccess, T? value, string? error)
    {
        _isSuccess = isSuccess;
        _value = value;
        _error = error;
    }

    /// <summary>
    /// Creates a successful result with a value.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>A successful <see cref="Result{T}"/>.</returns>
    public static Result<T> Success(T value) => new(true, value, null);

    /// <summary>
    /// Creates a failed result with an error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed <see cref="Result{T}"/>.</returns>
    public static Result<T> Failure(string error) => new(false, default, error);

    /// <summary>
    /// Creates a failed result with multiple error messages.
    /// </summary>
    /// <param name="errors">The collection of error messages.</param>
    /// <returns>A failed <see cref="Result{T}"/>.</returns>
    public static Result<T> Failure(IEnumerable<string> errors) => new(false, default, string.Join("; ", errors));

    /// <summary>
    /// Converts the result to a non-generic result.
    /// </summary>
    /// <returns>A <see cref="Result"/> instance.</returns>
    public Result ToResult() => IsSuccess ? Result.Success() : Result.Failure(Error!);

    /// <summary>
    /// Maps the value to a new type if the result is successful.
    /// </summary>
    /// <typeparam name="TNew">The new type.</typeparam>
    /// <param name="mapper">The mapping function.</param>
    /// <returns>A new result with the mapped value or the original error.</returns>
    public Result<TNew> Map<TNew>(Func<T, TNew> mapper)
    {
        return IsSuccess ? Result<TNew>.Success(mapper(Value!)) : Result<TNew>.Failure(Error!);
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current result.
    /// </summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns>True if the objects are equal; otherwise, false.</returns>
    public override bool Equals(object? obj) => obj is Result<T> other && Equals(other);

    /// <summary>
    /// Determines whether the specified result is equal to the current result.
    /// </summary>
    /// <param name="other">The result to compare.</param>
    /// <returns>True if the results are equal; otherwise, false.</returns>
    public bool Equals(Result<T> other) =>
        _isSuccess == other._isSuccess
        && EqualityComparer<T>.Default.Equals(_value, other._value)
        && string.Equals(_error, other._error, StringComparison.Ordinal);

    /// <summary>
    /// Returns the hash code for the current result.
    /// </summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode() => HashCode.Combine(_isSuccess, _value, _error);

    /// <summary>
    /// Determines whether two results are equal.
    /// </summary>
    /// <param name="left">The first result.</param>
    /// <param name="right">The second result.</param>
    /// <returns>True if the results are equal; otherwise, false.</returns>
    public static bool operator ==(Result<T> left, Result<T> right) => left.Equals(right);

    /// <summary>
    /// Determines whether two results are not equal.
    /// </summary>
    /// <param name="left">The first result.</param>
    /// <param name="right">The second result.</param>
    /// <returns>True if the results are not equal; otherwise, false.</returns>
    public static bool operator !=(Result<T> left, Result<T> right) => !left.Equals(right);

    /// <summary>
    /// Returns a string representation of the result.
    /// </summary>
    /// <returns>A string representation of the result.</returns>
    public override string ToString() => IsSuccess ? $"Success: {Value}" : $"Failure: {Error}";
}
