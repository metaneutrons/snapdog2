//
// SnapDog
// The Snapcast-based Smart Home Audio System with MQTT & KNX integration
// Copyright (C) 2025 Fabian Schmieder
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <https://www.gnu.org/licenses/>.
//
using SnapDog2.Shared.Enums;
using SnapDog2.TestKit.Base;

namespace SnapDog2.TestKit;

/// <summary>
/// SnapDog2 TestKit - Comprehensive scenario testing for the SnapDog2 API.
/// </summary>
internal static class Program
{
    private static async Task Main(string[] args)
    {
        Console.WriteLine("🚀 SnapDog2 TestKit");
        Console.WriteLine("═══════════════════");
        Console.WriteLine();

        var options = ParseCommandLineArgs(args);

        if (options.ShowHelp)
        {
            ShowHelp();
            return;
        }

        Console.WriteLine("⚠️  WARNING: SnapDog2 TestKit expects values from the dev container setup.");
        Console.WriteLine("   Tests may fail if run against a different environment setup.");
        Console.WriteLine();

        try
        {
            if (options.RunHealthTests)
            {
                await RunHealthTestsAsync(options.ApiBaseUrl);
                Console.WriteLine();
            }

            if (options.RunMqttTests)
            {
                var mqttRunner = new MqttIntegrationTestRunner(
                    options.MqttUrl,
                    options.MqttBaseTopic,
                    options.ApiBaseUrl,
                    options.MqttUsername,
                    options.MqttPassword
                );
                await mqttRunner.RunAllTestsAsync();
                Console.WriteLine();
            }

            if (options.RunKnxTests)
            {
                var knxRunner = new KnxIntegrationTestRunner(
                    options.KnxHost,
                    options.KnxPort,
                    options.KnxConnectionType,
                    options.ApiBaseUrl);
                await knxRunner.RunAllTestsAsync();
                Console.WriteLine();
            }

            if (options.RunScenarios)
            {
                var testRunner = new TestRunner(options.ApiBaseUrl);
                await testRunner.RunAllScenariosAsync();
            }

            Console.WriteLine("🎉 All tests completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Tests failed: {ex.Message}");
            Environment.Exit(1);
        }
    }

    private static async Task RunHealthTestsAsync(string apiBaseUrl)
    {
        Console.WriteLine("🏥 Health Tests");
        Console.WriteLine("═══════════════");
        Console.WriteLine($"API Base URL: {apiBaseUrl}");
        Console.WriteLine();

        try
        {
            using var httpClient = new HttpClient();

            Console.WriteLine("🔧 Testing API health endpoint...");
            var response = await httpClient.GetAsync($"{apiBaseUrl}/health");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"   📊 Health status: {content}");

            if (content.Contains("Healthy") || content.Contains("healthy"))
            {
                Console.WriteLine("✅ API health check passed");
            }
            else
            {
                Console.WriteLine("⚠️  API health status unclear");
            }

            Console.WriteLine("✅ All health tests completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Health tests failed: {ex.Message}");
            throw;
        }
    }

    private static TestOptions ParseCommandLineArgs(string[] args)
    {
        var options = new TestOptions();

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--help" or "-h":
                    options.ShowHelp = true;
                    break;
                case "--tests":
                    if (i + 1 < args.Length)
                    {
                        var testTypes = args[++i].Split(',', StringSplitOptions.RemoveEmptyEntries);
                        options.RunMqttTests = testTypes.Contains("mqtt", StringComparer.OrdinalIgnoreCase);
                        options.RunKnxTests = testTypes.Contains("knx", StringComparer.OrdinalIgnoreCase);
                        options.RunHealthTests = testTypes.Contains("health", StringComparer.OrdinalIgnoreCase);
                        options.RunScenarios = testTypes.Contains("scenarios", StringComparer.OrdinalIgnoreCase);
                    }
                    break;
                case "--mqtt-url":
                    if (i + 1 < args.Length)
                    {
                        options.MqttUrl = args[++i];
                    }

                    break;
                case "--mqtt-base-topic":
                    if (i + 1 < args.Length)
                    {
                        options.MqttBaseTopic = args[++i];
                    }

                    break;
                case "--mqtt-username":
                    if (i + 1 < args.Length)
                    {
                        options.MqttUsername = args[++i];
                    }

                    break;
                case "--mqtt-password":
                    if (i + 1 < args.Length)
                    {
                        options.MqttPassword = args[++i];
                    }

                    break;
                case "--knx-host":
                    if (i + 1 < args.Length)
                    {
                        options.KnxHost = args[++i];
                    }

                    break;
                case "--knx-port":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out var port))
                    {
                        options.KnxPort = port;
                    }

                    break;
                case "--knx-connection":
                    if (i + 1 < args.Length && Enum.TryParse<KnxConnectionType>(args[++i], true, out var connType))
                    {
                        options.KnxConnectionType = connType;
                    }

                    break;
                case "--api-url":
                    if (i + 1 < args.Length)
                    {
                        options.ApiBaseUrl = args[++i];
                    }

                    break;
            }
        }

        return options;
    }

    private static void ShowHelp()
    {
        Console.WriteLine("Available options:");
        Console.WriteLine("  --help, -h              Show this help message");
        Console.WriteLine();
        Console.WriteLine("Test Selection:");
        Console.WriteLine("  --tests <types>         Comma-separated test types: mqtt,knx,health,scenarios");
        Console.WriteLine("                          (default: all tests)");
        Console.WriteLine();
        Console.WriteLine("Configuration:");
        Console.WriteLine("  --api-url <url>         API base URL (default: http://localhost:8000/api)");
        Console.WriteLine("  --mqtt-url <url>        MQTT broker URL (default: mqtt://localhost:1883)");
        Console.WriteLine("  --mqtt-base-topic <topic> MQTT base topic (default: snapdog/)");
        Console.WriteLine("  --mqtt-username <user>  MQTT username (default: snapdog)");
        Console.WriteLine("  --mqtt-password <pass>  MQTT password (default: snapdog)");
        Console.WriteLine("  --knx-host <host>       KNX gateway host (default: localhost)");
        Console.WriteLine("  --knx-port <port>       KNX gateway port (default: 3671)");
        Console.WriteLine("  --knx-connection <type> KNX connection type: Tunnel, Router, Usb (default: Tunnel)");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  dotnet run                                    # Run all tests");
        Console.WriteLine("  dotnet run -- --tests mqtt                   # MQTT tests only");
        Console.WriteLine("  dotnet run -- --tests knx,health             # KNX and health tests");
        Console.WriteLine("  dotnet run -- --tests mqtt --mqtt-url mqtt://broker.local:1883");
        Console.WriteLine("  dotnet run -- --tests knx --knx-host 192.168.1.100");
    }

    private class TestOptions
    {
        public bool ShowHelp { get; set; }
        public string ApiBaseUrl { get; set; } = "http://localhost:8000/api";
        public string MqttUrl { get; set; } = "mqtt://localhost:1883";
        public string MqttBaseTopic { get; set; } = "snapdog/";
        public string MqttUsername { get; set; } = "snapdog";
        public string MqttPassword { get; set; } = "snapdog";
        public string KnxHost { get; set; } = "localhost";
        public int KnxPort { get; set; } = 3671;
        public KnxConnectionType KnxConnectionType { get; set; } = KnxConnectionType.Tunnel;
        public bool RunMqttTests { get; set; } = true;
        public bool RunKnxTests { get; set; } = true;
        public bool RunHealthTests { get; set; } = true;
        public bool RunScenarios { get; set; } = true;
    }
}
