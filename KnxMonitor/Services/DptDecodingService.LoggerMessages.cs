using Microsoft.Extensions.Logging;

namespace KnxMonitor.Services;

/// <summary>
/// High-performance LoggerMessage definitions for DptDecodingService.
/// Eliminates boxing, reduces allocations, and provides compile-time safety.
/// </summary>
public partial class DptDecodingService
{
    // DPT Validation Operations (10301-10303)
    [LoggerMessage(10301, LogLevel.Debug, "Unsupported DPT: {DptId}")]
    private partial void LogUnsupportedDpt(string dptId);

    [LoggerMessage(10302, LogLevel.Debug, "Invalid data length for DPT {DptId}: expected {Expected}, got {Actual}")]
    private partial void LogInvalidDataLengthForDpt(string dptId, int expected, int actual);

    [LoggerMessage(10303, LogLevel.Error, "Error decoding DPT {DptId} with data {Data}")]
    private partial void LogErrorDecodingDpt(Exception ex, string dptId, string data);
}
