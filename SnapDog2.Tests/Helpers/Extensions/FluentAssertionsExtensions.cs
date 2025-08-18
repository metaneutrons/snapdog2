using System.Collections;
using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;
using SnapDog2.Core.Models;

namespace SnapDog2.Tests.Helpers.Extensions;

/// <summary>
/// Enterprise-grade FluentAssertions extensions for domain-specific assertions.
/// Provides specialized assertions for Result patterns and async collection testing.
/// </summary>
public static class FluentAssertionsExtensions
{
    /// <summary>
    /// Asserts that a Result is successful.
    /// </summary>
    /// <param name="assertions">The object assertions.</param>
    /// <param name="because">A formatted phrase as is supported by <see cref="string.Format(string,object[])" /> explaining why the assertion is needed. If the phrase does not start with the word <i>because</i>, it is prepended automatically.</param>
    /// <param name="becauseArgs">Zero or more objects to format using the placeholders in <paramref name="because" />.</param>
    /// <returns>An <see cref="AndConstraint{T}" /> which can be used to chain assertions.</returns>
    public static AndConstraint<ObjectAssertions> BeSuccessful(
        this ObjectAssertions assertions,
        string because = "",
        params object[] becauseArgs
    )
    {
        var result = assertions.Subject as Result;

        using (new AssertionScope())
        {
            result.Should().NotBeNull(because, becauseArgs);
            result!
                .IsSuccess.Should()
                .BeTrue("Expected result to be successful, but it failed with error: {0}", result.ErrorMessage);
        }

        return new AndConstraint<ObjectAssertions>(assertions);
    }

    /// <summary>
    /// Asserts that a Result&lt;T&gt; is successful and returns the value for further assertions.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <param name="assertions">The object assertions.</param>
    /// <param name="because">A formatted phrase explaining why the assertion is needed.</param>
    /// <param name="becauseArgs">Zero or more objects to format using the placeholders in <paramref name="because" />.</param>
    /// <returns>An <see cref="AndConstraint{T}" /> which can be used to chain assertions.</returns>
    public static AndConstraint<ObjectAssertions> BeSuccessful<T>(
        this ObjectAssertions assertions,
        string because = "",
        params object[] becauseArgs
    )
    {
        var result = assertions.Subject as Result<T>;

        using (new AssertionScope())
        {
            result.Should().NotBeNull(because, becauseArgs);
            result!
                .IsSuccess.Should()
                .BeTrue("Expected result to be successful, but it failed with error: {0}", result.ErrorMessage);
        }

        return new AndConstraint<ObjectAssertions>(assertions);
    }

    /// <summary>
    /// Asserts that a Result is failed.
    /// </summary>
    /// <param name="assertions">The object assertions.</param>
    /// <param name="because">A formatted phrase explaining why the assertion is needed.</param>
    /// <param name="becauseArgs">Zero or more objects to format using the placeholders in <paramref name="because" />.</param>
    /// <returns>An <see cref="AndConstraint{T}" /> which can be used to chain assertions.</returns>
    public static AndConstraint<ObjectAssertions> BeFailed(
        this ObjectAssertions assertions,
        string because = "",
        params object[] becauseArgs
    )
    {
        var result = assertions.Subject as Result;

        using (new AssertionScope())
        {
            result.Should().NotBeNull(because, becauseArgs);
            result!.IsFailure.Should().BeTrue("Expected result to be failed, but it was successful");
            result.ErrorMessage.Should().NotBeNullOrEmpty("Expected failed result to have an error message");
        }

        return new AndConstraint<ObjectAssertions>(assertions);
    }

    /// <summary>
    /// Asserts that a Result is failed with a specific error message.
    /// </summary>
    /// <param name="assertions">The object assertions.</param>
    /// <param name="expectedErrorMessage">The expected error message.</param>
    /// <param name="because">A formatted phrase explaining why the assertion is needed.</param>
    /// <param name="becauseArgs">Zero or more objects to format using the placeholders in <paramref name="because" />.</param>
    /// <returns>An <see cref="AndConstraint{T}" /> which can be used to chain assertions.</returns>
    public static AndConstraint<ObjectAssertions> BeFailedWithMessage(
        this ObjectAssertions assertions,
        string expectedErrorMessage,
        string because = "",
        params object[] becauseArgs
    )
    {
        var result = assertions.Subject as Result;

        using (new AssertionScope())
        {
            result.Should().NotBeNull(because, becauseArgs);
            result!.IsFailure.Should().BeTrue("Expected result to be failed, but it was successful");
            result.ErrorMessage.Should().Be(expectedErrorMessage, because, becauseArgs);
        }

        return new AndConstraint<ObjectAssertions>(assertions);
    }

    /// <summary>
    /// Asserts that a collection contains items matching a predicate within a specified timeout.
    /// Useful for testing async operations that populate collections over time.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    /// <param name="collection">The collection to check.</param>
    /// <param name="predicate">The predicate to match items against.</param>
    /// <param name="timeout">The maximum time to wait for matching items.</param>
    /// <param name="because">A formatted phrase explaining why the assertion is needed.</param>
    /// <param name="becauseArgs">Zero or more objects to format using the placeholders in <paramref name="because" />.</param>
    /// <returns>A task that represents the asynchronous assertion operation.</returns>
    public static async Task ContainMatchingItemsWithinAsync<T>(
        this IEnumerable<T> collection,
        Func<T, bool> predicate,
        TimeSpan timeout,
        string because = "",
        params object[] becauseArgs
    )
    {
        var startTime = DateTime.UtcNow;

        while (DateTime.UtcNow - startTime < timeout)
        {
            if (collection.Any(predicate))
            {
                return; // Success - found matching items
            }

            await Task.Delay(100); // Wait a bit before checking again
        }

        // If we get here, we timed out
        using (new AssertionScope())
        {
            collection.Should().Contain(item => predicate(item), because, becauseArgs);
        }
    }

    /// <summary>
    /// Asserts that a collection will have a specific count within a specified timeout.
    /// Useful for testing async operations that modify collection size over time.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    /// <param name="collection">The collection to check.</param>
    /// <param name="expectedCount">The expected count.</param>
    /// <param name="timeout">The maximum time to wait for the expected count.</param>
    /// <param name="because">A formatted phrase explaining why the assertion is needed.</param>
    /// <param name="becauseArgs">Zero or more objects to format using the placeholders in <paramref name="because" />.</param>
    /// <returns>A task that represents the asynchronous assertion operation.</returns>
    public static async Task HaveCountWithinAsync<T>(
        this IEnumerable<T> collection,
        int expectedCount,
        TimeSpan timeout,
        string because = "",
        params object[] becauseArgs
    )
    {
        var startTime = DateTime.UtcNow;

        while (DateTime.UtcNow - startTime < timeout)
        {
            if (collection.Count() == expectedCount)
            {
                return; // Success - found expected count
            }

            await Task.Delay(100); // Wait a bit before checking again
        }

        // If we get here, we timed out
        using (new AssertionScope())
        {
            collection.Should().HaveCount(expectedCount, because, becauseArgs);
        }
    }
}
