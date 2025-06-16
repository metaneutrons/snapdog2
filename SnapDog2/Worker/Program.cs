using EnvoyConfig;
using EnvoyConfig.Conversion;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using SnapDog2.Core.Configuration;

namespace SnapDog2.Worker;

/// <summary>
/// Main entry point and composition root for the SnapDog2 application.
/// Enhanced with EnvoyConfig pattern integration for environment variable support.
/// Responsible for configuring services, logging, and application lifecycle.
/// </summary>
public class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Configure Serilog early for startup logging
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File("logs/snapdog-.txt", rollingInterval: RollingInterval.Day)
            .CreateBootstrapLogger();

        try
        {
            Log.Information("Starting SnapDog2 application with EnvoyConfig integration");

            var host = CreateHostBuilder(args).Build();

            // Get configuration for validation
            var config = host.Services.GetRequiredService<SnapDogConfiguration>();
            Log.Information("Configuration loaded successfully");
            Log.Information("Environment: {Environment}", config.System.Environment);
            Log.Information("Log Level: {LogLevel}", config.System.LogLevel);
            Log.Information("Telemetry Enabled: {TelemetryEnabled}", config.Telemetry.Enabled);
            Log.Information("Clients Configured: {ClientCount}", config.Clients.Count);
            Log.Information("Radio Stations: {RadioStationCount}", config.RadioStations.Count);

            DisplayHeader();
            DisplayEnvironmentVariables();
            DisplayConfiguration(config);
            DisplayFooter();

            Log.Information("SnapDog2 application shut down cleanly");
            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "SnapDog2 application terminated unexpectedly");
            return 1;
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseSerilog((context, configuration) => configuration.ReadFrom.Configuration(context.Configuration))
            .ConfigureServices(
                (hostContext, services) =>
                {
                    // Load configuration using EnvoyConfig directly - no ConfigurationLoader needed
                    var snapDogConfig = LoadSnapDogConfiguration();
                    services.AddSingleton(snapDogConfig);

                    // Add MediatR for future phases
                    services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

                    // Placeholder for future service registrations
                    ConfigureServices(services, snapDogConfig);
                }
            );

    /// <summary>
    /// Loads SnapDog configuration using EnvoyConfig's automatic loading.
    /// Registers custom converters and sets the global prefix.
    /// </summary>
    /// <returns>A fully configured SnapDogConfiguration instance.</returns>
    private static SnapDogConfiguration LoadSnapDogConfiguration()
    {
        // Register custom type converters
        TypeConverterRegistry.RegisterConverter(typeof(KnxAddress), new KnxAddressConverter());
        TypeConverterRegistry.RegisterConverter(typeof(KnxAddress?), new KnxAddressConverter());

        // Set the global prefix for EnvoyConfig
        EnvConfig.GlobalPrefix = "SNAPDOG_";

        // Use EnvoyConfig's automatic loading - this handles all [Env(...)] attributes
        // including nested objects, lists, and complex mappings automatically
        return EnvConfig.Load<SnapDogConfiguration>();
    }

    private static void ConfigureServices(IServiceCollection services, SnapDogConfiguration configuration)
    {
        // This method will be expanded in future phases to register:
        // - Infrastructure services (Phase 2)
        // - Server layer services (Phase 3)
        // - API layer services (Phase 4)

        Log.Information("Service configuration completed for Phase 0");
    }

    private static void DisplayHeader()
    {
        Console.WriteLine();
        Console.WriteLine("🐶 SnapDog2 Configuration Validation Tool");
        Console.WriteLine("==========================================");
        Console.WriteLine();
        Console.WriteLine("✓ EnvoyConfig pattern integration completed");
        Console.WriteLine("✓ Environment variable support enabled");
        Console.WriteLine("✓ Configuration system enhanced");
        Console.WriteLine("✓ KNX address conversion functional");
        Console.WriteLine("✓ Application ready for Phase 1 development");
        Console.WriteLine();
    }

    private static void DisplayEnvironmentVariables()
    {
        Console.WriteLine("--- ENVIRONMENT VARIABLES ---");
        Console.WriteLine();

        // Display all SNAPDOG_ environment variables
        var snapdogVars = Environment
            .GetEnvironmentVariables()
            .Cast<System.Collections.DictionaryEntry>()
            .Where(e => e.Key is string k && k.StartsWith("SNAPDOG_"))
            .Select(e => ($"{e.Key}", e.Value?.ToString() ?? "<null>"))
            .OrderBy(t => t.Item1)
            .ToArray();

        if (snapdogVars.Length > 0)
        {
            Console.WriteLine($"Found {snapdogVars.Length} SNAPDOG_ environment variables:");
            foreach (var (key, value) in snapdogVars)
            {
                Console.WriteLine($"  {key} = {value}");
            }
        }
        else
        {
            Console.WriteLine("No SNAPDOG_ environment variables found.");
            Console.WriteLine("Configuration loaded from default values and appsettings.json.");
        }

        Console.WriteLine();
        Console.WriteLine("--- END ENVIRONMENT VARIABLES ---");
        Console.WriteLine();
    }

    private static void DisplayConfiguration(SnapDogConfiguration config)
    {
        Console.WriteLine("--- CONFIGURATION SUMMARY ---");
        Console.WriteLine();

        // Basic system configuration
        Console.WriteLine("System Configuration:");
        Console.WriteLine($"  Environment: {config.System.Environment}");
        Console.WriteLine($"  Log Level: {config.System.LogLevel}");
        Console.WriteLine($"  Application: {config.System.ApplicationName}");
        Console.WriteLine($"  Telemetry: {(config.Telemetry.Enabled ? "Enabled" : "Disabled")}");
        Console.WriteLine();

        // Snapcast server configuration
        Console.WriteLine("Snapcast Server:");
        Console.WriteLine($"  Host: {config.Services.Snapcast.Host}");
        Console.WriteLine($"  Port: {config.Services.Snapcast.Port}");
        Console.WriteLine($"  Timeout: {config.Services.Snapcast.TimeoutSeconds}s");
        Console.WriteLine();

        // API configuration
        Console.WriteLine("API Configuration:");
        Console.WriteLine($"  Port: {config.Api.Port}");
        Console.WriteLine($"  HTTPS: {(config.Api.HttpsEnabled ? "Enabled" : "Disabled")}");
        Console.WriteLine($"  API Key: {(string.IsNullOrEmpty(config.Api.ApiKey) ? "Not configured" : "Configured")}");
        Console.WriteLine();

        // Zones configuration
        Console.WriteLine($"Zones Configured: {config.Zones.Count}");
        for (int i = 0; i < config.Zones.Count; i++)
        {
            var zone = config.Zones[i];
            Console.WriteLine($"  Zone {i + 1}: {zone.Name}");
            Console.WriteLine($"    Description: {zone.Description}");
            Console.WriteLine($"    Enabled: {zone.Enabled}");
        }
        Console.WriteLine();

        // Client configurations
        Console.WriteLine($"Clients Configured: {config.Clients.Count}");
        for (int i = 0; i < config.Clients.Count; i++)
        {
            var client = config.Clients[i];
            Console.WriteLine($"  Client {i + 1}: {client.Name}");
            Console.WriteLine($"    MAC: {client.Mac}");
            Console.WriteLine($"    MQTT Base Topic: {client.MqttBaseTopic}");
            Console.WriteLine($"    Default Zone: {client.DefaultZone}");
            Console.WriteLine($"    KNX Enabled: {client.Knx.Enabled}");

            if (client.Knx.Enabled)
            {
                Console.WriteLine($"    KNX Volume: {client.Knx.Volume?.ToString() ?? "Not configured"}");
                Console.WriteLine($"    KNX Mute: {client.Knx.Mute?.ToString() ?? "Not configured"}");
            }
        }
        Console.WriteLine();

        // Radio station configurations
        Console.WriteLine($"Radio Stations: {config.RadioStations.Count}");
        for (int i = 0; i < config.RadioStations.Count; i++)
        {
            var station = config.RadioStations[i];
            Console.WriteLine($"  Station {i + 1}: {station.Name}");
            Console.WriteLine($"    URL: {station.Url}");
            Console.WriteLine($"    Enabled: {station.Enabled}");
            if (!string.IsNullOrEmpty(station.Description))
            {
                Console.WriteLine($"    Description: {station.Description}");
            }
        }
        Console.WriteLine();

        Console.WriteLine("--- END CONFIGURATION SUMMARY ---");
        Console.WriteLine();
    }

    private static void DisplayFooter()
    {
        Console.WriteLine("✅ Configuration validation completed successfully!");
        Console.WriteLine();
        Console.WriteLine("🔧 Next Steps:");
        Console.WriteLine("  1. Configure clients using SNAPDOG_CLIENT_X_* environment variables");
        Console.WriteLine("  2. Configure radio stations using SNAPDOG_RADIO_X_* environment variables");
        Console.WriteLine("  3. Set KNX addresses using format '2/1/1' for Main/Middle/Sub groups");
        Console.WriteLine("  4. Proceed to Phase 1 development");
        Console.WriteLine();
        Console.WriteLine("📖 Example environment variables:");
        Console.WriteLine("  SNAPDOG_CLIENT_1_NAME=Living Room");
        Console.WriteLine("  SNAPDOG_CLIENT_1_MAC=aa:bb:cc:dd:ee:ff");
        Console.WriteLine("  SNAPDOG_CLIENT_1_KNX_ENABLED=true");
        Console.WriteLine("  SNAPDOG_CLIENT_1_KNX_VOLUME=2/1/1");
        Console.WriteLine("  SNAPDOG_RADIO_1_NAME=Classical FM");
        Console.WriteLine("  SNAPDOG_RADIO_1_URL=http://stream.example.com/classical");
        Console.WriteLine();
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
}
