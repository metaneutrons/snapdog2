namespace SnapDog2.Worker.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Abstractions;

/// <summary>
/// Hosted service responsible for initializing integration services (Snapcast, MQTT, KNX, etc.) on application startup.
/// This ensures that all external integrations are properly connected and ready before the application starts serving requests.
/// </summary>
public class IntegrationServicesHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<IntegrationServicesHostedService> _logger;

    public IntegrationServicesHostedService(
        IServiceProvider serviceProvider,
        ILogger<IntegrationServicesHostedService> logger
    )
    {
        this._serviceProvider = serviceProvider;
        this._logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        this._logger.LogInformation("Starting integration services initialization...");

        try
        {
            // Initialize services in parallel for faster startup
            var initializationTasks = new List<Task>();

            // Initialize Snapcast service if available
            var snapcastService = this._serviceProvider.GetService<ISnapcastService>();
            if (snapcastService != null)
            {
                this._logger.LogInformation("Initializing Snapcast service...");
                initializationTasks.Add(this.InitializeSnapcastServiceAsync(snapcastService, stoppingToken));
            }
            else
            {
                this._logger.LogWarning("Snapcast service not registered - skipping initialization");
            }

            // Initialize MQTT service if available
            var mqttService = this._serviceProvider.GetService<IMqttService>();
            if (mqttService != null)
            {
                this._logger.LogInformation("Initializing MQTT service...");
                initializationTasks.Add(this.InitializeMqttServiceAsync(mqttService, stoppingToken));
            }
            else
            {
                this._logger.LogWarning("MQTT service not registered - skipping initialization");
            }

            // Initialize KNX service if available
            var knxService = this._serviceProvider.GetService<IKnxService>();
            if (knxService != null)
            {
                this._logger.LogInformation("Initializing KNX service...");
                initializationTasks.Add(this.InitializeKnxServiceAsync(knxService, stoppingToken));
            }
            else
            {
                this._logger.LogWarning("KNX service not registered - skipping initialization");
            }

            // Wait for all services to initialize
            if (initializationTasks.Count > 0)
            {
                await Task.WhenAll(initializationTasks);
                this._logger.LogInformation("All integration services initialized successfully");
            }
            else
            {
                this._logger.LogWarning("No integration services found to initialize");
            }

            // Keep the service running to maintain connections
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            this._logger.LogInformation("Integration services initialization cancelled");
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Failed to initialize integration services");
            throw; // Re-throw to stop the application if critical services fail
        }
    }

    private async Task InitializeSnapcastServiceAsync(
        ISnapcastService snapcastService,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var result = await snapcastService.InitializeAsync(cancellationToken);
            if (result.IsSuccess)
            {
                this._logger.LogInformation("✅ Snapcast service initialized successfully");
            }
            else
            {
                this._logger.LogError("❌ Failed to initialize Snapcast service: {ErrorMessage}", result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "❌ Exception during Snapcast service initialization");
        }
    }

    private async Task InitializeMqttServiceAsync(IMqttService mqttService, CancellationToken cancellationToken)
    {
        try
        {
            var result = await mqttService.InitializeAsync(cancellationToken);
            if (result.IsSuccess)
            {
                this._logger.LogInformation(
                    "✅ MQTT service initialized successfully - Connected: {IsConnected}",
                    mqttService.IsConnected
                );
            }
            else
            {
                this._logger.LogError("❌ Failed to initialize MQTT service: {ErrorMessage}", result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "❌ Exception during MQTT service initialization");
        }
    }

    private async Task InitializeKnxServiceAsync(IKnxService knxService, CancellationToken cancellationToken)
    {
        try
        {
            var result = await knxService.InitializeAsync(cancellationToken);
            if (result.IsSuccess)
            {
                this._logger.LogInformation(
                    "✅ KNX service initialized successfully - Connected: {IsConnected}",
                    knxService.IsConnected
                );
            }
            else
            {
                this._logger.LogError("❌ Failed to initialize KNX service: {ErrorMessage}", result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "❌ Exception during KNX service initialization");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        this._logger.LogInformation("Stopping integration services...");
        await base.StopAsync(cancellationToken);
        this._logger.LogInformation("Integration services stopped");
    }
}
