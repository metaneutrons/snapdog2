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
    [LoggerMessage(1, LogLevel.Information, "User {UserId} logged in")]
    private partial void LogUserLogin(int UserId);

    // ERROR: This LoggerMessage method is not at the end of the class (should trigger SNAPDOG002)
    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Failed to process {ItemId}")]
    private partial void LogProcessingFailed(string ItemId);

    // This is a regular method that comes after LoggerMessage methods
    public void DoSomething()
    {
        LogUserLogin(123);
        LogProcessingFailed("item-456");
        LogCorrectMethod();
    }

    // ERROR: Mixed positional and named parameters (should trigger SNAPDOG001)
    [LoggerMessage(3, Level = LogLevel.Error, Message = "Critical error occurred")]
    private partial void LogCriticalError();

    // This method is also not at the end (should trigger SNAPDOG002)
    [LoggerMessage(EventId = 4, Level = LogLevel.Debug, Message = "Debug info: {Details}")]
    private partial void LogDebugInfo(string Details);

    // Another regular method
    public void AnotherMethod()
    {
        // Some code
    }

    // CORRECT: This LoggerMessage method is at the end and uses named parameters
    [LoggerMessage(EventId = 5, Level = LogLevel.Information, Message = "Operation completed successfully")]
    private partial void LogCorrectMethod();
}
