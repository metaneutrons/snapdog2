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

using Microsoft.CodeAnalysis.CodeFixes;
using Xunit;

namespace SnapDog.Analyzers.Tests;

public class LoggerMessageCodeFixTests
{
    [Fact]
    public void CodeFixProvider_CanBeInstantiated()
    {
        var codeFixProvider = new LoggerMessageCodeFixProvider();
        Assert.NotNull(codeFixProvider);
    }

    [Fact]
    public void CodeFixProvider_SupportsCorrectDiagnosticIds()
    {
        var codeFixProvider = new LoggerMessageCodeFixProvider();
        var fixableDiagnosticIds = codeFixProvider.FixableDiagnosticIds;

        Assert.Contains("SNAPDOG001", fixableDiagnosticIds);
        Assert.Contains("SNAPDOG002", fixableDiagnosticIds);
        Assert.Equal(2, fixableDiagnosticIds.Length);
    }

    [Fact]
    public void CodeFixProvider_HasBatchFixAllProvider()
    {
        var codeFixProvider = new LoggerMessageCodeFixProvider();
        var fixAllProvider = codeFixProvider.GetFixAllProvider();

        Assert.NotNull(fixAllProvider);
        // Verify it's the batch fixer - check that it's the same as WellKnownFixAllProviders.BatchFixer
        Assert.Same(WellKnownFixAllProviders.BatchFixer, fixAllProvider);
    }

    [Fact]
    public void LoggerMessageAnalyzer_AndCodeFixProvider_HaveMatchingDiagnosticIds()
    {
        var analyzer = new LoggerMessageAnalyzer();
        var codeFixProvider = new LoggerMessageCodeFixProvider();

        var analyzerDiagnosticIds = analyzer.SupportedDiagnostics.Select(d => d.Id).ToArray();
        var codeFixDiagnosticIds = codeFixProvider.FixableDiagnosticIds.ToArray();

        // Verify that all analyzer diagnostics have corresponding code fixes
        foreach (var analyzerDiagnosticId in analyzerDiagnosticIds)
        {
            Assert.Contains(analyzerDiagnosticId, codeFixDiagnosticIds);
        }

        // Verify that all code fix diagnostics have corresponding analyzer rules
        foreach (var codeFixDiagnosticId in codeFixDiagnosticIds)
        {
            Assert.Contains(codeFixDiagnosticId, analyzerDiagnosticIds);
        }
    }

    [Fact]
    public void CodeFixProvider_Integration_WithAnalyzer()
    {
        // This is a basic integration test to ensure the CodeFixProvider
        // can work with diagnostics from the analyzer
        var analyzer = new LoggerMessageAnalyzer();
        var codeFixProvider = new LoggerMessageCodeFixProvider();

        // Test that we have the expected diagnostic rules
        var useNamedParametersRule = analyzer.SupportedDiagnostics.FirstOrDefault(d => d.Id == "SNAPDOG001");
        var moveToEndRule = analyzer.SupportedDiagnostics.FirstOrDefault(d => d.Id == "SNAPDOG002");

        Assert.NotNull(useNamedParametersRule);
        Assert.NotNull(moveToEndRule);

        // Test that the CodeFixProvider can handle these diagnostic IDs
        Assert.Contains("SNAPDOG001", codeFixProvider.FixableDiagnosticIds);
        Assert.Contains("SNAPDOG002", codeFixProvider.FixableDiagnosticIds);

        // Verify diagnostic properties
        Assert.Equal("LoggerMessage should use named parameters", useNamedParametersRule.Title.ToString());
        Assert.Equal("LoggerMessage methods should be moved to end of class", moveToEndRule.Title.ToString());
    }
}
