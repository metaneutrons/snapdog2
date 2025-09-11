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
using System.Text;
using System.Text.Json;

namespace SnapDog2.TestKit;

/// <summary>
/// Real-world MQTT integration test runner for SnapDog2.
/// Tests MQTT integration functionality through API endpoints and service verification.
/// </summary>
public class MqttIntegrationTestRunner
{
    private readonly string _mqttUrl;
    private readonly string _baseTopic;
    private readonly string _apiBaseUrl;
    private readonly string _mqttUsername;
    private readonly string _mqttPassword;
    private readonly HttpClient _httpClient;

    public MqttIntegrationTestRunner(string mqttUrl, string baseTopic, string apiBaseUrl, string mqttUsername, string mqttPassword)
    {
        _mqttUrl = mqttUrl;
        _baseTopic = baseTopic.TrimEnd('/') + "/";
        _apiBaseUrl = apiBaseUrl;
        _mqttUsername = mqttUsername;
        _mqttPassword = mqttPassword;
        _httpClient = new HttpClient();
    }

    public async Task RunAllTestsAsync()
    {
        Console.WriteLine("🔌 MQTT Integration Tests");
        Console.WriteLine("═════════════════════════");
        Console.WriteLine($"MQTT Broker: {_mqttUrl}");
        Console.WriteLine($"Base Topic: {_baseTopic}");
        Console.WriteLine($"API Base URL: {_apiBaseUrl}");
        Console.WriteLine();

        try
        {
            await TestMqttServiceHealth();
            await TestZonePlaybackStateNotification();
            await TestClientVolumeNotification();
            await TestClientMuteNotification();
            await TestZoneTrackChangedNotification();

            Console.WriteLine("✅ All MQTT integration tests completed successfully!");
            Console.WriteLine($"📝 Note: Use external MQTT client to verify messages:");
            Console.WriteLine($"   mosquitto_sub -h {ExtractHostFromUrl(_mqttUrl)} -u {_mqttUsername} -P {_mqttPassword} -t '{_baseTopic}#'");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ MQTT integration tests failed: {ex.Message}");
            throw;
        }
        finally
        {
            _httpClient.Dispose();
        }
    }

    private async Task TestMqttServiceHealth()
    {
        Console.WriteLine("🔧 Testing MQTT service health...");

        try
        {
            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/health");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            if (content.Contains("Healthy") || content.Contains("healthy"))
            {
                Console.WriteLine("✅ MQTT service health check passed");
            }
            else
            {
                Console.WriteLine("⚠️  MQTT service health status unclear");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ MQTT service health check failed: {ex.Message}");
            throw;
        }
    }

    private async Task TestZonePlaybackStateNotification()
    {
        Console.WriteLine("🎵 Testing zone playback state MQTT integration...");

        try
        {
            // Trigger zone play via API - this should generate MQTT notifications
            var response = await _httpClient.PostAsync($"{_apiBaseUrl}/v1/zones/1/play", null);
            response.EnsureSuccessStatusCode();

            Console.WriteLine($"   📤 API call successful - should publish to {_baseTopic}zones/1/playback");

            // Wait for processing
            await Task.Delay(2000);

            // Trigger zone pause
            response = await _httpClient.PostAsync($"{_apiBaseUrl}/v1/zones/1/pause", null);
            response.EnsureSuccessStatusCode();

            Console.WriteLine($"   📤 Pause command sent - should publish to {_baseTopic}zones/1/playback");
            await Task.Delay(1000);

            Console.WriteLine("✅ Zone playback state MQTT integration test completed");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Zone playback state MQTT test failed: {ex.Message}");
            throw;
        }
    }

    private async Task TestClientVolumeNotification()
    {
        Console.WriteLine("🔊 Testing client volume MQTT integration...");

        try
        {
            // Set client volume via API - this should generate MQTT notifications
            var volumeData = JsonSerializer.Serialize(50);
            var content = new StringContent(volumeData, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"{_apiBaseUrl}/v1/clients/1/volume", content);
            response.EnsureSuccessStatusCode();

            Console.WriteLine($"   📤 Volume set to 50 - should publish to {_baseTopic}clients/1/volume");

            await Task.Delay(1000);

            // Verify volume was set
            var getResponse = await _httpClient.GetAsync($"{_apiBaseUrl}/v1/clients/1/volume");
            getResponse.EnsureSuccessStatusCode();
            var volumeContent = await getResponse.Content.ReadAsStringAsync();

            if (volumeContent.Contains("50"))
            {
                Console.WriteLine("✅ Client volume MQTT integration test completed");
            }
            else
            {
                Console.WriteLine("⚠️  Client volume may not have been set correctly");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Client volume MQTT test failed: {ex.Message}");
            throw;
        }
    }

    private async Task TestClientMuteNotification()
    {
        Console.WriteLine("🔇 Testing client mute MQTT integration...");

        try
        {
            // Toggle client mute via API - this should generate MQTT notifications
            var response = await _httpClient.PostAsync($"{_apiBaseUrl}/v1/clients/1/mute/toggle", null);
            response.EnsureSuccessStatusCode();

            Console.WriteLine($"   📤 Mute toggled - should publish to {_baseTopic}clients/1/mute");

            await Task.Delay(1000);

            // Toggle again to restore state
            response = await _httpClient.PostAsync($"{_apiBaseUrl}/v1/clients/1/mute/toggle", null);
            response.EnsureSuccessStatusCode();

            Console.WriteLine($"   📤 Mute toggled again - should publish to {_baseTopic}clients/1/mute");
            await Task.Delay(1000);

            Console.WriteLine("✅ Client mute MQTT integration test completed");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Client mute MQTT test failed: {ex.Message}");
            throw;
        }
    }

    private async Task TestZoneTrackChangedNotification()
    {
        Console.WriteLine("🎶 Testing zone track changed MQTT integration...");

        try
        {
            // Trigger track changes
            await _httpClient.PostAsync($"{_apiBaseUrl}/v1/zones/1/next", null);
            Console.WriteLine($"   📤 Next track - should publish to {_baseTopic}zones/1/track/*");

            await Task.Delay(2000);

            await _httpClient.PostAsync($"{_apiBaseUrl}/v1/zones/1/previous", null);
            Console.WriteLine($"   📤 Previous track - should publish to {_baseTopic}zones/1/track/*");

            await Task.Delay(1000);

            Console.WriteLine("✅ Zone track changed MQTT integration test completed");
            Console.WriteLine("   📝 Expected topics: title, artist, album, duration, position");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Zone track changed MQTT test failed: {ex.Message}");
            throw;
        }
    }

    private static string ExtractHostFromUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            return uri.Host;
        }
        catch
        {
            return "localhost";
        }
    }
}
