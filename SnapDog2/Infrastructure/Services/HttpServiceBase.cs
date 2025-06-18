using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Polly;

namespace SnapDog2.Infrastructure.Services;

/// <summary>
/// Abstract base class for HTTP-based external services with integrated resilience patterns.
/// Provides standardized HTTP operations with Polly policies, structured logging, and proper resource disposal.
/// </summary>
public abstract class HttpServiceBase : IDisposable, IAsyncDisposable
{
    private readonly HttpClient _httpClient;
    private readonly IAsyncPolicy _resiliencePolicy;
    private readonly ILogger _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the HttpServiceBase class.
    /// </summary>
    /// <param name="httpClient">HTTP client for making requests</param>
    /// <param name="resilienceStrategy">Polly resilience strategy for fault tolerance</param>
    /// <param name="logger">Logger for structured logging</param>
    protected HttpServiceBase(HttpClient httpClient, IAsyncPolicy resiliencePolicy, ILogger logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _resiliencePolicy = resiliencePolicy ?? throw new ArgumentNullException(nameof(resiliencePolicy));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
        };
    }

    /// <summary>
    /// Gets the service name for logging and monitoring purposes.
    /// </summary>
    protected abstract string ServiceName { get; }

    /// <summary>
    /// Performs a GET request with resilience policies.
    /// </summary>
    /// <param name="endpoint">The endpoint to request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>HTTP response message</returns>
    protected async Task<HttpResponseMessage> GetAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        return await ExecuteWithResilienceAsync(
            async () =>
            {
                _logger.LogDebug("Executing GET request to {ServiceName} endpoint: {Endpoint}", ServiceName, endpoint);

                var response = await _httpClient.GetAsync(endpoint, cancellationToken);

                _logger.LogDebug(
                    "GET request to {ServiceName} endpoint {Endpoint} completed with status: {StatusCode}",
                    ServiceName,
                    endpoint,
                    response.StatusCode
                );

                return response;
            },
            $"GET {endpoint}",
            cancellationToken
        );
    }

    /// <summary>
    /// Performs a GET request and deserializes the response to the specified type.
    /// </summary>
    /// <typeparam name="T">Type to deserialize the response to</typeparam>
    /// <param name="endpoint">The endpoint to request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Deserialized response object</returns>
    protected async Task<T?> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default)
    {
        using var response = await GetAsync(endpoint, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning(
                "GET request to {ServiceName} endpoint {Endpoint} failed with status {StatusCode}: {ErrorContent}",
                ServiceName,
                endpoint,
                response.StatusCode,
                errorContent
            );

            response.EnsureSuccessStatusCode();
        }

        var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(jsonContent))
        {
            _logger.LogWarning(
                "Empty response received from {ServiceName} endpoint: {Endpoint}",
                ServiceName,
                endpoint
            );
            return default;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(jsonContent, _jsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogError(
                ex,
                "Failed to deserialize response from {ServiceName} endpoint {Endpoint}. Content: {Content}",
                ServiceName,
                endpoint,
                jsonContent
            );
            throw;
        }
    }

    /// <summary>
    /// Performs a POST request with JSON payload.
    /// </summary>
    /// <param name="endpoint">The endpoint to request</param>
    /// <param name="payload">Object to serialize as JSON payload</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>HTTP response message</returns>
    protected async Task<HttpResponseMessage> PostAsync<T>(
        string endpoint,
        T payload,
        CancellationToken cancellationToken = default
    )
    {
        ThrowIfDisposed();

        return await ExecuteWithResilienceAsync(
            async () =>
            {
                _logger.LogDebug("Executing POST request to {ServiceName} endpoint: {Endpoint}", ServiceName, endpoint);

                var jsonPayload = JsonSerializer.Serialize(payload, _jsonOptions);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);

                _logger.LogDebug(
                    "POST request to {ServiceName} endpoint {Endpoint} completed with status: {StatusCode}",
                    ServiceName,
                    endpoint,
                    response.StatusCode
                );

                return response;
            },
            $"POST {endpoint}",
            cancellationToken
        );
    }

    /// <summary>
    /// Performs a PUT request with JSON payload.
    /// </summary>
    /// <param name="endpoint">The endpoint to request</param>
    /// <param name="payload">Object to serialize as JSON payload</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>HTTP response message</returns>
    protected async Task<HttpResponseMessage> PutAsync<T>(
        string endpoint,
        T payload,
        CancellationToken cancellationToken = default
    )
    {
        ThrowIfDisposed();

        return await ExecuteWithResilienceAsync(
            async () =>
            {
                _logger.LogDebug("Executing PUT request to {ServiceName} endpoint: {Endpoint}", ServiceName, endpoint);

                var jsonPayload = JsonSerializer.Serialize(payload, _jsonOptions);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync(endpoint, content, cancellationToken);

                _logger.LogDebug(
                    "PUT request to {ServiceName} endpoint {Endpoint} completed with status: {StatusCode}",
                    ServiceName,
                    endpoint,
                    response.StatusCode
                );

                return response;
            },
            $"PUT {endpoint}",
            cancellationToken
        );
    }

    /// <summary>
    /// Performs a DELETE request.
    /// </summary>
    /// <param name="endpoint">The endpoint to request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>HTTP response message</returns>
    protected async Task<HttpResponseMessage> DeleteAsync(
        string endpoint,
        CancellationToken cancellationToken = default
    )
    {
        ThrowIfDisposed();

        return await ExecuteWithResilienceAsync(
            async () =>
            {
                _logger.LogDebug(
                    "Executing DELETE request to {ServiceName} endpoint: {Endpoint}",
                    ServiceName,
                    endpoint
                );

                var response = await _httpClient.DeleteAsync(endpoint, cancellationToken);

                _logger.LogDebug(
                    "DELETE request to {ServiceName} endpoint {Endpoint} completed with status: {StatusCode}",
                    ServiceName,
                    endpoint,
                    response.StatusCode
                );

                return response;
            },
            $"DELETE {endpoint}",
            cancellationToken
        );
    }

    /// <summary>
    /// Checks if the service is available by attempting a health check endpoint.
    /// </summary>
    /// <param name="healthCheckEndpoint">Health check endpoint (default: "/health")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if service is available, false otherwise</returns>
    protected async Task<bool> IsServiceAvailableAsync(
        string healthCheckEndpoint = "/health",
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            using var response = await GetAsync(healthCheckEndpoint, cancellationToken);
            var isAvailable = response.IsSuccessStatusCode;

            _logger.LogDebug(
                "{ServiceName} availability check: {IsAvailable} (Status: {StatusCode})",
                ServiceName,
                isAvailable,
                response.StatusCode
            );

            return isAvailable;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "{ServiceName} availability check failed", ServiceName);
            return false;
        }
    }

    /// <summary>
    /// Executes an operation with the configured resilience strategy.
    /// </summary>
    /// <typeparam name="T">Return type of the operation</typeparam>
    /// <param name="operation">Operation to execute</param>
    /// <param name="operationName">Name of the operation for logging</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the operation</returns>
    private async Task<T> ExecuteWithResilienceAsync<T>(
        Func<Task<T>> operation,
        string operationName,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            _logger.LogDebug("Executing {ServiceName} operation: {OperationName}", ServiceName, operationName);

            var result = await _resiliencePolicy.ExecuteAsync(async () => await operation());

            _logger.LogDebug(
                "{ServiceName} operation {OperationName} completed successfully",
                ServiceName,
                operationName
            );

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "{ServiceName} operation {OperationName} failed after resilience policies",
                ServiceName,
                operationName
            );
            throw;
        }
    }

    /// <summary>
    /// Throws ObjectDisposedException if the service has been disposed.
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(GetType().Name);
        }
    }

    /// <summary>
    /// Disposes of the HTTP service resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Asynchronously disposes of the HTTP service resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        Dispose(false);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes of the HTTP service resources.
    /// </summary>
    /// <param name="disposing">True if disposing, false if finalizing</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _httpClient?.Dispose();
            _disposed = true;
            _logger.LogDebug("{ServiceName} HTTP service disposed", ServiceName);
        }
    }

    /// <summary>
    /// Core async disposal logic.
    /// </summary>
    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (!_disposed)
        {
            if (_httpClient is IAsyncDisposable asyncDisposableClient)
            {
                await asyncDisposableClient.DisposeAsync();
            }
            else
            {
                _httpClient?.Dispose();
            }

            _disposed = true;
            _logger.LogDebug("{ServiceName} HTTP service disposed asynchronously", ServiceName);
        }
    }
}
