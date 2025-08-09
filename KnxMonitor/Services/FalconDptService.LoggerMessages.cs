using Microsoft.Extensions.Logging;

namespace KnxMonitor.Services;

/// <summary>
/// High-performance LoggerMessage definitions for FalconDptService.
/// Eliminates boxing, reduces allocations, and provides compile-time safety.
/// </summary>
public partial class FalconDptService
{
    // Main Decoding Operations (9201-9205)
    [LoggerMessage(9201, LogLevel.Debug, "Successfully decoded {DptId} using Falcon SDK: {Value}")]
    private partial void LogSuccessfullyDecodedUsingFalconSdk(string dptId, object value);

    [LoggerMessage(
        9202,
        LogLevel.Warning,
        "Falcon SDK couldn't decode DPT {DptId} with data {Data} - this shouldn't happen for standard DPTs"
    )]
    private partial void LogFalconSdkCouldNotDecodeDpt(string dptId, string data);

    [LoggerMessage(9203, LogLevel.Error, "Error using Falcon SDK to decode DPT {DptId}")]
    private partial void LogErrorUsingFalconSdkToDecodeDpt(Exception ex, string dptId);

    [LoggerMessage(9204, LogLevel.Debug, "Auto-detected DPT {DptId} for data {Data}")]
    private partial void LogAutoDetectedDptForData(string dptId, string data);

    [LoggerMessage(9205, LogLevel.Debug, "Could not auto-detect DPT for data {Data}")]
    private partial void LogCouldNotAutoDetectDptForData(string data);

    // GroupValue Creation Operations (9206-9212)
    [LoggerMessage(9206, LogLevel.Debug, "Trying GroupValue constructor with byte[] and string for DPT {DptId}")]
    private partial void LogTryingGroupValueConstructorForDpt(string dptId);

    [LoggerMessage(9207, LogLevel.Debug, "GroupValue constructor failed for DPT {DptId}")]
    private partial void LogGroupValueConstructorFailedForDpt(Exception ex, string dptId);

    [LoggerMessage(9208, LogLevel.Debug, "Trying factory method {MethodName} for DPT {DptId}")]
    private partial void LogTryingFactoryMethodForDpt(string methodName, string dptId);

    [LoggerMessage(9209, LogLevel.Debug, "Factory method {MethodName} failed for DPT {DptId}")]
    private partial void LogFactoryMethodFailedForDpt(Exception ex, string methodName, string dptId);

    [LoggerMessage(9210, LogLevel.Debug, "Trying DPT-specific type {TypeName} for DPT {DptId}")]
    private partial void LogTryingDptSpecificTypeForDpt(string typeName, string dptId);

    [LoggerMessage(9211, LogLevel.Debug, "DPT-specific type {TypeName} failed for DPT {DptId}")]
    private partial void LogDptSpecificTypeFailedForDpt(Exception ex, string typeName, string dptId);

    [LoggerMessage(
        9212,
        LogLevel.Warning,
        "Could not create Falcon GroupValue for DPT {DptId} - no suitable constructor/factory found"
    )]
    private partial void LogCouldNotCreateFalconGroupValueForDpt(string dptId);

    [LoggerMessage(9213, LogLevel.Error, "Error creating Falcon GroupValue for DPT {DptId}")]
    private partial void LogErrorCreatingFalconGroupValueForDpt(Exception ex, string dptId);

    // Value Extraction Operations (9214-9219)
    [LoggerMessage(9214, LogLevel.Debug, "Extracted value from property {PropertyName}: {Value} ({ValueType})")]
    private partial void LogExtractedValueFromProperty(string propertyName, object value, string valueType);

    [LoggerMessage(9215, LogLevel.Debug, "Error reading property {PropertyName}")]
    private partial void LogErrorReadingProperty(Exception ex, string propertyName);

    [LoggerMessage(9216, LogLevel.Debug, "Extracted value from method {MethodName}: {Value} ({ValueType})")]
    private partial void LogExtractedValueFromMethod(string methodName, object value, string valueType);

    [LoggerMessage(9217, LogLevel.Debug, "Error calling method {MethodName}")]
    private partial void LogErrorCallingMethod(Exception ex, string methodName);

    [LoggerMessage(9218, LogLevel.Debug, "Using GroupValue object itself as value: {Value} ({ValueType})")]
    private partial void LogUsingGroupValueObjectItselfAsValue(object value, string valueType);

    [LoggerMessage(9219, LogLevel.Error, "Error extracting value from Falcon GroupValue")]
    private partial void LogErrorExtractingValueFromFalconGroupValue(Exception ex);
}
