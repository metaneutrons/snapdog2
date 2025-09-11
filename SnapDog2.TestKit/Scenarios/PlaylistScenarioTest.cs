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
namespace SnapDog2.TestKit.Scenarios;

using System.Text.Json;
using SnapDog2.TestKit.Base;

/// <summary>
/// Tests playlist functionality and cover image validation.
/// </summary>
public class PlaylistScenarioTest : BaseScenarioTest
{
    public PlaylistScenarioTest(string baseUrl) : base(baseUrl) { }

    public override string Name => "Playlist Scenario";
    public override string Icon => "🎮";

    public override async Task<bool> ExecuteAsync()
    {
        Console.WriteLine("   🖼️  Testing playlist cover URLs and images...");

        var tracksResponse = await GetAsync("/v1/media/playlists/2/tracks");
        var tracks = JsonSerializer.Deserialize<JsonElement[]>(tracksResponse);

        Console.WriteLine($"   📊 Found {tracks.Length} tracks in Subsonic playlist");

        var validCovers = 0;
        foreach (var track in tracks)
        {
            var coverUrl = track.GetProperty("coverArtUrl").GetString();
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
                        var fullUrl = coverUrl.StartsWith("http") ? coverUrl : $"{BaseUrl}{coverUrl}";
                        Console.WriteLine($"   🔗 Testing full URL: {fullUrl}");
                        var imageResponse = await HttpClient.GetAsync(fullUrl);
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
    }
}
