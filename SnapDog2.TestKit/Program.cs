using System.CommandLine;
using System.CommandLine.Parsing;
using SnapDog2.TestKit;

// Create options using modern System.CommandLine pattern
Option<string> urlOption = new("--url", "-u")
{
    Description = "Base URL for the SnapDog2 API",
    DefaultValueFactory = _ => "http://localhost:8000/api",
};

Option<bool> failEarlyOption = new("--fail-early", "-f")
{
    Description = "Stop immediately when a test fails",
};

Option<bool> baseOnlyOption = new("--base-only", "-b")
{
    Description = "Run only base API tests, skip scenario tests",
};

Option<bool> scenariosOnlyOption = new("--scenarios-only", "-s")
{
    Description = "Run only scenario tests, skip base tests",
};

// Create root command using modern pattern
RootCommand rootCommand = new("SnapDog2 TestKit - Comprehensive API Testing Suite");
rootCommand.Options.Add(urlOption);
rootCommand.Options.Add(failEarlyOption);
rootCommand.Options.Add(baseOnlyOption);
rootCommand.Options.Add(scenariosOnlyOption);

// Parse and handle commands
var parseResult = rootCommand.Parse(args);

// Check if parsing failed or help was requested
if (parseResult.Errors.Count > 0 || args.Contains("--help") || args.Contains("-h"))
{
    var result = parseResult.Invoke();
    return result;
}

// Extract parsed values
string url = parseResult.GetValue(urlOption) ?? "http://localhost:8000/api";
bool failEarly = parseResult.GetValue(failEarlyOption);
bool baseOnly = parseResult.GetValue(baseOnlyOption);
bool scenariosOnly = parseResult.GetValue(scenariosOnlyOption);

// Display warning message with icon
Console.WriteLine("⚠️  WARNING: SnapDog2 TestKit expects values from the dev container setup.");
Console.WriteLine("   Tests may fail if run against a different environment setup.");
Console.WriteLine();

var exitCode = 0;

// Run base tests (unless scenarios-only)
if (!scenariosOnly)
{
    var testRunner = new ApiTestRunner(url, failEarly);
    exitCode = await testRunner.RunAllTestsAsync();
}

// Run scenario tests (unless base-only, and only if base tests passed)
if (!baseOnly && exitCode == 0)
{
    var scenarioRunner = new ScenarioTestRunner(url);
    await scenarioRunner.RunScenariosAsync();
    scenarioRunner.Dispose();
}

return exitCode;
