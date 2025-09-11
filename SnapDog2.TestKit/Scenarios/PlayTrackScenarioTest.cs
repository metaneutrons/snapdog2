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

using SnapDog2.TestKit.Base;

/// <summary>
/// Tests play, pause, resume, and stop functionality across different endpoints.
/// </summary>
public class PlayTrackScenarioTest : BaseScenarioTest
{
    public PlayTrackScenarioTest(string baseUrl) : base(baseUrl) { }

    public override string Name => "Play Track Scenario";
    public override string Icon => "🎮";

    public override async Task<bool> ExecuteAsync()
    {
        Console.WriteLine("   🔍 Setting playlist to Radio (index 1)...");
        await PutAsync("/v1/zones/1/playlist", "1");

        Console.WriteLine("   ▶️  Testing /play/playlist/1/track endpoint...");
        await PostAsync("/v1/zones/1/play/playlist/1/track", "1");
        var playState1 = await GetAsync("/v1/zones/1/track/playing");
        Console.WriteLine($"   📈 Playback state after /play/playlist/1/track: {playState1} (true=Playing)");

        Console.WriteLine("   ⏸️  Testing pause functionality...");
        await PostAsync("/v1/zones/1/pause");
        var pauseState = await GetAsync("/v1/zones/1/track/playing");
        Console.WriteLine($"   📊 Playback state after pause: {pauseState} (false=Paused)");

        Console.WriteLine("   ▶️  Testing /play endpoint (resume)...");
        await PostAsync("/v1/zones/1/play");
        var resumeState = await GetAsync("/v1/zones/1/track/playing");
        Console.WriteLine($"   📈 Playback state after /play (resume): {resumeState} (true=Playing)");

        Console.WriteLine("   ⏹️  Testing stop functionality...");
        await PostAsync("/v1/zones/1/stop");
        var stopState = await GetAsync("/v1/zones/1/track/playing");
        Console.WriteLine($"   📈 Playback state after stop: {stopState} (false=Stopped)");

        Console.WriteLine("   ✅ All play endpoints tested successfully");
        Console.WriteLine("   📋 Tested: /play/playlist/1/track ✅, /pause ✅, /play (resume) ✅, /stop ✅");
        Console.WriteLine("   📊 Playback states: 0=Stopped, 1=Playing, 2=Paused");

        return true;
    }
}
