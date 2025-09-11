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
using SnapDog2.TestKit.Base;

namespace SnapDog2.TestKit;

/// <summary>
/// SnapDog2 TestKit - Comprehensive scenario testing for the SnapDog2 API.
/// </summary>
internal static class Program
{
    private static async Task Main(string[] args)
    {
        Console.WriteLine("⚠️  WARNING: SnapDog2 TestKit expects values from the dev container setup.");
        Console.WriteLine("   Tests may fail if run against a different environment setup.");
        Console.WriteLine();
        Console.WriteLine();

        var baseUrl = "http://localhost:8000/api";
        var testRunner = new TestRunner(baseUrl);

        if (args.Contains("--scenarios-only"))
        {
            await testRunner.RunAllScenariosAsync();
        }
        else
        {
            Console.WriteLine("🚀 SnapDog2 TestKit");
            Console.WriteLine("═══════════════════");
            Console.WriteLine("Available options:");
            Console.WriteLine("  --scenarios-only    Run scenario tests only");
            Console.WriteLine();
            Console.WriteLine("Running scenario tests by default...");
            Console.WriteLine();

            await testRunner.RunAllScenariosAsync();
        }
    }
}
