using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace SnapDog2.Tests.Helpers.Extensions;

/// <summary>
/// Enterprise-grade extensions for ITestOutputHelper to provide structured logging
/// </summary>
public static class TestOutputExtensions
{
    /// <summary>
    /// Writes a formatted log message with timestamp and level
    /// </summary>
    public static void WriteLog(this ITestOutputHelper output, LogLevel level, string message, params object[] args)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var levelString = level.ToString().ToUpperInvariant();
        var formattedMessage = args.Length > 0 ? string.Format(message, args) : message;

        output.WriteLine($"[{timestamp}] [{levelString}] {formattedMessage}");
    }

    /// <summary>
    /// Writes an information log message
    /// </summary>
    public static void WriteInfo(this ITestOutputHelper output, string message, params object[] args)
    {
        output.WriteLog(LogLevel.Information, message, args);
    }

    /// <summary>
    /// Writes a warning log message
    /// </summary>
    public static void WriteWarning(this ITestOutputHelper output, string message, params object[] args)
    {
        output.WriteLog(LogLevel.Warning, message, args);
    }

    /// <summary>
    /// Writes an error log message
    /// </summary>
    public static void WriteError(this ITestOutputHelper output, string message, params object[] args)
    {
        output.WriteLog(LogLevel.Error, message, args);
    }

    /// <summary>
    /// Writes a debug log message
    /// </summary>
    public static void WriteDebug(this ITestOutputHelper output, string message, params object[] args)
    {
        output.WriteLog(LogLevel.Debug, message, args);
    }

    /// <summary>
    /// Writes an object as formatted JSON
    /// </summary>
    public static void WriteJson<T>(this ITestOutputHelper output, T obj, string? prefix = null)
    {
        var json = JsonSerializer.Serialize(
            obj,
            new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
        );

        var message = prefix != null ? $"{prefix}: {json}" : json;
        output.WriteInfo(message);
    }

    /// <summary>
    /// Writes a test section header
    /// </summary>
    public static void WriteSection(this ITestOutputHelper output, string sectionName)
    {
        var separator = new string('=', 50);
        output.WriteLine(separator);
        output.WriteLine($"  {sectionName}");
        output.WriteLine(separator);
    }

    /// <summary>
    /// Writes a test step
    /// </summary>
    public static void WriteStep(this ITestOutputHelper output, string stepName, string? description = null)
    {
        var message = description != null ? $"🔸 {stepName}: {description}" : $"🔸 {stepName}";
        output.WriteInfo(message);
    }

    /// <summary>
    /// Writes a success message
    /// </summary>
    public static void WriteSuccess(this ITestOutputHelper output, string message, params object[] args)
    {
        var formattedMessage = args.Length > 0 ? string.Format(message, args) : message;
        output.WriteInfo($"✅ {formattedMessage}");
    }

    /// <summary>
    /// Writes a failure message
    /// </summary>
    public static void WriteFailure(this ITestOutputHelper output, string message, params object[] args)
    {
        var formattedMessage = args.Length > 0 ? string.Format(message, args) : message;
        output.WriteError($"❌ {formattedMessage}");
    }

    /// <summary>
    /// Writes performance metrics
    /// </summary>
    public static void WritePerformance(
        this ITestOutputHelper output,
        string operation,
        TimeSpan duration,
        string? additionalInfo = null
    )
    {
        var info = additionalInfo != null ? $" ({additionalInfo})" : "";
        output.WriteInfo($"⏱️ {operation}: {duration.TotalMilliseconds:F2}ms{info}");
    }

    /// <summary>
    /// Measures and logs execution time of an operation
    /// </summary>
    public static async Task<T> MeasureAsync<T>(this ITestOutputHelper output, string operation, Func<Task<T>> action)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var result = await action();
            stopwatch.Stop();
            output.WritePerformance(operation, stopwatch.Elapsed);
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            output.WritePerformance(operation, stopwatch.Elapsed, $"FAILED: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Measures and logs execution time of an async operation without return value
    /// </summary>
    public static async Task MeasureAsync(this ITestOutputHelper output, string operation, Func<Task> action)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            await action();
            stopwatch.Stop();
            output.WritePerformance(operation, stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            output.WritePerformance(operation, stopwatch.Elapsed, $"FAILED: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Measures and logs execution time of an operation
    /// </summary>
    public static T Measure<T>(this ITestOutputHelper output, string operation, Func<T> action)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var result = action();
            stopwatch.Stop();
            output.WritePerformance(operation, stopwatch.Elapsed);
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            output.WritePerformance(operation, stopwatch.Elapsed, $"FAILED: {ex.Message}");
            throw;
        }
    }
}

/// <summary>
/// XUnit test output logger provider for integration with Microsoft.Extensions.Logging
/// </summary>
public class XunitLoggerProvider : ILoggerProvider
{
    private readonly ITestOutputHelper _output;

    public XunitLoggerProvider(ITestOutputHelper output)
    {
        _output = output;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new XunitLogger(_output, categoryName);
    }

    public void Dispose()
    {
        // Nothing to dispose
    }
}

/// <summary>
/// XUnit logger implementation
/// </summary>
public class XunitLogger : ILogger
{
    private readonly ITestOutputHelper _output;
    private readonly string _categoryName;

    public XunitLogger(ITestOutputHelper output, string categoryName)
    {
        _output = output;
        _categoryName = categoryName;
    }

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel >= LogLevel.Debug;
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter
    )
    {
        if (!IsEnabled(logLevel))
            return;

        var message = formatter(state, exception);
        var fullMessage = $"[{_categoryName}] {message}";

        if (exception != null)
        {
            fullMessage += $" Exception: {exception}";
        }

        _output.WriteLog(logLevel, fullMessage);
    }
}

/// <summary>
/// Extension methods for adding XUnit logging to service collection
/// </summary>
public static class XunitLoggingExtensions
{
    /// <summary>
    /// Adds XUnit test output logging to the service collection
    /// </summary>
    public static ILoggingBuilder AddXunitTestOutput(this ILoggingBuilder builder, ITestOutputHelper output)
    {
        builder.AddProvider(new XunitLoggerProvider(output));
        return builder;
    }
}
