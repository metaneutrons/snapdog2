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
namespace SnapDog2.TestKit.Base;

using System.Reflection;

/// <summary>
/// Test runner that discovers and executes scenario tests via reflection.
/// </summary>
public class TestRunner
{
    private readonly string _baseUrl;
    private int _passedTests = 0;
    private int _failedTests = 0;
    private readonly List<string> _failedTestNames = [];

    public TestRunner(string baseUrl)
    {
        _baseUrl = baseUrl;
    }

    /// <summary>
    /// Discovers and runs all scenario tests in the Scenarios namespace.
    /// </summary>
    public async Task RunAllScenariosAsync()
    {
        Console.WriteLine("🎬 Scenario Tests");
        Console.WriteLine("─────────────────");

        var testTypes = DiscoverScenarioTests();

        foreach (var testType in testTypes)
        {
            await RunScenarioAsync(testType);
        }

        PrintResults();
    }

    /// <summary>
    /// Discovers all scenario test classes via reflection.
    /// </summary>
    private static List<Type> DiscoverScenarioTests()
    {
        return Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.Namespace?.StartsWith("SnapDog2.TestKit.Scenarios") == true)
            .Where(t => typeof(IScenarioTest).IsAssignableFrom(t))
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .OrderBy(t => t.Name)
            .ToList();
    }

    /// <summary>
    /// Runs a single scenario test.
    /// </summary>
    private async Task RunScenarioAsync(Type testType)
    {
        try
        {
            // Create instance with base URL parameter
            var test = (IScenarioTest)Activator.CreateInstance(testType, _baseUrl)!;

            // Print scenario header
            Console.WriteLine($"{test.Icon} {test.Name}");

            // Execute test
            var result = await test.ExecuteAsync();

            // Print result
            if (result)
            {
                Console.WriteLine($"✅ {test.Name}");
                _passedTests++;
            }
            else
            {
                Console.WriteLine(); // Add blank line before failed scenario
                Console.WriteLine($"❌ {test.Name} - Test condition failed");
                _failedTests++;
                _failedTestNames.Add($"{test.Name} - Test condition failed");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ {testType.Name} - Exception: {ex.Message}");
            _failedTests++;
            _failedTestNames.Add($"{testType.Name} - Exception: {ex.Message}");
        }
    }

    /// <summary>
    /// Prints the final test results summary.
    /// </summary>
    private void PrintResults()
    {
        Console.WriteLine();
        Console.WriteLine("📊 SCENARIO RESULTS");
        Console.WriteLine("═══════════════════");
        Console.WriteLine($"Total Tests:  {_passedTests + _failedTests}");
        Console.WriteLine($"✅ Passed:    {_passedTests}");
        Console.WriteLine($"❌ Failed:    {_failedTests}");

        if (_failedTests > 0)
        {
            Console.WriteLine();
            Console.WriteLine("❌ FAILED SCENARIOS:");
            foreach (var failedTest in _failedTestNames)
            {
                Console.WriteLine($"   • {failedTest}");
            }
        }
    }
}
