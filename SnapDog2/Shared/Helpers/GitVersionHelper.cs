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

namespace SnapDog2.Shared.Helpers;

using SnapDog2.Shared.Models;

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
