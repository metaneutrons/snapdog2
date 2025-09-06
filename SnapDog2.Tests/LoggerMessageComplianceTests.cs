using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Xunit;
using RegexMatch = System.Text.RegularExpressions.Match;

namespace SnapDog2.Tests;

public class LoggerMessageComplianceTests
{
    [Fact]
    public void AllEventIds_ShouldBeInCorrectRanges()
    {
        var violations = new List<string>();
        var files = Directory.GetFiles("../../../..", "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains("obj") && !f.Contains("Tests"));

        foreach (var file in files)
        {
            var content = File.ReadAllText(file);
            var matches = Regex.Matches(content, @"EventId\s*=\s*(\d+)");

            foreach (RegexMatch match in matches)
            {
                var eventId = int.Parse(match.Groups[1].Value);
                var category = GetExpectedCategory(file);
                var expectedRange = GetCategoryRange(category);

                if (eventId < expectedRange.Min || eventId > expectedRange.Max)
                {
                    violations.Add($"EventId {eventId} in {Path.GetFileName(file)} should be in {category} range ({expectedRange.Min}-{expectedRange.Max})");
                }
            }
        }

        Assert.Empty(violations);
    }

    [Fact]
    public void LoggerMessages_ShouldNotContainProhibitedIcons()
    {
        var prohibitedIcons = new[] { "⏭️", "🎧", "🗑️", "🚨", "🔧", "🎵", "🎶", "📱", "🔊" };
        var allowedIcons = new[] { "✅", "❌", "⚠️", "⚡" };
        var violations = new List<string>();

        var files = Directory.GetFiles("../../../..", "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains("obj") && !f.Contains("Tests"));

        foreach (var file in files)
        {
            var content = File.ReadAllText(file);
            var messageMatches = Regex.Matches(content, @"Message\s*=\s*""([^""]+)""");

            foreach (RegexMatch match in messageMatches)
            {
                var message = match.Groups[1].Value;
                foreach (var icon in prohibitedIcons)
                {
                    if (message.Contains(icon))
                    {
                        violations.Add($"Prohibited icon '{icon}' found in {Path.GetFileName(file)}: {message}");
                    }
                }
            }
        }

        Assert.Empty(violations);
    }

    [Fact]
    public void EventIds_ShouldBeUnique()
    {
        var eventIds = new Dictionary<int, List<string>>();
        var files = Directory.GetFiles("../../../..", "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains("obj") && !f.Contains("Tests"));

        foreach (var file in files)
        {
            var content = File.ReadAllText(file);
            var matches = Regex.Matches(content, @"EventId\s*=\s*(\d+)");

            foreach (RegexMatch match in matches)
            {
                var eventId = int.Parse(match.Groups[1].Value);
                if (!eventIds.ContainsKey(eventId))
                {
                    eventIds[eventId] = new List<string>();
                }

                eventIds[eventId].Add(Path.GetFileName(file));
            }
        }

        var duplicates = eventIds.Where(kvp => kvp.Value.Count > 1).ToList();
        Assert.Empty(duplicates.Select(d => $"EventId {d.Key} used in: {string.Join(", ", d.Value)}"));
    }

    private static string GetExpectedCategory(string filePath)
    {
        if (filePath.Contains("Domain/Services"))
        {
            return "Domain";
        }

        if (filePath.Contains("Application/Services"))
        {
            return "Application";
        }

        if (filePath.Contains("Server/"))
        {
            return "Server";
        }

        if (filePath.Contains("Api/"))
        {
            return "Api";
        }

        if (filePath.Contains("Infrastructure/Services"))
        {
            return "Infrastructure";
        }

        if (filePath.Contains("Infrastructure/Integrations"))
        {
            return "Integration";
        }

        if (filePath.Contains("Audio") || filePath.Contains("Media"))
        {
            return "Audio";
        }

        if (filePath.Contains("Metrics") || filePath.Contains("Performance"))
        {
            return "Metrics";
        }

        if (filePath.Contains("Notification"))
        {
            return "Notifications";
        }

        return "Infrastructure";
    }

    private static (int Min, int Max) GetCategoryRange(string category) => category switch
    {
        "Domain" => (10000, 10999),
        "Application" => (11000, 11999),
        "Server" => (12000, 12999),
        "Api" => (13000, 13999),
        "Infrastructure" => (14000, 14999),
        "Integration" => (15000, 15999),
        "Audio" => (16000, 16999),
        "Notifications" => (17000, 17999),
        "Metrics" => (18000, 18999),
        _ => (14000, 14999)
    };
}
