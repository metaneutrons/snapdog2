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
namespace SnapDog2.Shared.Models;

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
