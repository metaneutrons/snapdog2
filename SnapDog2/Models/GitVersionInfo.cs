namespace SnapDog2.Models;

/// <summary>
/// Contains GitVersion information for the application
/// </summary>
public class GitVersionInfo
{
    public string AssemblySemFileVer { get; set; } = string.Empty;
    public string AssemblySemVer { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string? BuildMetaData { get; set; }
    public string CommitDate { get; set; } = string.Empty;
    public int CommitsSinceVersionSource { get; set; }
    public string EscapedBranchName { get; set; } = string.Empty;
    public string FullBuildMetaData { get; set; } = string.Empty;
    public string FullSemVer { get; set; } = string.Empty;
    public string InformationalVersion { get; set; } = string.Empty;
    public int Major { get; set; }
    public string MajorMinorPatch { get; set; } = string.Empty;
    public int Minor { get; set; }
    public int Patch { get; set; }
    public string PreReleaseLabel { get; set; } = string.Empty;
    public string PreReleaseLabelWithDash { get; set; } = string.Empty;
    public int PreReleaseNumber { get; set; }
    public string PreReleaseTag { get; set; } = string.Empty;
    public string PreReleaseTagWithDash { get; set; } = string.Empty;
    public string SemVer { get; set; } = string.Empty;
    public string Sha { get; set; } = string.Empty;
    public string ShortSha { get; set; } = string.Empty;
    public int UncommittedChanges { get; set; }
    public string VersionSourceSha { get; set; } = string.Empty;
    public int WeightedPreReleaseNumber { get; set; }
}
