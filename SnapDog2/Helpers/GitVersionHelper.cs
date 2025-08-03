using System.Reflection;
using SnapDog2.Models;

namespace SnapDog2.Helpers;

/// <summary>
/// Helper class to extract GitVersion information from assembly metadata
/// </summary>
public static class GitVersionHelper
{
    /// <summary>
    /// Gets GitVersion information from the current assembly
    /// </summary>
    /// <returns>GitVersionInfo populated with current version data</returns>
    public static GitVersionInfo GetVersionInfo()
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Get the informational version which contains the full GitVersion info
        var informationalVersion =
            assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "Unknown";
        var assemblyVersion = assembly.GetName().Version?.ToString() ?? "Unknown";
        var fileVersion = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version ?? "Unknown";

        // Parse the informational version to extract GitVersion components
        // Format: "0.1.0-beta.1+Branch.develop.Sha.01bc21baa47be00212511efc5b773aa2df642b0a"
        var parts = informationalVersion.Split('+');
        var versionPart = parts.Length > 0 ? parts[0] : "Unknown";
        var metadataPart = parts.Length > 1 ? parts[1] : "";

        // Parse version part (e.g., "0.1.0-beta.1")
        var versionComponents = versionPart.Split('-');
        var baseVersion = versionComponents.Length > 0 ? versionComponents[0] : "0.0.0";
        var preReleaseTag = versionComponents.Length > 1 ? versionComponents[1] : "";

        // Parse base version (e.g., "0.1.0")
        var versionNumbers = baseVersion.Split('.');
        var major = versionNumbers.Length > 0 && int.TryParse(versionNumbers[0], out var maj) ? maj : 0;
        var minor = versionNumbers.Length > 1 && int.TryParse(versionNumbers[1], out var min) ? min : 0;
        var patch = versionNumbers.Length > 2 && int.TryParse(versionNumbers[2], out var pat) ? pat : 0;

        // Parse pre-release tag (e.g., "beta.1")
        var preReleaseComponents = preReleaseTag.Split('.');
        var preReleaseLabel = preReleaseComponents.Length > 0 ? preReleaseComponents[0] : "";
        var preReleaseNumber =
            preReleaseComponents.Length > 1 && int.TryParse(preReleaseComponents[1], out var preNum) ? preNum : 0;

        // Parse metadata (e.g., "Branch.develop.Sha.01bc21baa47be00212511efc5b773aa2df642b0a")
        var metadataComponents = metadataPart.Split('.');
        var branchName = "Unknown";
        var sha = "Unknown";
        var shortSha = "Unknown";

        if (metadataComponents.Length >= 4)
        {
            branchName = metadataComponents[1];
            sha = metadataComponents[3];
            shortSha = sha.Length > 7 ? sha.Substring(0, 7) : sha;
        }

        return new GitVersionInfo
        {
            AssemblySemFileVer = fileVersion,
            AssemblySemVer = assemblyVersion,
            BranchName = branchName,
            BuildMetaData = string.IsNullOrEmpty(metadataPart) ? null : metadataPart,
            CommitDate = GetAssemblyMetadata(assembly, "BuildDate") ?? "Unknown",
            CommitsSinceVersionSource = 1, // We can't easily determine this from the informational version
            EscapedBranchName = branchName,
            FullBuildMetaData = metadataPart,
            FullSemVer = versionPart,
            InformationalVersion = informationalVersion,
            Major = major,
            MajorMinorPatch = baseVersion,
            Minor = minor,
            Patch = patch,
            PreReleaseLabel = preReleaseLabel,
            PreReleaseLabelWithDash = string.IsNullOrEmpty(preReleaseLabel) ? "" : $"-{preReleaseLabel}",
            PreReleaseNumber = preReleaseNumber,
            PreReleaseTag = preReleaseTag,
            PreReleaseTagWithDash = string.IsNullOrEmpty(preReleaseTag) ? "" : $"-{preReleaseTag}",
            SemVer = versionPart,
            Sha = sha,
            ShortSha = shortSha,
            UncommittedChanges = 0, // We can't easily determine this from the informational version
            VersionSourceSha = "Unknown", // We can't easily determine this from the informational version
            WeightedPreReleaseNumber = preReleaseNumber,
        };
    }

    /// <summary>
    /// Gets assembly metadata value by key
    /// </summary>
    /// <param name="assembly">Assembly to search</param>
    /// <param name="key">Metadata key</param>
    /// <returns>Metadata value or null if not found</returns>
    private static string? GetAssemblyMetadata(Assembly assembly, string key)
    {
        return assembly.GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(attr => attr.Key == key)?.Value;
    }
}
