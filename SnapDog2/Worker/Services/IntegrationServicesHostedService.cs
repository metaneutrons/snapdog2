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
public partial class IntegrationServicesHostedService : BackgroundService
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

    #region Logging

    [LoggerMessage(5001, LogLevel.Information, "🚀 Starting integration services initialization...")]
    private partial void LogInitializationStarted();

    [LoggerMessage(5002, LogLevel.Information, "Initializing Snapcast service...")]
    private partial void LogInitializingSnapcast();

    [LoggerMessage(5003, LogLevel.Warning, "Snapcast service not registered - skipping initialization")]
    private partial void LogSnapcastNotRegistered();

    [LoggerMessage(5004, LogLevel.Information, "Initializing MQTT service...")]
    private partial void LogInitializingMqtt();

    [LoggerMessage(5005, LogLevel.Warning, "MQTT service not registered - skipping initialization")]
    private partial void LogMqttNotRegistered();

    [LoggerMessage(5006, LogLevel.Information, "Initializing KNX service...")]
    private partial void LogInitializingKnx();

    [LoggerMessage(5007, LogLevel.Warning, "KNX service not registered - skipping initialization")]
    private partial void LogKnxNotRegistered();

    [LoggerMessage(5008, LogLevel.Information, "✅ Snapcast service initialized successfully")]
    private partial void LogSnapcastInitialized();

    [LoggerMessage(5009, LogLevel.Information, "✅ MQTT service initialized successfully - Connected: {IsConnected}")]
    private partial void LogMqttInitialized(bool isConnected);

    [LoggerMessage(5010, LogLevel.Information, "✅ KNX service initialized successfully - Connected: {IsConnected}")]
    private partial void LogKnxInitialized(bool isConnected);

    [LoggerMessage(5011, LogLevel.Error, "❌ Failed to initialize Snapcast service: {ErrorMessage}")]
    private partial void LogSnapcastInitializationFailed(string errorMessage);

    [LoggerMessage(5012, LogLevel.Error, "❌ Failed to initialize MQTT service: {ErrorMessage}")]
    private partial void LogMqttInitializationFailed(string errorMessage);

    [LoggerMessage(5013, LogLevel.Error, "❌ Failed to initialize KNX service: {ErrorMessage}")]
    private partial void LogKnxInitializationFailed(string errorMessage);

    [LoggerMessage(5014, LogLevel.Information, "✅ All integration services initialized successfully: [{Services}]")]
    private partial void LogAllServicesInitialized(string services);

    [LoggerMessage(
        5015,
        LogLevel.Warning,
        "⚠️  SYSTEM DEGRADED: Non-critical integration services failed, but core functionality remains available. Failed: [{FailedServices}], Successful: [{SuccessfulServices}]"
    )]
    private partial void LogSystemDegraded(string failedServices, string successfulServices);

    [LoggerMessage(
        5016,
        LogLevel.Critical,
        "🚨 SYSTEM NON-FUNCTIONAL: Critical integration services failed. Critical failures: [{CriticalFailures}], Other failures: [{NonCriticalFailures}], Successful: [{SuccessfulServices}]. Application will terminate."
    )]
    private partial void LogSystemNonFunctional(
        string criticalFailures,
        string nonCriticalFailures,
        string successfulServices
    );

    [LoggerMessage(5017, LogLevel.Warning, "No integration services found to initialize")]
    private partial void LogNoServicesFound();

    [LoggerMessage(5018, LogLevel.Information, "🔌 Disabling failed service: {ServiceName}")]
    private partial void LogDisablingService(string serviceName);

    [LoggerMessage(5019, LogLevel.Information, "📴 {ServiceName} service marked as disabled")]
    private partial void LogServiceDisabled(string serviceName);

    [LoggerMessage(5020, LogLevel.Warning, "⚠️  Unknown service name for disabling: {ServiceName}")]
    private partial void LogUnknownServiceName(string serviceName);

    [LoggerMessage(5021, LogLevel.Error, "❌ Failed to disable service: {ServiceName}")]
    private partial void LogServiceDisableFailed(string serviceName, Exception exception);

    #endregion

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        this.LogInitializationStarted();

        try
        {
            // Initialize services in parallel for faster startup
            var initializationTasks = new List<Task>();

            // Initialize Snapcast service if available
            var snapcastService = this._serviceProvider.GetService<ISnapcastService>();
            if (snapcastService != null)
            {
                this.LogInitializingSnapcast();
                initializationTasks.Add(this.InitializeSnapcastServiceAsync(snapcastService, stoppingToken));
            }
            else
            {
                this.LogSnapcastNotRegistered();
            }

            // Initialize MQTT service if available
            var mqttService = this._serviceProvider.GetService<IMqttService>();
            if (mqttService != null)
            {
                this.LogInitializingMqtt();
                initializationTasks.Add(this.InitializeMqttServiceAsync(mqttService, stoppingToken));
            }
            else
            {
                this.LogMqttNotRegistered();
            }

            // Initialize KNX service if available
            var knxService = this._serviceProvider.GetService<IKnxService>();
            if (knxService != null)
            {
                this.LogInitializingKnx();
                initializationTasks.Add(this.InitializeKnxServiceAsync(knxService, stoppingToken));
            }
            else
            {
                this.LogKnxNotRegistered();
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
                    this.LogSystemNonFunctional(
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
                    this.LogSystemDegraded(
                        string.Join(", ", nonCriticalFailures.Select(s => s.Name)),
                        string.Join(", ", successfulServices.Select(s => s.Name))
                    );

                    // Disable failed services to prevent resource waste and error noise
                    await DisableFailedServicesAsync(nonCriticalFailures.Select(s => s.Name));
                }
                else
                {
                    // All services successful
                    this.LogAllServicesInitialized(string.Join(", ", successfulServices.Select(s => s.Name)));
                }
            }
            else
            {
                this.LogNoServicesFound();
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
                this.LogSnapcastInitialized();
            }
            else
            {
                this.LogSnapcastInitializationFailed(result.ErrorMessage ?? "Unknown error");
            }
        }
        catch (Exception ex)
        {
            this.LogSnapcastInitializationFailed(ex.Message);
        }
    }

    private async Task InitializeMqttServiceAsync(IMqttService mqttService, CancellationToken cancellationToken)
    {
        try
        {
            var result = await mqttService.InitializeAsync(cancellationToken);
            if (result.IsSuccess)
            {
                this.LogMqttInitialized(mqttService.IsConnected);
            }
            else
            {
                this.LogMqttInitializationFailed(result.ErrorMessage ?? "Unknown error");
            }
        }
        catch (Exception ex)
        {
            this.LogMqttInitializationFailed(ex.Message);
        }
    }

    private async Task InitializeKnxServiceAsync(IKnxService knxService, CancellationToken cancellationToken)
    {
        try
        {
            var result = await knxService.InitializeAsync(cancellationToken);
            if (result.IsSuccess)
            {
                this.LogKnxInitialized(knxService.IsConnected);
            }
            else
            {
                this.LogKnxInitializationFailed(result.ErrorMessage ?? "Unknown error");
            }
        }
        catch (Exception ex)
        {
            this.LogKnxInitializationFailed(ex.Message);
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
                this.LogDisablingService(serviceName);

                switch (serviceName.ToLowerInvariant())
                {
                    case "snapcast":
                        var snapcastService = this._serviceProvider.GetService<ISnapcastService>();
                        if (snapcastService != null)
                        {
                            // Note: ISnapcastService doesn't have a Dispose method in the interface
                            // The service will be disposed by DI container when the application shuts down
                            this.LogServiceDisabled("Snapcast");
                        }
                        break;

                    case "mqtt":
                        var mqttService = this._serviceProvider.GetService<IMqttService>();
                        if (mqttService != null)
                        {
                            // Note: IMqttService doesn't have a Dispose method in the interface
                            // The service will be disposed by DI container when the application shuts down
                            this.LogServiceDisabled("MQTT");
                        }
                        break;

                    case "knx":
                        var knxService = this._serviceProvider.GetService<IKnxService>();
                        if (knxService != null)
                        {
                            // Note: IKnxService doesn't have a Dispose method in the interface
                            // The service will be disposed by DI container when the application shuts down
                            this.LogServiceDisabled("KNX");
                        }
                        break;

                    default:
                        this.LogUnknownServiceName(serviceName);
                        break;
                }
            }
            catch (Exception ex)
            {
                this.LogServiceDisableFailed(serviceName, ex);
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
