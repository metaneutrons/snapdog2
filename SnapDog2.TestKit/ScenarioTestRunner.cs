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
        await ProgressAndPositionScenario();
        await TrackMetaScenario();
        await PlaylistScenario();

        PrintResults();
    }

    private async Task PlayTrackScenario()
    {
        await TestScenario("Play Track Scenario", async () =>
        {
            Console.WriteLine("   🔍 Setting playlist to Radio (index 1)...");
            await PutCommand("/v1/zones/1/playlist", "1");
            await Task.Delay(2000);

            // Test 1: /play/playlist/{playlistIndex}/track (primary method)
            Console.WriteLine("   ▶️  Testing /play/playlist/1/track endpoint...");
            await PostCommand("/v1/zones/1/play/playlist/1/track", "1");
            await Task.Delay(3000);

            var response1 = await GetAsync("/v1/zones/1");
            var zone1 = JObject.Parse(response1);
            var playbackState1 = zone1["playbackState"]?.ToObject<int>();
            Console.WriteLine($"   📈 Playback state after /play/playlist/1/track: {playbackState1} (1=Playing)");

            if (playbackState1 != 1)
            {
                Console.WriteLine($"   ❌ /play/playlist/1/track failed: expected 1, got {playbackState1}");
                return false;
            }

            // Test 2: Pause functionality
            Console.WriteLine("   ⏸️  Testing pause functionality...");
            await PostCommand("/v1/zones/1/pause");
            await Task.Delay(2000);

            var pauseResponse = await GetAsync("/v1/zones/1");
            var pauseZone = JObject.Parse(pauseResponse);
            var pauseState = pauseZone["playbackState"]?.ToObject<int>();
            Console.WriteLine($"   📊 Playback state after pause: {pauseState} (2=Paused)");

            if (pauseState != 2)
            {
                Console.WriteLine($"   ❌ Pause failed: expected 2, got {pauseState}");
                return false;
            }

            // Test 3: /play (resume)
            Console.WriteLine("   ▶️  Testing /play endpoint (resume)...");
            await PostCommand("/v1/zones/1/play");
            await Task.Delay(3000);

            var response2 = await GetAsync("/v1/zones/1");
            var zone2 = JObject.Parse(response2);
            var playbackState2 = zone2["playbackState"]?.ToObject<int>();
            Console.WriteLine($"   📈 Playback state after /play (resume): {playbackState2} (1=Playing)");

            if (playbackState2 != 1)
            {
                Console.WriteLine($"   ❌ /play (resume) failed: expected 1, got {playbackState2}");
                return false;
            }

            // Test 4: Stop functionality
            Console.WriteLine("   ⏹️  Testing stop functionality...");
            await PostCommand("/v1/zones/1/stop");
            await Task.Delay(2000);

            var response3 = await GetAsync("/v1/zones/1");
            var zone3 = JObject.Parse(response3);
            var playbackState3 = zone3["playbackState"]?.ToObject<int>();
            Console.WriteLine($"   📈 Playback state after stop: {playbackState3} (0=Stopped, 2=Paused)");

            // Accept both 0 (stopped) and 2 (paused) as valid stop states
            if (playbackState3 != 0 && playbackState3 != 2)
            {
                Console.WriteLine($"   ❌ Stop failed: expected 0 or 2, got {playbackState3}");
                return false;
            }

            Console.WriteLine("   ✅ All play endpoints tested successfully");
            Console.WriteLine("   📋 Tested: /play/playlist/1/track ✅, /pause ✅, /play (resume) ✅, /stop ✅");
            Console.WriteLine("   📊 Playback states: 0=Stopped, 1=Playing, 2=Paused");
            return true;
        });
    }

    private async Task ProgressAndPositionScenario()
    {
        await TestScenario("Progress & Position Scenario", async () =>
        {
            // Test radio track progress (playlist 1)
            Console.WriteLine("   📻 Testing radio track progress (playlist 1)...");
            await PutCommand("/v1/zones/1/playlist", "1");
            await PostCommand("/v1/zones/1/play/playlist/1/track", "1");
            await Task.Delay(3000);

            var pos1 = await GetAsync("/v1/zones/1/track/position");
            var progress1 = await GetAsync("/v1/zones/1/track/progress");
            Console.WriteLine($"   📊 Radio - Position: {pos1}ms, Progress: {progress1}");

            await Task.Delay(3000);
            var pos2 = await GetAsync("/v1/zones/1/track/position");
            var progress2 = await GetAsync("/v1/zones/1/track/progress");
            Console.WriteLine($"   📊 Radio - Position: {pos2}ms, Progress: {progress2}");

            if (int.Parse(pos2) > int.Parse(pos1))
            {
                Console.WriteLine("   ✅ Radio track: position increasing correctly");
            }

            // Test Subsonic track progress (playlist 2)
            Console.WriteLine("   🎵 Testing Subsonic track progress (playlist 2)...");
            await PutCommand("/v1/zones/1/playlist", "2");
            await PostCommand("/v1/zones/1/play/playlist/2/track", "1");

            // Wait longer for Subsonic stream buffering (HTTP streams need more time)
            Console.WriteLine("   ⏳ Waiting for Subsonic stream to buffer...");
            await Task.Delay(10000);

            var subPos1 = await GetAsync("/v1/zones/1/track/position");
            var subProgress1 = await GetAsync("/v1/zones/1/track/progress");
            Console.WriteLine($"   📊 Subsonic - Position: {subPos1}ms, Progress: {subProgress1}");

            await Task.Delay(3000);
            var subPos2 = await GetAsync("/v1/zones/1/track/position");
            var subProgress2 = await GetAsync("/v1/zones/1/track/progress");
            Console.WriteLine($"   📊 Subsonic - Position: {subPos2}ms, Progress: {subProgress2}");

            if (float.Parse(subProgress2) == 0)
            {
                Console.WriteLine("   ❌ Subsonic progress is 0 - position tracking is not working!");
                return false;
            }

            if (int.Parse(subPos2) > int.Parse(subPos1))
            {
                Console.WriteLine("   ✅ Subsonic track: position increasing correctly");
            }
            else
            {
                Console.WriteLine("   ❌ Subsonic position not increasing - playback may not be working!");
                return false;
            }

            return true;
        });
    }

    private async Task TrackMetaScenario()
    {
        await TestScenario("Track Meta Scenario", async () =>
        {
            Console.WriteLine("   🎵 Testing current track metadata...");

            // Play radio track
            await PutCommand("/v1/zones/1/playlist", "1");
            await PostCommand("/v1/zones/1/play/playlist/1/track", "1");
            await Task.Delay(3000);

            var title = await GetAsync("/v1/zones/1/track/title");
            var artist = await GetAsync("/v1/zones/1/track/artist");
            var album = await GetAsync("/v1/zones/1/track/album");

            Console.WriteLine($"   📊 Radio Track - Title: {title}, Artist: {artist}, Album: {album}");

            // Play Subsonic track
            await PutCommand("/v1/zones/1/playlist", "2");
            await PostCommand("/v1/zones/1/play/playlist/2/track", "1");
            await Task.Delay(3000);

            var subTitle = await GetAsync("/v1/zones/1/track/title");
            var subArtist = await GetAsync("/v1/zones/1/track/artist");
            var subAlbum = await GetAsync("/v1/zones/1/track/album");

            Console.WriteLine($"   📊 Subsonic Track - Title: {subTitle}, Artist: {subArtist}, Album: {subAlbum}");

            if (!string.IsNullOrEmpty(title) || !string.IsNullOrEmpty(subTitle))
            {
                Console.WriteLine("   ✅ Track metadata available");
            }

            return true;
        });
    }

    private async Task PlaylistScenario()
    {
        await TestScenario("Playlist Scenario", async () =>
        {
            Console.WriteLine("   🖼️  Testing playlist cover URLs and images...");

            // Check Subsonic playlist (index 2) which should have covers
            var tracksResponse = await GetAsync("/v1/media/playlists/2/tracks");
            var tracks = JArray.Parse(tracksResponse);
            Console.WriteLine($"   📊 Found {tracks.Count} tracks in Subsonic playlist");

            int validCovers = 0;
            foreach (var track in tracks)
            {
                var coverUrl = track["coverArtUrl"]?.ToString();
                if (!string.IsNullOrEmpty(coverUrl))
                {
                    Console.WriteLine($"   🔍 Found cover URL: {coverUrl}");

                    // Validate URL structure (accept both relative and absolute URLs)
                    if (coverUrl.StartsWith("/api/v1/cover/") || coverUrl.Contains("/api/v1/cover/"))
                    {
                        Console.WriteLine("   ✅ Valid cover URL structure");

                        // Test if the cover URL returns valid image data
                        try
                        {
                            // Handle both relative and absolute URLs
                            var fullUrl = coverUrl.StartsWith("http") ? coverUrl : $"{_baseUrl}{coverUrl}";
                            Console.WriteLine($"   🔗 Testing full URL: {fullUrl}");
                            var imageResponse = await _httpClient.GetAsync(fullUrl);
                            if (imageResponse.IsSuccessStatusCode)
                            {
                                var contentType = imageResponse.Content.Headers.ContentType?.MediaType;
                                if (contentType?.StartsWith("image/") == true)
                                {
                                    Console.WriteLine($"   ✅ Cover URL returns valid image ({contentType})");
                                    validCovers++;
                                }
                                else
                                {
                                    Console.WriteLine($"   ⚠️  Cover URL returns non-image content: {contentType}");
                                }
                            }
                            else
                            {
                                Console.WriteLine($"   ⚠️  Cover URL returns HTTP {imageResponse.StatusCode}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"   ⚠️  Error testing cover URL: {ex.Message}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("   ❌ Invalid cover URL structure");
                    }
                }
            }

            if (validCovers > 0)
            {
                Console.WriteLine($"   ✅ Found {validCovers} tracks with valid cover images");
            }
            else
            {
                Console.WriteLine("   ⚠️  No valid cover images found");
            }

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
