using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Abstractions;
using SnapDog2.Core.Configuration;

namespace SnapDog2.Services;

/// <summary>
/// Service that initializes Snapcast client grouping based on configured default zones.
/// Runs after startup to ensure clients are properly grouped by their zones.
/// </summary>
public partial class ClientZoneInitializationService : IHostedService
{
    private readonly ILogger<ClientZoneInitializationService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly SnapDogConfiguration _configuration;

    [LoggerMessage(8001, LogLevel.Information, "🎵 Starting client zone initialization")]
    private partial void LogInitializationStarted();

    [LoggerMessage(8002, LogLevel.Information, "✅ Client zone initialization completed successfully")]
    private partial void LogInitializationCompleted();

    [LoggerMessage(
        8003,
        LogLevel.Information,
        "📍 Initializing client {ClientIndex} ({ClientName}) to zone {ZoneIndex}"
    )]
    private partial void LogInitializingClient(int clientIndex, string clientName, int zoneIndex);

    [LoggerMessage(8004, LogLevel.Information, "✅ Successfully assigned client {ClientIndex} to zone {ZoneIndex}")]
    private partial void LogClientAssignedSuccessfully(int clientIndex, int zoneIndex);

    [LoggerMessage(
        8005,
        LogLevel.Warning,
        "⚠️ Failed to assign client {ClientIndex} to zone {ZoneIndex}: {ErrorMessage}"
    )]
    private partial void LogClientAssignmentFailed(int clientIndex, int zoneIndex, string errorMessage);

    [LoggerMessage(8006, LogLevel.Warning, "⚠️ Client {ClientIndex} not found in Snapcast, skipping zone assignment")]
    private partial void LogClientNotFound(int clientIndex);

    [LoggerMessage(8007, LogLevel.Error, "❌ Error during client zone initialization")]
    private partial void LogInitializationError(Exception ex);

    [LoggerMessage(8008, LogLevel.Information, "⏳ Waiting for Snapcast service to be ready...")]
    private partial void LogWaitingForSnapcastService();

    [LoggerMessage(8009, LogLevel.Information, "🔄 Snapcast service ready, proceeding with client initialization")]
    private partial void LogSnapcastServiceReady();

    [LoggerMessage(8010, LogLevel.Warning, "⏱️ Timeout waiting for Snapcast service to be ready")]
    private partial void LogSnapcastServiceTimeout();

    public ClientZoneInitializationService(
        ILogger<ClientZoneInitializationService> logger,
        IServiceProvider serviceProvider,
        SnapDogConfiguration configuration
    )
    {
        this._logger = logger;
        this._serviceProvider = serviceProvider;
        this._configuration = configuration;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        this.LogInitializationStarted();

        try
        {
            // Wait for Snapcast service to be ready
            await this.WaitForSnapcastServiceAsync(cancellationToken);

            // Initialize client zone assignments
            await this.InitializeClientZonesAsync(cancellationToken);

            this.LogInitializationCompleted();
        }
        catch (Exception ex)
        {
            this.LogInitializationError(ex);
            // Don't throw - this is not critical for application startup
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        // Nothing to clean up
        return Task.CompletedTask;
    }

    private async Task WaitForSnapcastServiceAsync(CancellationToken cancellationToken)
    {
        this.LogWaitingForSnapcastService();

        const int maxWaitTimeMs = 30000; // 30 seconds
        const int checkIntervalMs = 1000; // 1 second
        var elapsed = 0;

        while (elapsed < maxWaitTimeMs && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var scope = this._serviceProvider.CreateScope();
                var snapcastService = scope.ServiceProvider.GetRequiredService<ISnapcastService>();

                // Try to get server status to check if service is ready
                var statusResult = await snapcastService.GetServerStatusAsync(cancellationToken);
                if (statusResult.IsSuccess)
                {
                    this.LogSnapcastServiceReady();
                    return;
                }
            }
            catch
            {
                // Service not ready yet, continue waiting
            }

            await Task.Delay(checkIntervalMs, cancellationToken);
            elapsed += checkIntervalMs;
        }

        this.LogSnapcastServiceTimeout();
    }

    private async Task InitializeClientZonesAsync(CancellationToken cancellationToken)
    {
        using var scope = this._serviceProvider.CreateScope();
        var clientManager = scope.ServiceProvider.GetRequiredService<IClientManager>();

        // Process each configured client
        for (int clientIndex = 1; clientIndex <= this._configuration.Clients.Count; clientIndex++)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                var clientConfig = this._configuration.Clients[clientIndex - 1];
                var defaultZone = clientConfig.DefaultZone;

                this.LogInitializingClient(clientIndex, clientConfig.Name, defaultZone);

                // Check if client exists in Snapcast
                var clientResult = await clientManager.GetClientAsync(clientIndex);
                if (!clientResult.IsSuccess)
                {
                    this.LogClientNotFound(clientIndex);
                    continue;
                }

                // Assign client to its default zone
                var assignResult = await clientManager.AssignClientToZoneAsync(clientIndex, defaultZone);
                if (assignResult.IsSuccess)
                {
                    this.LogClientAssignedSuccessfully(clientIndex, defaultZone);
                }
                else
                {
                    this.LogClientAssignmentFailed(
                        clientIndex,
                        defaultZone,
                        assignResult.ErrorMessage ?? "Unknown error"
                    );
                }

                // Small delay between assignments to avoid overwhelming the Snapcast server
                await Task.Delay(500, cancellationToken);
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error initializing client {ClientIndex}", clientIndex);
                // Continue with next client
            }
        }
    }
}
