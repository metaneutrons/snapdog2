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
/// Tests track position and progress tracking for both radio and Subsonic streams.
/// </summary>
public class ProgressPositionScenarioTest : BaseScenarioTest
{
    public ProgressPositionScenarioTest(string baseUrl) : base(baseUrl) { }

    public override string Name => "Progress & Position Scenario";
    public override string Icon => "🎮";

    public override async Task<bool> ExecuteAsync()
    {
        // Test radio track progress
        Console.WriteLine("   📻 Testing radio track progress (playlist 1)...");
        await PutAsync("/v1/zones/1/playlist", "1");
        await PostAsync("/v1/zones/1/play/playlist/1/track", "1");
        await Task.Delay(3000);

        var radioPos1 = await GetAsync("/v1/zones/1/track/position");
        var radioProgress1 = await GetAsync("/v1/zones/1/track/progress");
        Console.WriteLine($"   📊 Radio - Position: {radioPos1}ms, Progress: {radioProgress1}");

        await Task.Delay(3000);
        var radioPos2 = await GetAsync("/v1/zones/1/track/position");
        var radioProgress2 = await GetAsync("/v1/zones/1/track/progress");
        Console.WriteLine($"   📊 Radio - Position: {radioPos2}ms, Progress: {radioProgress2}");

        if (int.Parse(radioPos2) > int.Parse(radioPos1))
        {
            Console.WriteLine("   ✅ Radio track: position increasing correctly");
        }

        // Test Subsonic track progress
        Console.WriteLine("   🎵 Testing Subsonic track progress (playlist 2)...");
        await PutAsync("/v1/zones/1/playlist", "2");
        await PostAsync("/v1/zones/1/play/playlist/2/track", "1");

        Console.WriteLine("   ⏳ Waiting for Subsonic stream to buffer...");
        await Task.Delay(10000);

        var subPos1 = await GetAsync("/v1/zones/1/track/position");
        var subProgress1 = await GetAsync("/v1/zones/1/track/progress");
        Console.WriteLine($"   📊 Subsonic - Position: {subPos1}ms, Progress: {subProgress1}");

        await Task.Delay(3000);
        var subPos2 = await GetAsync("/v1/zones/1/track/position");
        var subProgress2 = await GetAsync("/v1/zones/1/track/progress");
        Console.WriteLine($"   📊 Subsonic - Position: {subPos2}ms, Progress: {subProgress2}");

        if (float.Parse(subProgress2) <= 0.0001f) // Use small threshold instead of exact 0
        {
            Console.WriteLine("   ❌ Subsonic progress is near 0 - position tracking is not working!");
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
    }
}
