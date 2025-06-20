using EnvoyConfig;
using EnvoyConfig.Conversion;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using SnapDog2.Core.Configuration;
using SnapDog2.Core.Demo;
using SnapDog2.Core.Events;
using SnapDog2.Core.State;

// Configure Serilog early
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/snapdog-demo-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("=== SnapDog2 Phase 1 Demo Application Starting ===");

    // Create and configure the host
    using var host = CreateHostBuilder(args).Build();

    // Get the demo orchestrator
    var demoOrchestrator = host.Services.GetRequiredService<DemoOrchestrator>();

    // Run the comprehensive demo
    await demoOrchestrator.RunComprehensiveDemoAsync();

    Log.Information("=== SnapDog2 Phase 1 Demo Application Completed Successfully ===");
}
catch (Exception ex)
{
    Log.Fatal(ex, "SnapDog2 Demo Application terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

/// <summary>
/// Creates and configures the host builder with all necessary services.
/// </summary>
/// <param name="args">Command line arguments.</param>
/// <returns>Configured host builder.</returns>
static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .UseSerilog()
        .ConfigureServices(
            (context, services) =>
            {
                // Configure EnvoyConfig for configuration loading
                ConfigureEnvoyConfig();

                // Load configuration
                var configuration = LoadConfiguration();
                services.AddSingleton(configuration);

                // Register MediatR
                services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

                // Register core services
                services.AddSingleton<IEventPublisher, InMemoryEventPublisher>();
                services.AddSingleton<IStateManager, StateManager>();

                // Register demo classes
                services.AddTransient<DomainEntitiesDemo>();
                services.AddTransient<StateManagementDemo>();
                services.AddTransient<EventsDemo>();
                services.AddTransient<ValidationDemo>();
                services.AddTransient<DemoOrchestrator>();

                // Register validators (FluentValidation auto-registration could be used in real scenarios)
                RegisterValidators(services);
            }
        );

/// <summary>
/// Configures EnvoyConfig with custom type converters.
/// </summary>
static void ConfigureEnvoyConfig()
{
    // TypeConverterRegistry.RegisterConverter(typeof(KnxAddress), new KnxAddressConverter()); // Temporarily commented out due to type mismatch
    // TypeConverterRegistry.RegisterConverter(typeof(KnxAddress?), new KnxAddressConverter()); // Temporarily commented out due to type mismatch
    EnvConfig.GlobalPrefix = "SNAPDOG_";
}

/// <summary>
/// Loads the SnapDog configuration using EnvoyConfig.
/// </summary>
/// <returns>Loaded SnapDog configuration.</returns>
static SnapDogConfiguration LoadConfiguration()
{
    try
    {
        var config = EnvConfig.Load<SnapDogConfiguration>();
        Log.Information("Configuration loaded successfully");
        return config;
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Failed to load configuration from environment, using defaults");
        return new SnapDogConfiguration();
    }
}

/// <summary>
/// Registers all validators with the DI container.
/// </summary>
/// <param name="services">Service collection.</param>
static void RegisterValidators(IServiceCollection services)
{
    // Register all validators from the validation namespace
    var validatorTypes = typeof(Program)
        .Assembly.GetTypes()
        .Where(t => t.Name.EndsWith("Validator") && !t.IsAbstract && !t.IsInterface)
        .ToList();

    foreach (var validatorType in validatorTypes)
    {
        services.AddTransient(validatorType);
    }

    Log.Information("Registered {ValidatorCount} validators", validatorTypes.Count);
}

/// <summary>
/// Orchestrates the comprehensive demo of all Phase 1 capabilities.
/// </summary>
public class DemoOrchestrator
{
    private readonly ILogger<DemoOrchestrator> _logger;
    private readonly DomainEntitiesDemo _entitiesDemo;
    private readonly StateManagementDemo _stateDemo;
    private readonly EventsDemo _eventsDemo;
    private readonly ValidationDemo _validationDemo;
    private readonly SnapDogConfiguration _configuration;

    public DemoOrchestrator(
        ILogger<DemoOrchestrator> logger,
        DomainEntitiesDemo entitiesDemo,
        StateManagementDemo stateDemo,
        EventsDemo eventsDemo,
        ValidationDemo validationDemo,
        SnapDogConfiguration configuration
    )
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _entitiesDemo = entitiesDemo ?? throw new ArgumentNullException(nameof(entitiesDemo));
        _stateDemo = stateDemo ?? throw new ArgumentNullException(nameof(stateDemo));
        _eventsDemo = eventsDemo ?? throw new ArgumentNullException(nameof(eventsDemo));
        _validationDemo = validationDemo ?? throw new ArgumentNullException(nameof(validationDemo));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <summary>
    /// Runs the comprehensive demo showcasing all Phase 1 capabilities.
    /// </summary>
    public async Task RunComprehensiveDemoAsync()
    {
        _logger.LogInformation("Starting comprehensive SnapDog2 Phase 1 demonstration");

        try
        {
            // 1. Configuration Demo
            await DemonstrateConfigurationAsync();

            // 2. Domain Entities Demo
            await _entitiesDemo.RunDemoAsync();

            // 3. State Management Demo
            await _stateDemo.RunDemoAsync();

            // 4. Events Demo
            await _eventsDemo.RunDemoAsync();

            // 5. Validation Demo
            await _validationDemo.RunDemoAsync();

            // 6. Multi-threaded Operations Demo
            await DemonstrateMultiThreadedOperationsAsync();

            // 7. Performance Demo
            await DemonstratePerformanceAsync();

            // 8. Error Handling Demo
            await DemonstrateErrorHandlingAsync();

            _logger.LogInformation("Comprehensive demo completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Demo execution failed");
            throw;
        }
    }

    /// <summary>
    /// Demonstrates configuration loading and validation.
    /// </summary>
    private async Task DemonstrateConfigurationAsync()
    {
        _logger.LogInformation("=== Configuration Demo ===");

        _logger.LogInformation(
            "Application: {ApplicationName} v{Version}",
            _configuration.System.ApplicationName,
            _configuration.System.Version
        );
        _logger.LogInformation("Environment: {Environment}", _configuration.System.Environment);
        _logger.LogInformation("API Port: {Port}", _configuration.Api.Port);
        _logger.LogInformation("Telemetry Enabled: {Enabled}", _configuration.Telemetry.Enabled);
        _logger.LogInformation("Configured Zones: {ZoneCount}", _configuration.Zones.Count);
        _logger.LogInformation("Configured Clients: {ClientCount}", _configuration.Clients.Count);
        _logger.LogInformation("Configured Radio Stations: {StationCount}", _configuration.RadioStations.Count);

        await Task.Delay(1000); // Simulate async operation
        _logger.LogInformation("Configuration demo completed");
    }

    /// <summary>
    /// Demonstrates multi-threaded state operations.
    /// </summary>
    private async Task DemonstrateMultiThreadedOperationsAsync()
    {
        _logger.LogInformation("=== Multi-threaded Operations Demo ===");

        var tasks = new List<Task>();
        var cancellationTokenSource = new CancellationTokenSource();

        // Simulate concurrent state updates
        for (int i = 0; i < 5; i++)
        {
            int workerId = i;
            tasks.Add(
                Task.Run(async () =>
                {
                    await _stateDemo.SimulateConcurrentUpdatesAsync(workerId, cancellationTokenSource.Token);
                })
            );
        }

        // Let them run for a few seconds
        await Task.Delay(3000);
        cancellationTokenSource.Cancel();

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Multi-threaded operations cancelled as expected");
        }

        _logger.LogInformation("Multi-threaded operations demo completed");
    }

    /// <summary>
    /// Demonstrates performance characteristics.
    /// </summary>
    private async Task DemonstratePerformanceAsync()
    {
        _logger.LogInformation("=== Performance Demo ===");

        const int iterations = 1000;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Measure state update performance
        for (int i = 0; i < iterations; i++)
        {
            await _stateDemo.BenchmarkStateUpdatesAsync();
        }

        stopwatch.Stop();
        _logger.LogInformation(
            "Completed {Iterations} state updates in {ElapsedMs}ms ({Rate:F2} ops/sec)",
            iterations,
            stopwatch.ElapsedMilliseconds,
            iterations / stopwatch.Elapsed.TotalSeconds
        );

        await Task.Delay(500);
        _logger.LogInformation("Performance demo completed");
    }

    /// <summary>
    /// Demonstrates error handling and recovery scenarios.
    /// </summary>
    private async Task DemonstrateErrorHandlingAsync()
    {
        _logger.LogInformation("=== Error Handling Demo ===");

        try
        {
            // Demonstrate validation errors
            await _validationDemo.DemonstrateValidationErrorsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Caught expected validation error during demo");
        }

        try
        {
            // Demonstrate state management errors
            await _stateDemo.DemonstrateErrorScenariosAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Caught expected state management error during demo");
        }

        await Task.Delay(500);
        _logger.LogInformation("Error handling demo completed");
    }
}
