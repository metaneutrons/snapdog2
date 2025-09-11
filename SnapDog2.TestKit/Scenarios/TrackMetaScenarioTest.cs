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
/// Tests track metadata retrieval for different media sources.
/// </summary>
public class TrackMetaScenarioTest : BaseScenarioTest
{
    public TrackMetaScenarioTest(string baseUrl) : base(baseUrl) { }

    public override string Name => "Track Meta Scenario";
    public override string Icon => "🎮";

    public override async Task<bool> ExecuteAsync()
    {
        Console.WriteLine("   🎵 Testing current track metadata...");

        // Test radio track metadata
        await PutAsync("/v1/zones/1/playlist", "1");
        await PostAsync("/v1/zones/1/play/playlist/1/track", "1");
        await Task.Delay(2000);

        var radioZoneState = await GetAsync("/v1/zones/1");
        var radioZone = JsonSerializer.Deserialize<JsonElement>(radioZoneState);
        var radioTrack = radioZone.GetProperty("track");
        var radioTitle = radioTrack.GetProperty("title").GetString();
        var radioArtist = radioTrack.GetProperty("artist").GetString();
        var radioAlbum = radioTrack.GetProperty("album").GetString();

        Console.WriteLine($"   📊 Radio Track - Title: \"{radioTitle}\", Artist: \"{radioArtist}\", Album: \"{radioAlbum}\"");

        // Test Subsonic track metadata
        await PutAsync("/v1/zones/1/playlist", "2");
        await PostAsync("/v1/zones/1/play/playlist/2/track", "1");
        await Task.Delay(2000);

        var subsonicZoneState = await GetAsync("/v1/zones/1");
        var subsonicZone = JsonSerializer.Deserialize<JsonElement>(subsonicZoneState);
        var subsonicTrack = subsonicZone.GetProperty("track");
        var subsonicTitle = subsonicTrack.GetProperty("title").GetString();
        var subsonicArtist = subsonicTrack.GetProperty("artist").GetString();
        var subsonicAlbum = subsonicTrack.GetProperty("album").GetString();

        Console.WriteLine($"   📊 Subsonic Track - Title: \"{subsonicTitle}\", Artist: \"{subsonicArtist}\", Album: \"{subsonicAlbum}\"");

        Console.WriteLine("   ✅ Track metadata available");
        return true;
    }
}
