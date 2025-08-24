using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace SnapDog.Analyzers.Tests;

public class AnalyzerTestFileTests
{
    private const string TestCode =
        @"
using Microsoft.Extensions.Logging;

namespace AnalyzerTest;

public partial class TestService
{
    private readonly ILogger<TestService> _logger;

    public TestService(ILogger<TestService> logger)
    {
        _logger = logger;
    }

    // ERROR: This LoggerMessage uses positional parameters (should trigger SNAPDOG001)
    [LoggerMessage(1, LogLevel.Information, ""User {UserId} logged in"")]
    private partial void LogUserLogin(int UserId);

    // ERROR: This LoggerMessage method is not at the end of the class (should trigger SNAPDOG002)
    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = ""Failed to process {ItemId}"")]
    private partial void LogProcessingFailed(string ItemId);

    // This is a regular method that comes after LoggerMessage methods
    public void DoSomething()
    {
        LogUserLogin(123);
        LogProcessingFailed(""item-456"");
        LogCorrectMethod();
    }

    // ERROR: Mixed positional and named parameters (should trigger SNAPDOG001)
    [LoggerMessage(3, Level = LogLevel.Error, Message = ""Critical error occurred"")]
    private partial void LogCriticalError();

    // This method is also not at the end (should trigger SNAPDOG002)
    [LoggerMessage(EventId = 4, Level = LogLevel.Debug, Message = ""Debug info: {Details}"")]
    private partial void LogDebugInfo(string Details);

    // Another regular method
    public void AnotherMethod()
    {
        // Some code
    }

    // CORRECT: This LoggerMessage method is at the end and uses named parameters
    [LoggerMessage(EventId = 5, Level = LogLevel.Information, Message = ""Operation completed successfully"")]
    private partial void LogCorrectMethod();
}";

    [Fact]
    public void AnalyzerTestFile_ShouldDetectAllExpectedErrors()
    {
        var diagnostics = GetDiagnostics(TestCode);

        // Should detect multiple SNAPDOG001 errors (positional parameters)
        var snapdog001Diagnostics = diagnostics.Where(d => d.Id == "SNAPDOG001").ToArray();
        Assert.True(
            snapdog001Diagnostics.Length >= 2,
            $"Expected at least 2 SNAPDOG001 diagnostics, but found {snapdog001Diagnostics.Length}"
        );

        // Should detect multiple SNAPDOG002 errors (methods not at end)
        var snapdog002Diagnostics = diagnostics.Where(d => d.Id == "SNAPDOG002").ToArray();
        Assert.True(
            snapdog002Diagnostics.Length >= 2,
            $"Expected at least 2 SNAPDOG002 diagnostics, but found {snapdog002Diagnostics.Length}"
        );

        // Print all diagnostics for debugging
        foreach (var diagnostic in diagnostics)
        {
            var lineSpan = diagnostic.Location.GetLineSpan();
            System.Console.WriteLine(
                $"{diagnostic.Id}: {diagnostic.GetMessage()} at line {lineSpan.StartLinePosition.Line + 1}"
            );
        }
    }

    [Fact]
    public void AnalyzerTestFile_ShouldDetectSpecificErrors()
    {
        var diagnostics = GetDiagnostics(TestCode);

        // Check for specific expected errors
        var allDiagnostics = diagnostics.ToArray();

        // Should find positional parameters error on LogUserLogin (line ~16)
        Assert.Contains(
            allDiagnostics,
            d =>
                d.Id == "SNAPDOG001"
                && d.Location.GetLineSpan().StartLinePosition.Line >= 15
                && d.Location.GetLineSpan().StartLinePosition.Line <= 17
        );

        // Should find mixed parameters error on LogCriticalError (line ~31)
        Assert.Contains(
            allDiagnostics,
            d =>
                d.Id == "SNAPDOG001"
                && d.Location.GetLineSpan().StartLinePosition.Line >= 30
                && d.Location.GetLineSpan().StartLinePosition.Line <= 33
        );

        // Should find method positioning errors
        Assert.Contains(allDiagnostics, d => d.Id == "SNAPDOG002");
    }

    private static Diagnostic[] GetDiagnostics(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        // Create basic references
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Console).Assembly.Location),
        };

        var compilation = CSharpCompilation.Create("TestAssembly", new[] { syntaxTree }, references);

        var analyzer = new LoggerMessageAnalyzer();
        var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));

        var diagnostics = compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().Result;
        return diagnostics.ToArray();
    }
}
