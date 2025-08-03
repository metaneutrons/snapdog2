using System.CommandLine;
using SnapDog2.Helpers;

namespace SnapDog2.CommandLine;

/// <summary>
/// Command-line parser for SnapDog2 using System.CommandLine natively
/// </summary>
public static class SnapDogCommandLineParser
{
    /// <summary>
    /// Creates the root command with all options and handlers
    /// </summary>
    public static RootCommand CreateRootCommand()
    {
        var rootCommand = new RootCommand("SnapDog2 - Smart Home Audio Controller");

        // Environment file option
        var envFileOption = new Option<string?>("--env-file");
        envFileOption.Description = "Path to environment file to load (.env format)";
        envFileOption.Aliases.Add("-e");
        rootCommand.Add(envFileOption);

        // Set the main handler for the root command
        rootCommand.SetAction(
            (ParseResult parseResult) =>
            {
                // This is normal execution - extract options and continue
                var options = new SnapDogOptions
                {
                    EnvFile = parseResult.GetValue(envFileOption),
                    // Future options:
                    // LogLevel = parseResult.GetValue(logLevelOption),
                    // HttpPort = parseResult.GetValue(httpPortOption)
                };

                // Load environment file if specified
                if (!string.IsNullOrEmpty(options.EnvFile))
                {
                    try
                    {
                        LoadEnvironmentFile(options.EnvFile);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Failed to load environment file: {ex.Message}");
                        return 1;
                    }
                }

                // Signal that we should continue with normal application startup
                return -1; // Special code meaning "continue with app"
            }
        );

        // Future options can be added here:
        // var logLevelOption = new Option<string?>("--log-level");
        // logLevelOption.Description = "Override log level";
        // logLevelOption.Aliases.Add("-l");
        // rootCommand.Add(logLevelOption);

        return rootCommand;
    }

    /// <summary>
    /// Executes command line parsing and returns options if app should continue
    /// </summary>
    public static SnapDogOptions? ExecuteCommandLine(string[] args)
    {
        // Handle version manually before System.CommandLine processes it
        if (args.Contains("--version"))
        {
            ShowVersionInfo();
            Environment.Exit(0);
        }

        var rootCommand = CreateRootCommand();
        var parseResult = rootCommand.Parse(args);
        var exitCode = parseResult.Invoke();

        // Exit codes:
        // 0 = success and exit (help, etc.)
        // 1 = error and exit
        // -1 = continue with normal app execution
        if (exitCode == -1)
        {
            // Extract options for normal execution
            var envFileOption = rootCommand.Options.OfType<Option<string?>>().First(o => o.Name == "env-file");

            return new SnapDogOptions { EnvFile = parseResult.GetValue(envFileOption) };
        }

        // Exit with the code from System.CommandLine
        Environment.Exit(exitCode);
        return null; // Never reached
    }

    /// <summary>
    /// Shows comprehensive version information
    /// </summary>
    private static void ShowVersionInfo()
    {
        var versionInfo = GitVersionHelper.GetVersionInfo();

        Console.WriteLine($"SnapDog2 {versionInfo.FullSemVer}");
        Console.WriteLine();
        Console.WriteLine("Version Information:");
        Console.WriteLine($"  Version: {versionInfo.FullSemVer}");
        Console.WriteLine($"  Branch: {versionInfo.BranchName}");
        Console.WriteLine($"  Commit: {versionInfo.ShortSha} ({versionInfo.CommitDate})");
        Console.WriteLine($"  Build: {versionInfo.FullBuildMetaData}");

        if (versionInfo.UncommittedChanges > 0)
        {
            Console.WriteLine($"  ⚠️  Uncommitted changes: {versionInfo.UncommittedChanges}");
        }

        Console.WriteLine();
        Console.WriteLine("Runtime Information:");
        Console.WriteLine($"  .NET: {Environment.Version}");
        Console.WriteLine($"  OS: {Environment.OSVersion}");
        Console.WriteLine(
            $"  Architecture: {Environment.ProcessorCount} cores, {System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture}"
        );
    }

    /// <summary>
    /// Loads environment variables from a file
    /// </summary>
    public static void LoadEnvironmentFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Environment file not found: {filePath}");
        }

        Console.WriteLine($"📁 Loading environment variables from: {filePath}");

        var loadedCount = 0;
        foreach (var line in File.ReadAllLines(filePath))
        {
            var trimmedLine = line.Trim();

            // Skip empty lines and comments
            if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith('#'))
                continue;

            var parts = trimmedLine.Split('=', 2);
            if (parts.Length == 2)
            {
                var key = parts[0].Trim();
                var value = parts[1].Trim();

                // Remove quotes if present
                if ((value.StartsWith('"') && value.EndsWith('"')) || (value.StartsWith('\'') && value.EndsWith('\'')))
                {
                    value = value[1..^1];
                }

                // Only set if not already set (command line and existing env vars take precedence)
                if (Environment.GetEnvironmentVariable(key) == null)
                {
                    Environment.SetEnvironmentVariable(key, value);
                    loadedCount++;
                }
            }
        }

        Console.WriteLine($"✅ Loaded {loadedCount} environment variables from {filePath}");
    }
}
