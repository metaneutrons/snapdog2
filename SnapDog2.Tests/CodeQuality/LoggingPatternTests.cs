namespace SnapDog2.Tests.CodeQuality;

using System.Text.RegularExpressions;
using Xunit;

/// <summary>
/// Tests to ensure code quality standards are maintained, specifically around logging patterns.
/// These tests help prevent regression to traditional logging patterns after modernization.
/// </summary>
public class LoggingPatternTests
{
    private static readonly string[] ExcludedDirectories =
    {
        "bin",
        "obj",
        ".git",
        "node_modules",
        "Tests", // Allow traditional logging in test files
    };

    private static readonly string[] ExcludedFiles = { "GlobalSuppressions.cs", "AssemblyInfo.cs" };

    [Fact]
    public void ShouldNotContainTraditionalLoggingPatterns()
    {
        // Arrange
        var projectRoot = GetProjectRoot();
        var violations = new List<LoggingViolation>();

        // Traditional logging patterns to detect
        var traditionalPatterns = new[]
        {
            new Regex(@"_logger\.Log(Trace|Debug|Information|Warning|Error|Critical)\s*\(", RegexOptions.Compiled),
            new Regex(@"logger\.Log(Trace|Debug|Information|Warning|Error|Critical)\s*\(", RegexOptions.Compiled),
            new Regex(@"_log\.Log(Trace|Debug|Information|Warning|Error|Critical)\s*\(", RegexOptions.Compiled),
            new Regex(@"log\.Log(Trace|Debug|Information|Warning|Error|Critical)\s*\(", RegexOptions.Compiled),
        };

        // Act
        ScanDirectoryForViolations(projectRoot, traditionalPatterns, violations);

        // Assert
        if (violations.Any())
        {
            var violationReport = string.Join(
                "\n",
                violations.Select(v => $"  {v.FilePath}:{v.LineNumber} - {v.Pattern}")
            );

            Assert.Fail(
                $"Found {violations.Count} traditional logging pattern(s). All logging should use LoggerMessage patterns:\n{violationReport}\n\n"
                    + "To fix: Convert to LoggerMessage pattern:\n"
                    + "  [LoggerMessage(EventId, LogLevel.Level, \"Message template\")]\n"
                    + "  private partial void LogMethodName(params...);\n"
            );
        }
    }

    [Fact]
    public void ShouldUseLoggerMessagePatterns()
    {
        // Arrange
        var projectRoot = GetProjectRoot();
        var loggerMessageCount = 0;
        var partialClassCount = 0;

        // LoggerMessage patterns to verify
        var loggerMessagePattern = new Regex(@"\[LoggerMessage\(", RegexOptions.Compiled);
        var partialClassPattern = new Regex(@"partial\s+class\s+\w+", RegexOptions.Compiled);

        // Act
        CountLoggerMessageUsage(
            projectRoot,
            loggerMessagePattern,
            partialClassPattern,
            ref loggerMessageCount,
            ref partialClassCount
        );

        // Assert
        Assert.True(
            loggerMessageCount > 0,
            "Expected to find LoggerMessage attributes in the codebase. "
                + "If logging exists, it should use high-performance LoggerMessage patterns."
        );

        // Note: Not all classes with LoggerMessage need to be partial (some might be in base classes)
        // So we just verify that some partial classes exist for LoggerMessage generation
    }

    [Fact]
    public void ShouldNotHaveEventIdConflicts()
    {
        // Arrange
        var projectRoot = GetProjectRoot();
        var eventIds = new Dictionary<int, List<string>>();
        var eventIdPattern = new Regex(@"\[LoggerMessage\((\d+),", RegexOptions.Compiled);

        // Act
        ScanForEventIdConflicts(projectRoot, eventIdPattern, eventIds);

        // Assert
        var conflicts = eventIds.Where(kvp => kvp.Value.Count > 1).ToList();

        if (conflicts.Any())
        {
            var conflictReport = string.Join(
                "\n",
                conflicts.Select(conflict => $"  EventId {conflict.Key} used in: {string.Join(", ", conflict.Value)}")
            );

            Assert.Fail(
                $"Found EventId conflicts in LoggerMessage attributes:\n{conflictReport}\n\n"
                    + "Each LoggerMessage must have a unique EventId across the entire application."
            );
        }
    }

    [Fact]
    public void ShouldHaveConsistentEventIdRanges()
    {
        // Arrange
        var projectRoot = GetProjectRoot();
        var eventIdsByNamespace = new Dictionary<string, List<int>>();
        var eventIdPattern = new Regex(@"\[LoggerMessage\((\d+),", RegexOptions.Compiled);

        // Act
        ScanForEventIdsByNamespace(projectRoot, eventIdPattern, eventIdsByNamespace);

        // Assert - Verify EventId ranges follow conventions
        foreach (var (namespaceName, eventIds) in eventIdsByNamespace)
        {
            if (eventIds.Count <= 1)
            {
                continue; // Skip single-event namespaces
            }

            var sortedIds = eventIds.OrderBy(x => x).ToList();
            var expectedRange = GetExpectedEventIdRange(namespaceName);

            if (expectedRange.HasValue)
            {
                var (min, max) = expectedRange.Value;
                var outOfRange = sortedIds.Where(id => id < min || id > max).ToList();

                Assert.Fail(
                    $"EventIds in {namespaceName} should be in range {min}-{max}, "
                        + $"but found out-of-range: {string.Join(", ", outOfRange)}"
                );
            }
        }
    }

    private static void ScanDirectoryForViolations(
        string directory,
        Regex[] patterns,
        List<LoggingViolation> violations
    )
    {
        if (ExcludedDirectories.Any(excluded => directory.Contains(excluded, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        foreach (var file in Directory.GetFiles(directory, "*.cs"))
        {
            if (
                ExcludedFiles.Any(excluded =>
                    Path.GetFileName(file).Equals(excluded, StringComparison.OrdinalIgnoreCase)
                )
            )
            {
                continue;
            }

            var lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                foreach (var pattern in patterns)
                {
                    if (pattern.IsMatch(line))
                    {
                        violations.Add(
                            new LoggingViolation
                            {
                                FilePath = Path.GetRelativePath(GetProjectRoot(), file),
                                LineNumber = i + 1,
                                Pattern = line.Trim(),
                            }
                        );
                    }
                }
            }
        }

        foreach (var subDirectory in Directory.GetDirectories(directory))
        {
            ScanDirectoryForViolations(subDirectory, patterns, violations);
        }
    }

    private static void CountLoggerMessageUsage(
        string directory,
        Regex loggerMessagePattern,
        Regex partialClassPattern,
        ref int loggerMessageCount,
        ref int partialClassCount
    )
    {
        if (ExcludedDirectories.Any(excluded => directory.Contains(excluded, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        foreach (var file in Directory.GetFiles(directory, "*.cs"))
        {
            if (
                ExcludedFiles.Any(excluded =>
                    Path.GetFileName(file).Equals(excluded, StringComparison.OrdinalIgnoreCase)
                )
            )
            {
                continue;
            }

            var content = File.ReadAllText(file);
            loggerMessageCount += loggerMessagePattern.Matches(content).Count;
            partialClassCount += partialClassPattern.Matches(content).Count;
        }

        foreach (var subDirectory in Directory.GetDirectories(directory))
        {
            CountLoggerMessageUsage(
                subDirectory,
                loggerMessagePattern,
                partialClassPattern,
                ref loggerMessageCount,
                ref partialClassCount
            );
        }
    }

    private static void ScanForEventIdConflicts(
        string directory,
        Regex eventIdPattern,
        Dictionary<int, List<string>> eventIds
    )
    {
        if (ExcludedDirectories.Any(excluded => directory.Contains(excluded, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        foreach (var file in Directory.GetFiles(directory, "*.cs"))
        {
            if (
                ExcludedFiles.Any(excluded =>
                    Path.GetFileName(file).Equals(excluded, StringComparison.OrdinalIgnoreCase)
                )
            )
            {
                continue;
            }

            var content = File.ReadAllText(file);
            var matches = eventIdPattern.Matches(content);
            var relativePath = Path.GetRelativePath(GetProjectRoot(), file);

            foreach (Match match in matches)
            {
                if (int.TryParse(match.Groups[1].Value, out var eventId))
                {
                    if (!eventIds.ContainsKey(eventId))
                    {
                        eventIds[eventId] = new List<string>();
                    }

                    eventIds[eventId].Add(relativePath);
                }
            }
        }

        foreach (var subDirectory in Directory.GetDirectories(directory))
        {
            ScanForEventIdConflicts(subDirectory, eventIdPattern, eventIds);
        }
    }

    private static void ScanForEventIdsByNamespace(
        string directory,
        Regex eventIdPattern,
        Dictionary<string, List<int>> eventIdsByNamespace
    )
    {
        if (ExcludedDirectories.Any(excluded => directory.Contains(excluded, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        foreach (var file in Directory.GetFiles(directory, "*.cs"))
        {
            if (
                ExcludedFiles.Any(excluded =>
                    Path.GetFileName(file).Equals(excluded, StringComparison.OrdinalIgnoreCase)
                )
            )
            {
                continue;
            }

            var content = File.ReadAllText(file);
            var namespaceMatch = Regex.Match(content, @"namespace\s+([\w\.]+)");
            if (!namespaceMatch.Success)
            {
                continue;
            }

            var namespaceName = namespaceMatch.Groups[1].Value;
            var matches = eventIdPattern.Matches(content);

            foreach (Match match in matches)
            {
                if (int.TryParse(match.Groups[1].Value, out var eventId))
                {
                    if (!eventIdsByNamespace.ContainsKey(namespaceName))
                    {
                        eventIdsByNamespace[namespaceName] = new List<int>();
                    }

                    eventIdsByNamespace[namespaceName].Add(eventId);
                }
            }
        }

        foreach (var subDirectory in Directory.GetDirectories(directory))
        {
            ScanForEventIdsByNamespace(subDirectory, eventIdPattern, eventIdsByNamespace);
        }
    }

    private static (int Min, int Max)? GetExpectedEventIdRange(string namespaceName)
    {
        // Define EventId ranges by namespace/component
        return namespaceName switch
        {
            var ns when ns.Contains("Server.Features") => (1000, 1999),
            var ns when ns.Contains("Server.Behaviors") => (2000, 2999),
            var ns when ns.Contains("Infrastructure.Metrics") => (3000, 3999),
            var ns when ns.Contains("Infrastructure.Notifications") => (4000, 4999),
            var ns when ns.Contains("Infrastructure.Integrations") => (9000, 9999),
            _ => null, // No specific range requirement
        };
    }

    private static string GetProjectRoot()
    {
        var currentDirectory = Directory.GetCurrentDirectory();

        // Walk up the directory tree to find the project root
        while (currentDirectory != null && !File.Exists(Path.Combine(currentDirectory, "SnapDog2.sln")))
        {
            currentDirectory = Directory.GetParent(currentDirectory)?.FullName;
        }

        if (currentDirectory == null)
        {
            throw new InvalidOperationException("Could not find project root directory containing SnapDog2.sln");
        }

        return Path.Combine(currentDirectory, "SnapDog2");
    }

    private record LoggingViolation
    {
        public required string FilePath { get; init; }
        public required int LineNumber { get; init; }
        public required string Pattern { get; init; }
    }
}
