using Microsoft.Extensions.Logging;

namespace KnxMonitor.Services;

/// <summary>
/// High-performance LoggerMessage definitions for FalconService.
/// Eliminates boxing, reduces allocations, and provides compile-time safety.
/// </summary>
public partial class FalconService
{
    // Falcon Object Extraction Operations (9501-9504)
    [LoggerMessage(9501, LogLevel.Debug, "🔍 Extracting from Falcon object: {TypeName} ({Namespace})")]
    private partial void LogExtractingFromFalconObject(string typeName, string? @namespace);

    [LoggerMessage(9502, LogLevel.Debug, "🎯 Falcon SDK provided byte array: {Data}")]
    private partial void LogFalconSdkProvidedByteArray(string data);

    [LoggerMessage(9503, LogLevel.Debug, "✅ Falcon SDK provided primitive: {Value} ({Type}) -> DPT {DptId}")]
    private partial void LogFalconSdkProvidedPrimitive(object? value, string type, string? dptId);

    [LoggerMessage(9504, LogLevel.Error, "Error extracting from Falcon object {TypeName}")]
    private partial void LogErrorExtractingFromFalconObject(Exception ex, string typeName);

    // Byte Array Handling Operations (9505-9506)
    [LoggerMessage(9505, LogLevel.Debug, "📦 Decoded byte array {Data} -> {Value} (DPT {DptId})")]
    private partial void LogDecodedByteArray(string data, object? value, string? dptId);

    [LoggerMessage(9506, LogLevel.Warning, "Error decoding byte array {Data}")]
    private partial void LogErrorDecodingByteArray(Exception ex, string data);

    // Service Method Warnings (9507-9509)
    [LoggerMessage(
        9507,
        LogLevel.Warning,
        "DecodeValue called on CorrectFalconFirstService - this service is designed for live event extraction"
    )]
    private partial void LogDecodeValueCalledOnCorrectFalconFirstService();

    [LoggerMessage(
        9508,
        LogLevel.Warning,
        "DecodeValueWithAutoDetection called on CorrectFalconFirstService - this service is designed for live event extraction"
    )]
    private partial void LogDecodeValueWithAutoDetectionCalledOnCorrectFalconFirstService();

    [LoggerMessage(
        9509,
        LogLevel.Warning,
        "DetectDpt called on CorrectFalconFirstService - this service is designed for live event extraction"
    )]
    private partial void LogDetectDptCalledOnCorrectFalconFirstService();

    // Falcon SDK Object Extraction Operations (9510-9511)
    [LoggerMessage(9510, LogLevel.Debug, "✅ Extracted from Falcon SDK object: DPT {DptId}, Value {Value}")]
    private partial void LogExtractedFromFalconSdkObject(string? dptId, object? value);

    [LoggerMessage(9511, LogLevel.Warning, "Error extracting from Falcon SDK object {TypeName}")]
    private partial void LogErrorExtractingFromFalconSdkObject(Exception ex, string typeName);

    // Reflection-based Extraction Operations (9512-9515)
    [LoggerMessage(9512, LogLevel.Debug, "🔍 Reflecting on {TypeName} with {PropertyCount} properties")]
    private partial void LogReflectingOnType(string typeName, int propertyCount);

    [LoggerMessage(9513, LogLevel.Debug, "Error reading property {PropertyName}")]
    private partial void LogErrorReadingProperty(Exception ex, string propertyName);

    [LoggerMessage(9514, LogLevel.Debug, "🔍 Reflection result: DPT {DptId}, Value {Value}")]
    private partial void LogReflectionResult(string? dptId, object? value);

    [LoggerMessage(9515, LogLevel.Error, "Error during reflection on {TypeName}")]
    private partial void LogErrorDuringReflection(Exception ex, string typeName);
}
