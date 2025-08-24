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
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace SnapDog.Analyzers.Tests;

public class SimpleAnalyzerTests
{
    [Fact]
    public void Analyzer_CanBeInstantiated()
    {
        var analyzer = new LoggerMessageAnalyzer();
        Assert.NotNull(analyzer);
        Assert.Equal(2, analyzer.SupportedDiagnostics.Length);
    }

    [Fact]
    public void Analyzer_HasCorrectDiagnosticIds()
    {
        var analyzer = new LoggerMessageAnalyzer();
        var diagnostics = analyzer.SupportedDiagnostics;

        Assert.Contains(diagnostics, d => d.Id == "SNAPDOG001");
        Assert.Contains(diagnostics, d => d.Id == "SNAPDOG002");
    }

    [Fact]
    public void Analyzer_DetectsPositionalParameters()
    {
        const string testCode =
            @"
using Microsoft.Extensions.Logging;

public partial class TestClass
{
    private readonly ILogger<TestClass> _logger;

    [LoggerMessage(1, LogLevel.Information, ""Test message"")]
    private partial void LogTest();
}";

        var diagnostics = GetDiagnostics(testCode);

        // Should detect positional parameters
        Assert.Contains(diagnostics, d => d.Id == "SNAPDOG001");
    }

    [Fact]
    public void Analyzer_AcceptsNamedParameters()
    {
        const string testCode =
            @"
using Microsoft.Extensions.Logging;

public partial class TestClass
{
    private readonly ILogger<TestClass> _logger;

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = ""Test message"")]
    private partial void LogTest();
}";

        var diagnostics = GetDiagnostics(testCode);

        // Should not detect any issues with named parameters
        Assert.DoesNotContain(diagnostics, d => d.Id == "SNAPDOG001");
    }

    [Fact]
    public void Analyzer_DetectsMethodNotAtEnd()
    {
        const string testCode =
            @"
using Microsoft.Extensions.Logging;

public partial class TestClass
{
    private readonly ILogger<TestClass> _logger;

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = ""Test message"")]
    private partial void LogTest();

    public void RegularMethod()
    {
    }
}";

        var diagnostics = GetDiagnostics(testCode);

        // Should detect method not at end
        Assert.Contains(diagnostics, d => d.Id == "SNAPDOG002");
    }

    private static Diagnostic[] GetDiagnostics(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) }
        );

        var analyzer = new LoggerMessageAnalyzer();
        var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));

        var diagnostics = compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().Result;
        return diagnostics.ToArray();
    }
}
