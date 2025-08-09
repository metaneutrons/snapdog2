using Microsoft.Extensions.Logging;

namespace KnxMonitor.Services;

/// <summary>
/// High-performance LoggerMessage definitions for FalconDptDecodingService.
/// Eliminates boxing, reduces allocations, and provides compile-time safety.
/// </summary>
public partial class FalconDptDecodingService
{
    // Decoding Success Operations (9901-9902)
    [LoggerMessage(9901, LogLevel.Debug, "Successfully decoded {DptId} using Falcon SDK")]
    private partial void LogSuccessfullyDecodedUsingFalconSdk(string dptId);

    [LoggerMessage(9902, LogLevel.Debug, "Falling back to manual decoding for {DptId}")]
    private partial void LogFallingBackToManualDecoding(string dptId);

    // Error Handling Operations (9903-9904)
    [LoggerMessage(9903, LogLevel.Warning, "Error decoding DPT {DptId}, falling back to manual decoding")]
    private partial void LogErrorDecodingDpt(Exception ex, string dptId);

    [LoggerMessage(9904, LogLevel.Debug, "Failed to decode using Falcon SDK for DPT {DptId}")]
    private partial void LogFailedToDecodeUsingFalconSdk(Exception ex, string dptId);

    // Converter Operations (9905)
    [LoggerMessage(
        9905,
        LogLevel.Debug,
        "Successfully used Falcon converter {ConverterType}.{MethodName} for DPT {DptId}"
    )]
    private partial void LogSuccessfullyUsedFalconConverter(string converterType, string methodName, string dptId);
}
