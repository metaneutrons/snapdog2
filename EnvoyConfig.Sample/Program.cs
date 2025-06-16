using System;
using System.IO;
using System.Linq;
using dotenv.net;
using EnvoyConfig;
using EnvoyConfig.Conversion;
using Spectre.Console;

namespace EnvoyConfig.Sample;

/// <summary>
/// Main program for the SNAPDOG configuration demonstration.
/// Shows how to register custom type converters and load nested configurations using the SampleConfig with SNAPDOG clients.
/// </summary>
internal class Program
{
    private static void Main(string[] args)
    {
        // Register the custom KnxAddress converter first
        RegisterCustomConverters();

        // Resolve absolute path to sample.env
        var envPath = Path.Combine(AppContext.BaseDirectory, "sample.env");
        if (!File.Exists(envPath))
        {
            // Try project dir relative to working dir
            envPath = Path.GetFullPath("EnvoyConfig.Sample/sample.env", Environment.CurrentDirectory);
        }

        // Load .env file
        DotEnv.Load(options: new DotEnvOptions(envFilePaths: [envPath], overwriteExistingVars: true));

        // Set global prefix
        EnvConfig.GlobalPrefix = "SNAPDOG_";

        // Load config using SampleConfig (which now includes SnapdogClients)
        var config = EnvConfig.Load<SampleConfig>(new SpectreConsoleLogSink());

        DisplayHeader();
        DisplayEnvironmentVariables();
        DisplayConfiguration(config);
        DisplayFooter();
    }

    private static void RegisterCustomConverters()
    {
        // Register the custom KnxAddress converter
        TypeConverterRegistry.RegisterConverter(typeof(KnxAddress), new KnxAddressConverter());
        TypeConverterRegistry.RegisterConverter(typeof(KnxAddress?), new KnxAddressConverter());
    }

    private static void DisplayHeader()
    {
        AnsiConsole.Write(new Align(new FigletText("Snapdog Config").Color(Color.Green), HorizontalAlignment.Left));
        AnsiConsole.MarkupLine($"[bold yellow]🐶 Welcome to the Snapdog Sample Application![/]");
        AnsiConsole.MarkupLine($"[bold blue]Loaded configuration from:[/] [white]sample.env[/]");
        AnsiConsole.WriteLine();
    }

    private static void DisplayEnvironmentVariables()
    {
        AnsiConsole.Write(new Rule("[grey]--- ENVIRONMENT VARIABLES ---[/]").RuleStyle("grey"));

        // Print all SNAPDOG_ env vars
        var snapdogVars = Environment
            .GetEnvironmentVariables()
            .Cast<System.Collections.DictionaryEntry>()
            .Where(e => e.Key is string k && k.StartsWith("SNAPDOG_"))
            .Select(e => ($"{e.Key}", e.Value?.ToString() ?? "<null>"))
            .OrderBy(t => t.Item1)
            .ToArray();

        if (snapdogVars.Length > 0)
        {
            var table = new Table().NoBorder();
            table.AddColumn(new TableColumn("[grey]Env Key[/]").LeftAligned());
            table.AddColumn(new TableColumn("[grey]Value[/]").LeftAligned());
            foreach (var (k, v) in snapdogVars)
            {
                table.AddRow($"[white]{k}[/]", $"[bold cyan]{v}[/]");
            }

            AnsiConsole.Write(
                new Panel(table).Header("[green]SNAPDOG_ Environment Variables[/]", Justify.Left).Collapse()
            );
        }
        AnsiConsole.Write(new Rule("[grey]--- END ENVIRONMENT VARIABLES ---[/]").RuleStyle("grey"));
        AnsiConsole.WriteLine();
    }

    private static void DisplayConfiguration(SampleConfig config)
    {
        // Display basic system configuration
        PrintSection(
            "System",
            "⚙️",
            [("Environment", typeof(string).Name, config.Env), ("Log Level", typeof(string).Name, config.LogLevel)]
        );

        PrintSection(
            "Telemetry",
            "📊",
            [
                ("Enabled", typeof(bool).Name, config.TelemetryEnabled.ToString()),
                ("Service Name", typeof(string).Name, config.TelemetryServiceName),
                ("Sampling Rate", typeof(int).Name, config.TelemetrySamplingRate.ToString()),
            ]
        );

        PrintSection(
            "Prometheus",
            "📈",
            [
                ("Enabled", typeof(bool).Name, config.PrometheusEnabled.ToString()),
                ("Path", typeof(string).Name, config.PrometheusPath),
                ("Port", typeof(int).Name, config.PrometheusPort.ToString()),
            ]
        );

        PrintSection(
            "Jaeger",
            "🕵️",
            [
                ("Enabled", typeof(bool).Name, config.JaegerEnabled.ToString()),
                ("Endpoint", typeof(string).Name, config.JaegerEndpoint),
                ("Agent Host", typeof(string).Name, config.JaegerAgentHost),
                ("Agent Port", typeof(int).Name, config.JaegerAgentPort.ToString()),
            ]
        );

        PrintSection(
            "API Auth",
            "🔑",
            [
                ("Enabled", typeof(bool).Name, config.ApiAuthEnabled.ToString()),
                ("API Keys", "string[]", string.Join(", ", config.ApiKeys)),
            ]
        );

        PrintSection("Zones", "🗺️", [("Zones", "string[]", string.Join(", ", config.Zones))]);

        // Snapcast configuration as key-value map
        PrintSection(
            "Snapcast (key-value object)",
            "🎵",
            config.Snapcast.Select(kv => (kv.Key, typeof(string).Name, kv.Value)).ToArray()
        );

        // Print all MQTT zone configs
        if (config.ZonesMqtt != null && config.ZonesMqtt.Count > 0)
        {
            int idx = 1;
            foreach (var zone in config.ZonesMqtt)
            {
                var props = zone.GetType().GetProperties();
                var rows = props
                    .Select(p =>
                        (
                            p.Name.Replace("Topic", " Topic")
                                .Replace("Set ", "Set ")
                                .Replace("Base", "Base ")
                                .Replace("Control", "Control ")
                                .Replace("Track", "Track ")
                                .Replace("Playlist", "Playlist ")
                                .Replace("Volume", "Volume ")
                                .Replace("Mute", "Mute ")
                                .Replace("State", "State ")
                                .Replace("  ", " ")
                                .Trim(),
                            p.PropertyType.Name,
                            p.GetValue(zone)?.ToString() ?? "<null>"
                        )
                    )
                    .ToArray();
                PrintSection($"MQTT Zone {idx}", "📡", rows);
                idx++;
            }
        }

        // Display SNAPDOG clients configuration - NEW SECTION
        DisplaySnapdogClients(config.SnapdogClients);

        // Display radio stations
        if (config.RadioStations != null && config.RadioStations.Count > 0)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.Write(new Rule("[green]Radio Stations[/]").RuleStyle("green"));
            AnsiConsole.WriteLine();

            for (int i = 0; i < config.RadioStations.Count; i++)
            {
                var station = config.RadioStations[i];
                PrintSection(
                    $"Radio Station {i + 1}: {station.Name}",
                    "📻",
                    [("Name", typeof(string).Name, station.Name), ("URL", typeof(string).Name, station.URL)]
                );
            }
        }
        else
        {
            AnsiConsole.MarkupLine("[yellow]No radio stations configured.[/]");
        }
    }

    private static void DisplaySnapdogClients(System.Collections.Generic.List<ClientConfig> clients)
    {
        if (clients == null || clients.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No SNAPDOG clients configured.[/]");
            return;
        }

        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule($"[green]SNAPDOG Clients ({clients.Count} configured)[/]").RuleStyle("green"));
        AnsiConsole.WriteLine();

        for (int i = 0; i < clients.Count; i++)
        {
            var client = clients[i];

            // Basic client info
            PrintSection(
                $"Client {i + 1}: {client.Name}",
                "🎛️",
                [
                    ("Name", typeof(string).Name, client.Name),
                    ("MAC Address", typeof(string).Name, client.Mac),
                    ("MQTT Base Topic", typeof(string).Name, client.MqttBaseTopic),
                    ("Default Zone", typeof(int).Name, client.DefaultZone.ToString()),
                ]
            );

            // MQTT Topics
            var mqttRows = new[]
            {
                ("Volume Set Topic", typeof(string).Name, client.Mqtt.VolumeSetTopic ?? "<null>"),
                ("Mute Set Topic", typeof(string).Name, client.Mqtt.MuteSetTopic ?? "<null>"),
                ("Latency Set Topic", typeof(string).Name, client.Mqtt.LatencySetTopic ?? "<null>"),
                ("Zone Set Topic", typeof(string).Name, client.Mqtt.ZoneSetTopic ?? "<null>"),
                ("Control Topic", typeof(string).Name, client.Mqtt.ControlTopic ?? "<null>"),
                ("Connected Topic", typeof(string).Name, client.Mqtt.ConnectedTopic ?? "<null>"),
                ("Volume Topic", typeof(string).Name, client.Mqtt.VolumeTopic ?? "<null>"),
                ("Mute Topic", typeof(string).Name, client.Mqtt.MuteTopic ?? "<null>"),
                ("Latency Topic", typeof(string).Name, client.Mqtt.LatencyTopic ?? "<null>"),
                ("Zone Topic", typeof(string).Name, client.Mqtt.ZoneTopic ?? "<null>"),
                ("State Topic", typeof(string).Name, client.Mqtt.StateTopic ?? "<null>"),
            };
            PrintSection($"MQTT Topics (Client {i + 1})", "📡", mqttRows);

            // KNX Configuration
            var knxRows = new[]
            {
                ("Enabled", typeof(bool).Name, client.Knx.Enabled.ToString()),
                ("Volume", typeof(KnxAddress).Name, client.Knx.Volume?.ToString() ?? "<null>"),
                ("Volume Status", typeof(KnxAddress).Name, client.Knx.VolumeStatus?.ToString() ?? "<null>"),
                ("Volume Up", typeof(KnxAddress).Name, client.Knx.VolumeUp?.ToString() ?? "<null>"),
                ("Volume Down", typeof(KnxAddress).Name, client.Knx.VolumeDown?.ToString() ?? "<null>"),
                ("Mute", typeof(KnxAddress).Name, client.Knx.Mute?.ToString() ?? "<null>"),
                ("Mute Status", typeof(KnxAddress).Name, client.Knx.MuteStatus?.ToString() ?? "<null>"),
                ("Mute Toggle", typeof(KnxAddress).Name, client.Knx.MuteToggle?.ToString() ?? "<null>"),
                ("Latency", typeof(KnxAddress).Name, client.Knx.Latency?.ToString() ?? "<null>"),
                ("Latency Status", typeof(KnxAddress).Name, client.Knx.LatencyStatus?.ToString() ?? "<null>"),
                ("Zone", typeof(KnxAddress).Name, client.Knx.Zone?.ToString() ?? "<null>"),
                ("Zone Status", typeof(KnxAddress).Name, client.Knx.ZoneStatus?.ToString() ?? "<null>"),
                ("Connected Status", typeof(KnxAddress).Name, client.Knx.ConnectedStatus?.ToString() ?? "<null>"),
            };
            PrintSection($"KNX Configuration (Client {i + 1})", "🏠", knxRows);
        }
    }

    private static void DisplayFooter()
    {
        AnsiConsole.Write(new Rule("[green]✔ Ready![/]").RuleStyle("green"));
        AnsiConsole.MarkupLine("[italic grey]Tip: Edit sample.env and rerun to see changes instantly![/]");
    }

    private static void PrintSection(string title, string icon, (string, string, string)[] rows)
    {
        var panel = new Panel(CreateTable(rows)).Header($"{icon} [bold]{title}[/]", Justify.Left).Collapse();
        AnsiConsole.Write(panel);
    }

    private static Table CreateTable((string, string, string)[] rows)
    {
        var table = new Table().NoBorder();
        table.AddColumn(new TableColumn("[grey]Key[/]").LeftAligned());
        table.AddColumn(new TableColumn("[grey]Type[/]").LeftAligned());
        table.AddColumn(new TableColumn("[grey]Value[/]").LeftAligned());
        foreach (var (k, t, v) in rows)
        {
            var safeType = Markup.Escape(t);
            string[] lines;
            if ((t.Contains("[]") || t.ToLower().Contains("array")) && v.Contains(","))
            {
                // Split by comma and display each on a new line
                lines = v.Split(',').Select(s => Markup.Escape(s.Trim())).ToArray();
            }
            else if (v.Contains("\n"))
            {
                lines = v.Split('\n').Select(s => Markup.Escape(s)).ToArray();
            }
            else
            {
                lines = [Markup.Escape(v)];
            }
            for (int i = 0; i < lines.Length; i++)
            {
                if (i == 0)
                {
                    table.AddRow($"[white]{k}[/]", $"[yellow]{safeType}[/]", $"[bold cyan]{lines[i]}[/]");
                }
                else
                {
                    table.AddRow("", "", $"[bold cyan]{lines[i]}[/]");
                }
            }
        }

        return table;
    }
}
