using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SnapDog2.TestKit;

public class ApiTestRunner
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly bool _failEarly;
    private int _totalTests = 0;
    private int _passedTests = 0;
    private int _failedTests = 0;
    private readonly List<string> _failures = new();

    public ApiTestRunner(string baseUrl, bool failEarly)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _failEarly = failEarly;
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<int> RunAllTestsAsync()
    {
        Console.WriteLine($"🚀 Starting SnapDog2 API Tests against: {_baseUrl}");
        Console.WriteLine($"⚡ Fail Early Mode: {(_failEarly ? "ON" : "OFF")}");
        Console.WriteLine();

        var startTime = DateTime.UtcNow;

        // Run all test suites
        await RunHealthTests();
        await RunSystemTests();
        await RunZoneTests();
        await RunMediaTests();
        await RunClientTests();

        var duration = DateTime.UtcNow - startTime;

        // Print statistics
        Console.WriteLine();
        Console.WriteLine("📊 TEST RESULTS");
        Console.WriteLine("═══════════════");
        Console.WriteLine($"Total Tests:  {_totalTests}");
        Console.WriteLine($"✅ Passed:    {_passedTests}");
        Console.WriteLine($"❌ Failed:    {_failedTests}");
        Console.WriteLine($"⏱️  Duration:  {duration.TotalSeconds:F2}s");
        Console.WriteLine();

        if (_failedTests > 0)
        {
            Console.WriteLine("❌ FAILED TESTS:");
            foreach (var failure in _failures)
            {
                Console.WriteLine($"   • {failure}");
            }
            Console.WriteLine();
        }

        return _failedTests;
    }

    private async Task RunHealthTests()
    {
        Console.WriteLine("🏥 Health Tests");
        Console.WriteLine("───────────────");

        await TestEndpoint("GET", "/health", "Health Check", response =>
        {
            var json = JObject.Parse(response);
            return json["status"]?.ToString() == "Healthy";
        });
    }

    private async Task RunSystemTests()
    {
        Console.WriteLine("\n🖥️  System Tests");
        Console.WriteLine("────────────────");

        await TestEndpoint("GET", "/v1/system/status", "System Status", response =>
        {
            var json = JObject.Parse(response);
            return json["status"]?.ToString() == "running" && json["version"] != null;
        });

        await TestEndpoint("GET", "/v1/system/commands/status", "Command Status", response =>
        {
            var json = JObject.Parse(response);
            return json["status"] != null && json["timestamp"] != null;
        });

        await TestEndpoint("GET", "/v1/system/commands/errors", "Command Errors", response =>
        {
            var json = JObject.Parse(response);
            return json["error"] != null && json["timestamp"] != null;
        });

        await TestEndpoint("GET", "/v1/system/errors", "System Errors", response =>
        {
            var json = JObject.Parse(response);
            return json["error"] != null && json["timestamp"] != null;
        });

        await TestEndpoint("GET", "/v1/system/version", "System Version", response =>
        {
            var json = JObject.Parse(response);
            return json["version"] != null && json["timestamp"] != null;
        });

        await TestEndpoint("GET", "/v1/system/stats", "System Stats", response =>
        {
            var json = JObject.Parse(response);
            return json["uptime"] != null && json["memoryUsage"] != null && json["cpuTime"] != null;
        });
    }

    private async Task RunZoneTests()
    {
        Console.WriteLine("\n🎵 Zone Tests");
        Console.WriteLine("─────────────");

        // Get zones first
        var zonesResponse = await TestEndpoint("GET", "/v1/zones", "List Zones", response =>
        {
            var json = JObject.Parse(response);
            return json["items"] != null && json["total"] != null;
        });

        if (zonesResponse != null)
        {
            var zonesJson = JObject.Parse(zonesResponse);
            var zones = zonesJson["items"] as JArray;

            if (zones?.Count > 0)
            {
                // Test first zone endpoints
                var zoneIndex = 1; // Assuming zone 1 exists from dev setup

                await TestEndpoint("GET", $"/v1/zones/{zoneIndex}", "Zone Details", response =>
                {
                    var json = JObject.Parse(response);
                    return json["name"] != null && json["playbackState"] != null;
                });

                // Status endpoints
                await TestEndpoint("GET", $"/v1/zones/{zoneIndex}/name", "Zone Name");
                await TestEndpoint("GET", $"/v1/zones/{zoneIndex}/track", "Zone Track");
                await TestEndpoint("GET", $"/v1/zones/{zoneIndex}/track/title", "Track Title");
                await TestEndpoint("GET", $"/v1/zones/{zoneIndex}/track/artist", "Track Artist");
                await TestEndpoint("GET", $"/v1/zones/{zoneIndex}/track/album", "Track Album");
                await TestEndpoint("GET", $"/v1/zones/{zoneIndex}/volume", "Zone Volume");
                await TestEndpoint("GET", $"/v1/zones/{zoneIndex}/mute", "Zone Mute");
                await TestEndpoint("GET", $"/v1/zones/{zoneIndex}/repeat/track", "Track Repeat");
                await TestEndpoint("GET", $"/v1/zones/{zoneIndex}/repeat", "Playlist Repeat");
                await TestEndpoint("GET", $"/v1/zones/{zoneIndex}/shuffle", "Playlist Shuffle");
                await TestEndpoint("GET", $"/v1/zones/{zoneIndex}/playlist", "Zone Playlist");
                await TestEndpoint("GET", $"/v1/zones/{zoneIndex}/playlist/name", "Playlist Name");
                await TestEndpoint("GET", $"/v1/zones/{zoneIndex}/playlist/count", "Playlist Count");

                // Command endpoints (correct HTTP methods from blueprint)
                await TestEndpoint("POST", $"/v1/zones/{zoneIndex}/play", "Play Command");
                await TestEndpoint("POST", $"/v1/zones/{zoneIndex}/pause", "Pause Command");
                await TestEndpoint("POST", $"/v1/zones/{zoneIndex}/stop", "Stop Command");
                await TestEndpoint("PUT", $"/v1/zones/{zoneIndex}/volume", "Set Volume", null, "50");
                await TestEndpoint("PUT", $"/v1/zones/{zoneIndex}/mute", "Set Mute", null, "false");
                await TestEndpoint("PUT", $"/v1/zones/{zoneIndex}/repeat/track", "Set Track Repeat", null, "false");
                await TestEndpoint("PUT", $"/v1/zones/{zoneIndex}/repeat", "Set Playlist Repeat", null, "false");
                await TestEndpoint("PUT", $"/v1/zones/{zoneIndex}/shuffle", "Set Shuffle", null, "false");
            }
        }
    }

    private async Task RunMediaTests()
    {
        Console.WriteLine("\n🎶 Media Tests");
        Console.WriteLine("──────────────");

        await TestEndpoint("GET", "/v1/media/playlists", "List Playlists", response =>
        {
            var json = JArray.Parse(response);
            return json.Count >= 0; // Could be empty
        });

        await TestEndpoint("GET", "/v1/media/playlists/1", "Playlist Details", response =>
        {
            var json = JObject.Parse(response);
            return json["info"]?["name"] != null;
        });

        await TestEndpoint("GET", "/v1/media/tracks/1", "Track Details", response =>
        {
            var json = JObject.Parse(response);
            return json["title"] != null;
        });

        await TestEndpoint("GET", "/v1/media/playlists/1/tracks", "Playlist Tracks", response =>
        {
            var json = JArray.Parse(response);
            return json.Count >= 0;
        });

        await TestEndpoint("GET", "/v1/media/playlists/1/tracks/1", "Playlist Track Details", response =>
        {
            var json = JObject.Parse(response);
            return json["title"] != null;
        });
    }

    private async Task RunClientTests()
    {
        Console.WriteLine("\n👥 Client Tests");
        Console.WriteLine("───────────────");

        var clientsResponse = await TestEndpoint("GET", "/v1/clients", "List Clients", response =>
        {
            var json = JObject.Parse(response);
            return json["items"] != null && json["total"] != null;
        });

        if (clientsResponse != null)
        {
            var clientsJson = JObject.Parse(clientsResponse);
            var clients = clientsJson["items"] as JArray;

            if (clients?.Count > 0)
            {
                var clientId = clients[0]["id"]?.Value<int>() ?? 1;

                await TestEndpoint("GET", $"/v1/clients/{clientId}", "Client Details", response =>
                {
                    var json = JObject.Parse(response);
                    return json["name"] != null && json["mac"] != null;
                });

                await TestEndpoint("GET", $"/v1/clients/{clientId}/name", "Client Name");
                await TestEndpoint("GET", $"/v1/clients/{clientId}/volume", "Client Volume");
                await TestEndpoint("GET", $"/v1/clients/{clientId}/mute", "Client Mute");
                await TestEndpoint("GET", $"/v1/clients/{clientId}/connected", "Client Connected");

                // Command endpoints
                await TestEndpoint("PUT", $"/v1/clients/{clientId}/volume", "Set Client Volume", null, "75");
                await TestEndpoint("PUT", $"/v1/clients/{clientId}/mute", "Set Client Mute", null, "false");
            }
        }
    }

    private async Task<string?> TestEndpoint(string method, string endpoint, string testName,
        Func<string, bool>? validator = null, string? body = null)
    {
        _totalTests++;

        try
        {
            var url = $"{_baseUrl}{endpoint}";
            HttpResponseMessage response;

            if (method == "GET")
            {
                response = await _httpClient.GetAsync(url);
            }
            else if (method == "POST")
            {
                var content = body != null
                    ? new StringContent(body, System.Text.Encoding.UTF8, "application/json")
                    : new StringContent("", System.Text.Encoding.UTF8, "application/json");
                response = await _httpClient.PostAsync(url, content);
            }
            else if (method == "PUT")
            {
                var content = body != null
                    ? new StringContent(body, System.Text.Encoding.UTF8, "application/json")
                    : new StringContent("", System.Text.Encoding.UTF8, "application/json");
                response = await _httpClient.PutAsync(url, content);
            }
            else
            {
                throw new NotSupportedException($"HTTP method {method} not supported");
            }

            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                bool isValid = true;
                if (validator != null)
                {
                    try
                    {
                        isValid = validator(responseBody);
                    }
                    catch (Exception ex)
                    {
                        isValid = false;
                        Console.WriteLine($"❌ {testName} - Validation error: {ex.Message}");
                    }
                }

                if (isValid)
                {
                    Console.WriteLine($"✅ {testName}");
                    _passedTests++;
                    return responseBody;
                }
                else
                {
                    Console.WriteLine($"❌ {testName} - Invalid response format");
                    _failedTests++;
                    _failures.Add($"{testName} - Invalid response format");
                }
            }
            else
            {
                Console.WriteLine($"❌ {testName} - HTTP {(int)response.StatusCode} {response.ReasonPhrase}");
                _failedTests++;
                _failures.Add($"{testName} - HTTP {(int)response.StatusCode}");
            }

            if (_failEarly && _failedTests > 0)
            {
                Console.WriteLine($"\n💥 Fail-early mode: Stopping after {_failedTests} failure(s)");
                Environment.Exit(_failedTests);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ {testName} - Exception: {ex.Message}");
            _failedTests++;
            _failures.Add($"{testName} - Exception: {ex.Message}");

            if (_failEarly)
            {
                Console.WriteLine($"\n💥 Fail-early mode: Stopping after {_failedTests} failure(s)");
                Environment.Exit(_failedTests);
            }
        }

        return null;
    }
}
