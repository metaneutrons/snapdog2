using Microsoft.Extensions.Logging;

namespace KnxMonitor.Services;

/// <summary>
/// LoggerMessage definitions for FalconKnxMonitorService.
/// </summary>
public partial class FalconKnxMonitorService
{
    // Constructor and Configuration (9041-9050)
    [LoggerMessage(9041, LogLevel.Warning, "Invalid filter pattern: {Filter}")]
    private partial void LogInvalidFilterPattern(Exception ex, string filter);

    // Service Lifecycle (9051-9060)
    [LoggerMessage(9051, LogLevel.Information, "Starting Falcon-first KNX monitoring with {ConnectionType} connection")]
    private partial void LogStartingMonitoring(string connectionType);

    [LoggerMessage(
        9052,
        LogLevel.Information,
        "Falcon-first KNX monitoring started successfully - ready to use SDK decoded values!"
    )]
    private partial void LogMonitoringStartedSuccessfully();

    [LoggerMessage(9053, LogLevel.Error, "Failed to start Falcon-first KNX monitoring")]
    private partial void LogFailedToStartMonitoring(Exception ex);

    [LoggerMessage(9054, LogLevel.Information, "Falcon-first KNX monitoring stopped")]
    private partial void LogMonitoringStopped();

    [LoggerMessage(9055, LogLevel.Error, "Error stopping Falcon-first KNX monitoring")]
    private partial void LogErrorStoppingMonitoring(Exception ex);

    [LoggerMessage(9056, LogLevel.Error, "Error during async dispose")]
    private partial void LogErrorDuringAsyncDispose(Exception ex);

    [LoggerMessage(9057, LogLevel.Error, "Error disposing Falcon-first KNX monitor service")]
    private partial void LogErrorDisposingService(Exception ex);

    // Message Processing (9061-9080)
    [LoggerMessage(9061, LogLevel.Debug, "🎉 Received KNX message - Falcon SDK has already done the decoding work!")]
    private partial void LogReceivedKnxMessage();

    [LoggerMessage(9062, LogLevel.Error, "Error processing Falcon-first group message")]
    private partial void LogErrorProcessingGroupMessage(Exception ex);

    [LoggerMessage(
        9063,
        LogLevel.Debug,
        "✨ Falcon SDK treasure extracted - DPT: {DptId}, Value: {Value} ({ValueType}), Raw: {RawData}"
    )]
    private partial void LogFalconTreasureExtracted(string dptId, object? value, string valueType, string rawData);

    [LoggerMessage(9064, LogLevel.Debug, "No value in GroupEventArgs - probably a read request")]
    private partial void LogNoValueInGroupEventArgs();

    [LoggerMessage(9065, LogLevel.Debug, "🔍 Analyzing Falcon SDK value type: {ValueType} from namespace: {Namespace}")]
    private partial void LogAnalyzingFalconValueType(string valueType, string? @namespace);

    [LoggerMessage(9066, LogLevel.Debug, "🎯 Falcon SDK provided GroupValue object: {TypeName}")]
    private partial void LogFalconSdkProvidedGroupValueObject(string typeName);

    [LoggerMessage(9067, LogLevel.Debug, "🎯 Falcon SDK provided primitive type: {Value} ({Type})")]
    private partial void LogFalconSdkProvidedPrimitiveType(object? value, string type);

    [LoggerMessage(9068, LogLevel.Debug, "📋 GroupValue properties: {Properties}")]
    private partial void LogGroupValueProperties(string properties);

    [LoggerMessage(9069, LogLevel.Debug, "📋 GroupValue methods: {Methods}")]
    private partial void LogGroupValueMethods(string methods);

    [LoggerMessage(9070, LogLevel.Debug, "📦 Extracted bytes from GroupValue: {Data} (length: {Length})")]
    private partial void LogExtractedBytesFromGroupValue(string data, int length);

    [LoggerMessage(9071, LogLevel.Debug, "🎉 Decoded: {DecodedValue} (DPT {DptId})")]
    private partial void LogDecodedValue(object? decodedValue, string? dptId);

    [LoggerMessage(9072, LogLevel.Debug, "🎉 Extracted direct value: {Value} ({Type})")]
    private partial void LogExtractedDirectValue(object value, string type);

    [LoggerMessage(9073, LogLevel.Warning, "❌ Could not extract anything useful from GroupValue {TypeName}")]
    private partial void LogCouldNotExtractFromGroupValue(string typeName);

    [LoggerMessage(9074, LogLevel.Debug, "🎯 Falcon SDK provided GroupValue object")]
    private partial void LogFalconGroupValueObject();

    [LoggerMessage(9075, LogLevel.Debug, "🔍 Using reflection to explore Falcon SDK object")]
    private partial void LogUsingReflectionToExplore();

    [LoggerMessage(9076, LogLevel.Error, "Error extracting Falcon SDK treasure from {ValueType}")]
    private partial void LogErrorExtractingFalconTreasure(Exception ex, string valueType);

    // GroupValue Extraction (9081-9100)
    [LoggerMessage(
        9081,
        LogLevel.Debug,
        "📦 Extracted from Falcon GroupValue - DPT: {DptId}, Decoded: {DecodedValue}, Raw: {RawData}"
    )]
    private partial void LogExtractedFromFalconGroupValue(string? dptId, object? decodedValue, string rawData);

    [LoggerMessage(9082, LogLevel.Warning, "Error extracting from Falcon GroupValue {TypeName}")]
    private partial void LogErrorExtractingFromFalconGroupValue(Exception ex, string typeName);

    [LoggerMessage(9083, LogLevel.Debug, "🔍 Reflecting on {TypeName} with {PropertyCount} properties")]
    private partial void LogReflectingOnType(string typeName, int propertyCount);

    [LoggerMessage(9084, LogLevel.Debug, "Error reading property {PropertyName}")]
    private partial void LogErrorReadingProperty(Exception ex, string propertyName);

    [LoggerMessage(
        9085,
        LogLevel.Debug,
        "🔍 Reflection result - DPT: {DptId}, Decoded: {DecodedValue}, Raw: {RawData}"
    )]
    private partial void LogReflectionResult(string? dptId, object? decodedValue, string rawData);

    [LoggerMessage(9086, LogLevel.Error, "Error during reflection on {TypeName}")]
    private partial void LogErrorDuringReflection(Exception ex, string typeName);

    // Byte Extraction (9101-9110)
    [LoggerMessage(9087, LogLevel.Debug, "✅ Found bytes in property {PropertyName}: {Data}")]
    private partial void LogFoundBytesInProperty(string propertyName, string data);

    [LoggerMessage(9088, LogLevel.Debug, "✅ Found bytes from method {MethodName}: {Data}")]
    private partial void LogFoundBytesFromMethod(string methodName, string data);

    [LoggerMessage(9089, LogLevel.Debug, "❌ Could not extract bytes from GroupValue {TypeName}")]
    private partial void LogCouldNotExtractBytesFromGroupValue(string typeName);

    [LoggerMessage(9090, LogLevel.Debug, "Error extracting bytes from GroupValue")]
    private partial void LogErrorExtractingBytesFromGroupValue(Exception ex);

    // Value Extraction (9111-9120)
    [LoggerMessage(9091, LogLevel.Debug, "✅ Found value in property {PropertyName}: {Value} ({Type})")]
    private partial void LogFoundValueInProperty(string propertyName, object value, string type);

    [LoggerMessage(9092, LogLevel.Debug, "✅ Found value from method {MethodName}: {Value} ({Type})")]
    private partial void LogFoundValueFromMethod(string methodName, object value, string type);

    [LoggerMessage(9093, LogLevel.Debug, "❌ Could not extract value from GroupValue {TypeName}")]
    private partial void LogCouldNotExtractValueFromGroupValue(string typeName);

    [LoggerMessage(9094, LogLevel.Debug, "Error extracting value from GroupValue")]
    private partial void LogErrorExtractingValueFromGroupValue(Exception ex);

    // Connection Creation (9121-9128)
    [LoggerMessage(9095, LogLevel.Error, "Failed to create connector parameters")]
    private partial void LogFailedToCreateConnectorParameters(Exception ex);

    [LoggerMessage(9096, LogLevel.Debug, "Creating IP tunneling connection to {Gateway}:{Port}")]
    private partial void LogCreatingIpTunnelingConnection(string gateway, int port);

    [LoggerMessage(9097, LogLevel.Debug, "Creating IP routing connection to multicast address {MulticastAddress}")]
    private partial void LogCreatingIpRoutingConnection(string multicastAddress);

    [LoggerMessage(9098, LogLevel.Debug, "Creating IP routing connection to {MulticastAddress} ({ResolvedIp})")]
    private partial void LogCreatingIpRoutingConnectionResolved(string multicastAddress, object resolvedIp);

    [LoggerMessage(9099, LogLevel.Debug, "Creating USB connection to device: {Device}")]
    private partial void LogCreatingUsbConnection(object device);

    [LoggerMessage(
        9100,
        LogLevel.Debug,
        "🎉 Falcon-first KNX message: {MessageType} {GroupAddress} = {Value} (DPT: {DptId})"
    )]
    private partial void LogKnxMessage(object messageType, string groupAddress, object? value, string dptId);
}
