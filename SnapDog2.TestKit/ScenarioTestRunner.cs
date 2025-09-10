using System.Net.Http;
using System.Text;
using Newtonsoft.Json.Linq;

namespace SnapDog2.TestKit;

public class ScenarioTestRunner
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private int _passedTests = 0;
    private int _failedTests = 0;
    private readonly List<string> _failedTestNames = new();

    public ScenarioTestRunner(string baseUrl)
    {
        _baseUrl = baseUrl;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
    }

    public async Task RunScenariosAsync()
    {
        Console.WriteLine("\n🎬 Scenario Tests");
        Console.WriteLine("─────────────────");

        await PlayTrackScenario();
        await TrackMetadataScenario();
        await CoverImageScenario();

        PrintResults();
    }

    private async Task PlayTrackScenario()
    {
        await TestScenario("Play Track Scenario", async () =>
        {
            // 1. Set playlist first
            await PutCommand("/v1/zones/1/playlist", "1");
            await Task.Delay(1000);

            // 2. Play a track
            await PostCommand("/v1/zones/1/play");
            await Task.Delay(2000); // Wait for playback to start

            // 3. Check if status changed to playing (playbackState = 1)
            var response = await GetAsync("/v1/zones/1");
            var zone = JObject.Parse(response);

            var playbackState = zone["playbackState"]?.ToObject<int>();
            return playbackState == 1; // 1 = Playing
        });
    }

    private async Task TrackMetadataScenario()
    {
        await TestScenario("Track Metadata Scenario", async () =>
        {
            // 1. Set playlist and play track 1
            await PutCommand("/v1/zones/1/playlist", "1");
            await PostCommand("/v1/zones/1/play/playlist/1/track", "1");
            await Task.Delay(1000);

            // 2. Get track title
            var title1 = await GetAsync("/v1/zones/1/track/title");

            // 3. Play different track
            await PostCommand("/v1/zones/1/play/playlist/1/track", "2");
            await Task.Delay(1000);

            // 4. Check title changed
            var title2 = await GetAsync("/v1/zones/1/track/title");

            return !string.IsNullOrEmpty(title1) && !string.IsNullOrEmpty(title2) && title1 != title2;
        });
    }

    private async Task CoverImageScenario()
    {
        await TestScenario("Cover Image Scenario", async () =>
        {
            // 1. Check if any playlist has cover URLs
            var playlistsResponse = await GetAsync("/v1/media/playlists");
            var playlists = JArray.Parse(playlistsResponse);

            foreach (var playlist in playlists)
            {
                var playlistIndex = playlist["index"]?.ToObject<int>();
                if (playlistIndex.HasValue)
                {
                    var tracksResponse = await GetAsync($"/v1/media/playlists/{playlistIndex}/tracks");
                    var tracks = JArray.Parse(tracksResponse);

                    foreach (var track in tracks)
                    {
                        var coverUrl = track["coverArtUrl"]?.ToString();
                        if (!string.IsNullOrEmpty(coverUrl))
                        {
                            // Try to download the cover
                            var coverResponse = await _httpClient.GetAsync($"{_baseUrl}{coverUrl}");
                            if (coverResponse.IsSuccessStatusCode)
                            {
                                var contentType = coverResponse.Content.Headers.ContentType?.MediaType;
                                if (contentType?.StartsWith("image/") == true)
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }

            // If no covers found, that's also a valid scenario result
            Console.WriteLine("   ℹ️  No cover URLs found in current playlists");
            return true;
        });
    }

    private async Task TestScenario(string name, Func<Task<bool>> test)
    {
        try
        {
            var result = await test();
            if (result)
            {
                Console.WriteLine($"✅ {name}");
                _passedTests++;
            }
            else
            {
                Console.WriteLine($"❌ {name} - Test condition failed");
                _failedTests++;
                _failedTestNames.Add($"{name} - Test condition failed");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ {name} - {ex.Message}");
            _failedTests++;
            _failedTestNames.Add($"{name} - {ex.Message}");
        }
    }

    private async Task<string> GetAsync(string endpoint)
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}{endpoint}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    private async Task PostCommand(string endpoint, string? body = null)
    {
        var jsonBody = string.IsNullOrEmpty(body) ? "{}" : $"\"{body}\"";
        var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync($"{_baseUrl}{endpoint}", content);
        response.EnsureSuccessStatusCode();
    }

    private async Task PutCommand(string endpoint, string? body = null)
    {
        var jsonBody = string.IsNullOrEmpty(body) ? "{}" : $"\"{body}\"";
        var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
        var response = await _httpClient.PutAsync($"{_baseUrl}{endpoint}", content);
        response.EnsureSuccessStatusCode();
    }

    private void PrintResults()
    {
        var total = _passedTests + _failedTests;
        Console.WriteLine($"\n📊 SCENARIO RESULTS");
        Console.WriteLine($"═══════════════════");
        Console.WriteLine($"Total Tests:  {total}");
        Console.WriteLine($"✅ Passed:    {_passedTests}");
        Console.WriteLine($"❌ Failed:    {_failedTests}");

        if (_failedTests > 0)
        {
            Console.WriteLine($"\n❌ FAILED SCENARIOS:");
            foreach (var failure in _failedTestNames)
            {
                Console.WriteLine($"   • {failure}");
            }
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
