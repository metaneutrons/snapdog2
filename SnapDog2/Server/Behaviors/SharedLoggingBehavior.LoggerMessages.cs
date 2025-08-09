using Microsoft.Extensions.Logging;

namespace SnapDog2.Server.Behaviors;

/// <summary>
/// High-performance LoggerMessage definitions for SharedLoggingCommandBehavior.
/// Eliminates boxing, reduces allocations, and provides compile-time safety.
/// </summary>
public partial class SharedLoggingCommandBehavior<TCommand, TResponse>
{
    // Command Lifecycle Operations (9701-9703)
    [LoggerMessage(9701, LogLevel.Information, "Starting Command {CommandName}")]
    private partial void LogStartingCommand(string commandName);

    [LoggerMessage(9702, LogLevel.Information, "Completed Command {CommandName} in {ElapsedMilliseconds}ms")]
    private partial void LogCompletedCommand(string commandName, long elapsedMilliseconds);

    [LoggerMessage(9703, LogLevel.Error, "Command {CommandName} failed after {ElapsedMilliseconds}ms")]
    private partial void LogCommandFailed(Exception ex, string commandName, long elapsedMilliseconds);
}

/// <summary>
/// High-performance LoggerMessage definitions for SharedLoggingQueryBehavior.
/// Eliminates boxing, reduces allocations, and provides compile-time safety.
/// </summary>
public partial class SharedLoggingQueryBehavior<TQuery, TResponse>
{
    // Query Lifecycle Operations (9704-9706)
    [LoggerMessage(9704, LogLevel.Information, "Starting Query {QueryName}")]
    private partial void LogStartingQuery(string queryName);

    [LoggerMessage(9705, LogLevel.Information, "Completed Query {QueryName} in {ElapsedMilliseconds}ms")]
    private partial void LogCompletedQuery(string queryName, long elapsedMilliseconds);

    [LoggerMessage(9706, LogLevel.Error, "Query {QueryName} failed after {ElapsedMilliseconds}ms")]
    private partial void LogQueryFailed(Exception ex, string queryName, long elapsedMilliseconds);
}
