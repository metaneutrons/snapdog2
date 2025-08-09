using System.Reflection;
using SnapDog2.Core.Models;

namespace SnapDog2.Helpers;

/// <summary>
/// Helper class to extract GitVersion information from generated constants
/// </summary>
public static class GitVersionHelper
{
    /// <summary>
    /// Gets GitVersion information from the generated GitVersionInformation class
    /// </summary>
    /// <returns>GitVersionInfo populated with current version data</returns>
    public static GitVersionInfo GetVersionInfo()
    {
        var assembly = Assembly.GetExecutingAssembly();

        return new GitVersionInfo
        {
            AssemblySemFileVer = GitVersionInformation.AssemblySemFileVer,
            AssemblySemVer = GitVersionInformation.AssemblySemVer,
            BranchName = GitVersionInformation.BranchName,
            BuildMetaData = string.IsNullOrEmpty(GitVersionInformation.BuildMetaData)
                ? null
                : GitVersionInformation.BuildMetaData,
            CommitDate = GitVersionInformation.CommitDate,
            CommitsSinceVersionSource = int.Parse(GitVersionInformation.CommitsSinceVersionSource),
            EscapedBranchName = GitVersionInformation.EscapedBranchName,
            FullBuildMetaData = GitVersionInformation.FullBuildMetaData,
            FullSemVer = GitVersionInformation.FullSemVer,
            InformationalVersion = GitVersionInformation.InformationalVersion,
            Major = int.Parse(GitVersionInformation.Major),
            MajorMinorPatch = GitVersionInformation.MajorMinorPatch,
            Minor = int.Parse(GitVersionInformation.Minor),
            Patch = int.Parse(GitVersionInformation.Patch),
            PreReleaseLabel = GitVersionInformation.PreReleaseLabel,
            PreReleaseLabelWithDash = GitVersionInformation.PreReleaseLabelWithDash,
            PreReleaseNumber = int.Parse(GitVersionInformation.PreReleaseNumber),
            PreReleaseTag = GitVersionInformation.PreReleaseTag,
            PreReleaseTagWithDash = GitVersionInformation.PreReleaseTagWithDash,
            SemVer = GitVersionInformation.SemVer,
            Sha = GitVersionInformation.Sha,
            ShortSha = GitVersionInformation.ShortSha,
            UncommittedChanges = int.Parse(GitVersionInformation.UncommittedChanges),
            VersionSourceSha = GitVersionInformation.VersionSourceSha,
            WeightedPreReleaseNumber = int.Parse(GitVersionInformation.WeightedPreReleaseNumber),
        };
    }
}
