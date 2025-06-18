using System.Transactions;
using MediatR;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Common;

namespace SnapDog2.Server.Behaviors;

/// <summary>
/// Pipeline behavior that provides database transaction management for commands.
/// Ensures that command operations are executed within a transaction scope for data consistency.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public sealed class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public TransactionBehavior(ILogger<TransactionBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles the transaction management pipeline.
    /// </summary>
    /// <param name="request">The request to execute within a transaction.</param>
    /// <param name="next">The next behavior in the pipeline.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The response with transaction management applied.</returns>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        var requestName = typeof(TRequest).Name;

        // Only apply transaction management to commands that require it
        if (!RequiresTransaction(request))
        {
            _logger.LogTrace("No transaction required for request: {RequestName}", requestName);
            return await next();
        }

        // Check if request explicitly requires a specific transaction isolation level
        var isolationLevel = GetTransactionIsolationLevel(request);

        _logger.LogDebug(
            "Starting transaction for request: {RequestName} with isolation level: {IsolationLevel}",
            requestName,
            isolationLevel
        );

        var transactionOptions = new TransactionOptions
        {
            IsolationLevel = isolationLevel,
            Timeout = GetTransactionTimeout(request),
        };

        using var transactionScope = new TransactionScope(
            TransactionScopeOption.Required,
            transactionOptions,
            TransactionScopeAsyncFlowOption.Enabled
        );

        try
        {
            var response = await next();

            // Check if the response indicates success before committing
            if (IsSuccessfulResponse(response))
            {
                transactionScope.Complete();

                _logger.LogDebug("Transaction committed successfully for request: {RequestName}", requestName);
            }
            else
            {
                _logger.LogWarning(
                    "Transaction not committed due to unsuccessful response for request: {RequestName}",
                    requestName
                );
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Transaction failed for request: {RequestName}. Transaction will be rolled back.",
                requestName
            );

            // Transaction will be automatically rolled back when the scope is disposed
            throw;
        }
    }

    /// <summary>
    /// Determines if a request requires transaction management.
    /// </summary>
    /// <param name="request">The request to check.</param>
    /// <returns>True if a transaction is required; otherwise, false.</returns>
    private static bool RequiresTransaction(TRequest request)
    {
        // Check if the request explicitly requires a transaction
        if (request is IRequireTransaction)
        {
            return true;
        }

        // Check if the request explicitly excludes transactions
        if (request is IExcludeFromTransaction)
        {
            return false;
        }

        // By default, commands require transactions, queries do not
        return IsCommandRequest(request);
    }

    /// <summary>
    /// Determines if the request is a command that modifies data.
    /// </summary>
    /// <param name="request">The request to check.</param>
    /// <returns>True if it's a command request; otherwise, false.</returns>
    private static bool IsCommandRequest(TRequest request)
    {
        var requestName = typeof(TRequest).Name;
        return requestName.Contains("Command", StringComparison.OrdinalIgnoreCase)
            || requestName.StartsWith("Create", StringComparison.OrdinalIgnoreCase)
            || requestName.StartsWith("Update", StringComparison.OrdinalIgnoreCase)
            || requestName.StartsWith("Delete", StringComparison.OrdinalIgnoreCase)
            || requestName.StartsWith("Start", StringComparison.OrdinalIgnoreCase)
            || requestName.StartsWith("Stop", StringComparison.OrdinalIgnoreCase)
            || requestName.StartsWith("Add", StringComparison.OrdinalIgnoreCase)
            || requestName.StartsWith("Remove", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the appropriate transaction isolation level for the request.
    /// </summary>
    /// <param name="request">The request to get isolation level for.</param>
    /// <returns>The transaction isolation level.</returns>
    private static IsolationLevel GetTransactionIsolationLevel(TRequest request)
    {
        // Check if the request specifies a custom isolation level
        if (request is ICustomTransactionIsolation customIsolation)
        {
            return customIsolation.GetIsolationLevel();
        }

        // Default isolation levels based on request type
        var requestName = typeof(TRequest).Name.ToLowerInvariant();

        return requestName switch
        {
            // Critical operations that require strict consistency
            var name when name.Contains("delete") => IsolationLevel.Serializable,
            var name when name.Contains("payment") => IsolationLevel.Serializable,
            var name when name.Contains("transfer") => IsolationLevel.Serializable,

            // Operations that might read their own writes
            var name when name.Contains("create") => IsolationLevel.ReadCommitted,
            var name when name.Contains("update") => IsolationLevel.ReadCommitted,
            var name when name.Contains("start") => IsolationLevel.ReadCommitted,
            var name when name.Contains("stop") => IsolationLevel.ReadCommitted,

            // Default for most operations
            _ => IsolationLevel.ReadCommitted,
        };
    }

    /// <summary>
    /// Gets the transaction timeout for the request.
    /// </summary>
    /// <param name="request">The request to get timeout for.</param>
    /// <returns>The transaction timeout.</returns>
    private static TimeSpan GetTransactionTimeout(TRequest request)
    {
        // Check if the request specifies a custom timeout
        if (request is ICustomTransactionTimeout customTimeout)
        {
            return customTimeout.GetTransactionTimeout();
        }

        // Default timeouts based on request type
        var requestName = typeof(TRequest).Name.ToLowerInvariant();

        return requestName switch
        {
            // Long-running operations
            var name when name.Contains("import") => TimeSpan.FromMinutes(10),
            var name when name.Contains("export") => TimeSpan.FromMinutes(5),
            var name when name.Contains("process") => TimeSpan.FromMinutes(3),

            // Bulk operations
            var name when name.Contains("bulk") => TimeSpan.FromMinutes(2),
            var name when name.Contains("batch") => TimeSpan.FromMinutes(2),

            // Standard operations
            _ => TimeSpan.FromMinutes(1),
        };
    }

    /// <summary>
    /// Determines if a response indicates a successful operation.
    /// </summary>
    /// <param name="response">The response to check.</param>
    /// <returns>True if the response indicates success; otherwise, false.</returns>
    private static bool IsSuccessfulResponse(TResponse response)
    {
        return response switch
        {
            Result result => result.IsSuccess,
            _
                when typeof(TResponse).IsGenericType
                    && typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>) => (bool)(
                typeof(TResponse).GetProperty("IsSuccess")?.GetValue(response) ?? false
            ),
            null => false,
            _ => true, // Non-Result types are considered successful if they return a value
        };
    }
}

/// <summary>
/// Marker interface for requests that explicitly require transaction management.
/// </summary>
public interface IRequireTransaction { }

/// <summary>
/// Marker interface for requests that should be excluded from automatic transaction management.
/// Use this for operations that manage their own transactions or don't require them.
/// </summary>
public interface IExcludeFromTransaction { }

/// <summary>
/// Interface for requests that require a specific transaction isolation level.
/// </summary>
public interface ICustomTransactionIsolation
{
    /// <summary>
    /// Gets the required transaction isolation level.
    /// </summary>
    /// <returns>The transaction isolation level.</returns>
    IsolationLevel GetIsolationLevel();
}

/// <summary>
/// Interface for requests that require a custom transaction timeout.
/// </summary>
public interface ICustomTransactionTimeout
{
    /// <summary>
    /// Gets the required transaction timeout.
    /// </summary>
    /// <returns>The transaction timeout.</returns>
    TimeSpan GetTransactionTimeout();
}

/// <summary>
/// Base interface for requests that need full transaction control.
/// </summary>
public interface ITransactionControlled : ICustomTransactionIsolation, ICustomTransactionTimeout { }
