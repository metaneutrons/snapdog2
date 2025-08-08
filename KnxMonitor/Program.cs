using System.CommandLine;
using System.CommandLine.Parsing;
using KnxMonitor.Models;
using KnxMonitor.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace KnxMonitor;

/// <summary>
/// Main program entry point for the KNX Monitor application.
/// </summary>
public static class Program
{
    /// <summary>
    /// Main entry point for the application.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>Exit code.</returns>
    public static async Task<int> Main(string[] args)
    {
        // Create options using modern System.CommandLine pattern
        Option<string> gatewayOption = new("--gateway", "-g")
        {
            Description = "KNX gateway address (required for tunnel/router connections)",
            Required = true,
        };

        Option<string> connectionTypeOption = new("--connection-type", "-c")
        {
            Description = "Connection type: tunnel, router, usb",
            DefaultValueFactory = _ => "tunnel",
        };
        connectionTypeOption.AcceptOnlyFromAmong("tunnel", "router", "usb");

        Option<int> portOption = new("--port", "-p") { Description = "Gateway port", DefaultValueFactory = _ => 3671 };

        Option<bool> verboseOption = new("--verbose", "-v") { Description = "Enable verbose logging" };

        Option<string?> filterOption = new("--filter", "-f") { Description = "Group address filter pattern" };

        // Create root command using modern pattern
        RootCommand rootCommand = new("KNX Monitor - Visual debugging tool for KNX/EIB bus activity");
        rootCommand.Options.Add(gatewayOption);
        rootCommand.Options.Add(connectionTypeOption);
        rootCommand.Options.Add(portOption);
        rootCommand.Options.Add(verboseOption);
        rootCommand.Options.Add(filterOption);

        // Parse and handle the command
        ParseResult parseResult = rootCommand.Parse(args);

        // Check for parsing errors
        if (parseResult.Errors.Count > 0)
        {
            foreach (ParseError parseError in parseResult.Errors)
            {
                Console.Error.WriteLine(parseError.Message);
            }
            return 1;
        }

        // Extract parsed values
        string gateway = parseResult.GetValue(gatewayOption)!;
        string connectionType = parseResult.GetValue(connectionTypeOption)!;
        int port = parseResult.GetValue(portOption);
        bool verbose = parseResult.GetValue(verboseOption);
        string? filter = parseResult.GetValue(filterOption);

        // Run the monitor
        return await RunMonitorAsync(gateway, connectionType, port, verbose, filter);
    }

    /// <summary>
    /// Runs the KNX monitor with the specified configuration.
    /// </summary>
    /// <param name="gateway">Gateway address.</param>
    /// <param name="connectionType">Connection type (tunnel/router/usb).</param>
    /// <param name="port">Gateway port.</param>
    /// <param name="verbose">Enable verbose logging.</param>
    /// <param name="filter">Group address filter.</param>
    /// <returns>Exit code (0 = success, >0 = error).</returns>
    private static async Task<int> RunMonitorAsync(
        string gateway,
        string connectionType,
        int port,
        bool verbose,
        string? filter
    )
    {
        IHost? host = null;
        try
        {
            // Display startup banner
            DisplayStartupBanner();

            // Create configuration
            var config = new KnxMonitorConfig
            {
                ConnectionType = ParseConnectionType(connectionType),
                Gateway = gateway,
                Port = port,
                Verbose = verbose,
                Filter = filter,
            };

            // Validate configuration
            if (!ValidateConfiguration(config))
            {
                return 1; // Configuration error
            }

            // Create host builder
            var hostBuilder = Host.CreateDefaultBuilder()
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    if (verbose)
                    {
                        logging.AddConsole();
                        logging.SetMinimumLevel(LogLevel.Debug);
                    }
                    else
                    {
                        logging.SetMinimumLevel(LogLevel.Warning);
                    }
                })
                .ConfigureServices(services =>
                {
                    services.AddSingleton(config);
                    services.AddSingleton<IKnxMonitorService, KnxMonitorService>();
                    services.AddSingleton<IDisplayService, DisplayService>();
                });

            host = hostBuilder.Build();

            // Get services
            var monitorService = host.Services.GetRequiredService<IKnxMonitorService>();
            var displayService = host.Services.GetRequiredService<IDisplayService>();

            // Start display service
            await displayService.StartAsync(monitorService);

            // Start monitoring with retry logic
            const int maxRetries = 2;
            int attempt = 0;
            bool connected = false;

            while (attempt <= maxRetries && !connected)
            {
                attempt++;
                try
                {
                    Console.WriteLine(
                        $"[{DateTime.Now:HH:mm:ss.fff}] Connection attempt {attempt}/{maxRetries + 1}..."
                    );

                    using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10)); // 10 second timeout
                    await monitorService.StartMonitoringAsync(timeoutCts.Token);

                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] KNX Monitor connection successful");
                    connected = true;
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine(
                        $"[{DateTime.Now:HH:mm:ss.fff}] Connection attempt {attempt} timed out after 10 seconds"
                    );
                    if (attempt <= maxRetries)
                    {
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Retrying in 2 seconds...");
                        await Task.Delay(2000);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"[{DateTime.Now:HH:mm:ss.fff}] Connection attempt {attempt} failed: {ex.Message}"
                    );
                    if (attempt <= maxRetries)
                    {
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Retrying in 2 seconds...");
                        await Task.Delay(2000);
                    }
                }
            }

            if (!connected)
            {
                Console.WriteLine(
                    $"[{DateTime.Now:HH:mm:ss.fff}] Failed to connect after {maxRetries + 1} attempts. Exiting."
                );
                return 2; // Exit code 2 for connection failure
            }

            // Wait for cancellation (Ctrl+C)
            var cancellationToken = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                cancellationToken.Cancel();
            };

            await Task.Delay(Timeout.Infinite, cancellationToken.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected when Ctrl+C is pressed
            return 0; // Normal shutdown
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            return 1; // General error
        }
        finally
        {
            if (host != null)
            {
                // Stop services gracefully
                var monitorService = host.Services.GetService<IKnxMonitorService>();
                var displayService = host.Services.GetService<IDisplayService>();

                if (displayService != null)
                {
                    await displayService.StopAsync();
                }

                if (monitorService != null)
                {
                    await monitorService.StopMonitoringAsync();
                }

                await host.StopAsync();

                AnsiConsole.MarkupLine("\n[yellow]KNX Monitor stopped gracefully.[/]");
            }
        }

        return 0; // Success
    }

    /// <summary>
    /// Displays the startup banner.
    /// </summary>
    private static void DisplayStartupBanner()
    {
        AnsiConsole.Write(new FigletText("KNX Monitor").LeftJustified().Color(Color.Cyan1));

        AnsiConsole.MarkupLine("[dim]Visual debugging tool for KNX/EIB bus activity[/]");
        AnsiConsole.MarkupLine("[dim]Press Ctrl+C to stop monitoring[/]");
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Parses the connection type string.
    /// </summary>
    /// <param name="connectionType">Connection type string.</param>
    /// <returns>Parsed connection type.</returns>
    private static KnxConnectionType ParseConnectionType(string connectionType)
    {
        return connectionType.ToLowerInvariant() switch
        {
            "tunnel" => KnxConnectionType.Tunnel,
            "router" => KnxConnectionType.Router,
            "usb" => KnxConnectionType.Usb,
            _ => throw new ArgumentException(
                $"Invalid connection type: {connectionType}. Valid values are: tunnel, router, usb"
            ),
        };
    }

    /// <summary>
    /// Validates the configuration.
    /// </summary>
    /// <param name="config">Configuration to validate.</param>
    /// <returns>True if valid, false otherwise.</returns>
    private static bool ValidateConfiguration(KnxMonitorConfig config)
    {
        if (config.ConnectionType != KnxConnectionType.Usb && string.IsNullOrEmpty(config.Gateway))
        {
            AnsiConsole.MarkupLine("[red]Error: Gateway address is required for tunnel/router connections[/]");
            return false;
        }

        if (config.Port <= 0 || config.Port > 65535)
        {
            AnsiConsole.MarkupLine("[red]Error: Port must be between 1 and 65535[/]");
            return false;
        }

        return true;
    }
}
