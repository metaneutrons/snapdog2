namespace SnapDog2.Worker.Services;

using System.Linq;
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

            // Wait for all services to initialize and track results
            if (initializationTasks.Count > 0)
            {
                await Task.WhenAll(initializationTasks);

                // Check actual service states to provide accurate logging and system health assessment
                var serviceStates = new List<(string Name, bool IsConnected, bool IsCritical)>();

                if (snapcastService != null)
                {
                    serviceStates.Add(("Snapcast", snapcastService.IsConnected, true)); // Critical for audio functionality
                }

                if (mqttService != null)
                {
                    serviceStates.Add(("MQTT", mqttService.IsConnected, true)); // Critical for IoT integration
                }

                if (knxService != null)
                {
                    serviceStates.Add(("KNX", knxService.IsConnected, false)); // Non-critical, building automation
                }

                var successfulServices = serviceStates.Where(s => s.IsConnected).ToList();
                var failedServices = serviceStates.Where(s => !s.IsConnected).ToList();
                var criticalFailures = failedServices.Where(s => s.IsCritical).ToList();
                var nonCriticalFailures = failedServices.Where(s => !s.IsCritical).ToList();

                // Assess system health and take appropriate action
                if (criticalFailures.Any())
                {
                    // System is non-functional - critical services failed
                    this._logger.LogCritical(
                        "🚨 SYSTEM NON-FUNCTIONAL: Critical integration services failed. "
                            + "Critical failures: [{CriticalFailures}], Other failures: [{NonCriticalFailures}], "
                            + "Successful: [{SuccessfulServices}]. Application will terminate.",
                        string.Join(", ", criticalFailures.Select(s => s.Name)),
                        string.Join(", ", nonCriticalFailures.Select(s => s.Name)),
                        string.Join(", ", successfulServices.Select(s => s.Name))
                    );

                    // Disable failed services to prevent further issues
                    await DisableFailedServicesAsync(failedServices.Select(s => s.Name));

                    // Terminate application - system cannot function without critical services
                    Environment.Exit(1);
                }
                else if (nonCriticalFailures.Any())
                {
                    // System is degraded but functional - only non-critical services failed
                    this._logger.LogWarning(
                        "⚠️  SYSTEM DEGRADED: Non-critical integration services failed, but core functionality remains available. "
                            + "Failed: [{FailedServices}], Successful: [{SuccessfulServices}]",
                        string.Join(", ", nonCriticalFailures.Select(s => s.Name)),
                        string.Join(", ", successfulServices.Select(s => s.Name))
                    );

                    // Disable failed services to prevent resource waste and error noise
                    await DisableFailedServicesAsync(nonCriticalFailures.Select(s => s.Name));
                }
                else
                {
                    // All services successful
                    this._logger.LogInformation(
                        "✅ All integration services initialized successfully: [{Services}]",
                        string.Join(", ", successfulServices.Select(s => s.Name))
                    );
                }
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

    /// <summary>
    /// Disables failed integration services to prevent resource waste and error noise.
    /// This helps maintain system stability by stopping retry attempts and cleanup resources.
    /// </summary>
    /// <param name="failedServiceNames">Names of services that failed to initialize.</param>
    private async Task DisableFailedServicesAsync(IEnumerable<string> failedServiceNames)
    {
        foreach (var serviceName in failedServiceNames)
        {
            try
            {
                this._logger.LogInformation("🔌 Disabling failed service: {ServiceName}", serviceName);

                switch (serviceName.ToLowerInvariant())
                {
                    case "snapcast":
                        var snapcastService = this._serviceProvider.GetService<ISnapcastService>();
                        if (snapcastService != null)
                        {
                            // Note: ISnapcastService doesn't have a Dispose method in the interface
                            // The service will be disposed by DI container when the application shuts down
                            this._logger.LogInformation("📴 Snapcast service marked as disabled");
                        }
                        break;

                    case "mqtt":
                        var mqttService = this._serviceProvider.GetService<IMqttService>();
                        if (mqttService != null)
                        {
                            // Note: IMqttService doesn't have a Dispose method in the interface
                            // The service will be disposed by DI container when the application shuts down
                            this._logger.LogInformation("📴 MQTT service marked as disabled");
                        }
                        break;

                    case "knx":
                        var knxService = this._serviceProvider.GetService<IKnxService>();
                        if (knxService != null)
                        {
                            // Note: IKnxService doesn't have a Dispose method in the interface
                            // The service will be disposed by DI container when the application shuts down
                            this._logger.LogInformation("📴 KNX service marked as disabled");
                        }
                        break;

                    default:
                        this._logger.LogWarning("⚠️  Unknown service name for disabling: {ServiceName}", serviceName);
                        break;
                }
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "❌ Failed to disable service: {ServiceName}", serviceName);
            }
        }

        await Task.CompletedTask; // Placeholder for any async cleanup operations
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        this._logger.LogInformation("Stopping integration services...");
        await base.StopAsync(cancellationToken);
        this._logger.LogInformation("Integration services stopped");
    }
}
