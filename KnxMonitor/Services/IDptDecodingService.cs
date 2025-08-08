namespace KnxMonitor.Services;

/// <summary>
/// Service for decoding KNX Data Point Types (DPT) using Falcon SDK.
/// </summary>
public interface IDptDecodingService
{
    /// <summary>
    /// Decodes raw KNX data bytes to a typed value using the specified DPT.
    /// </summary>
    /// <param name="data">Raw KNX data bytes.</param>
    /// <param name="dptId">Data Point Type identifier (e.g., "1.001", "9.001").</param>
    /// <returns>Decoded value or null if decoding failed.</returns>
    object? DecodeValue(byte[] data, string dptId);

    /// <summary>
    /// Decodes raw KNX data bytes to a typed value, attempting to auto-detect the DPT.
    /// </summary>
    /// <param name="data">Raw KNX data bytes.</param>
    /// <returns>Decoded value with detected DPT information.</returns>
    (object? Value, string? DetectedDpt) DecodeValueWithAutoDetection(byte[] data);

    /// <summary>
    /// Gets a formatted display string for the decoded value.
    /// </summary>
    /// <param name="value">Decoded value.</param>
    /// <param name="dptId">Data Point Type identifier.</param>
    /// <returns>Formatted display string.</returns>
    string FormatValue(object? value, string? dptId);

    /// <summary>
    /// Attempts to detect the most likely DPT based on data length and content.
    /// </summary>
    /// <param name="data">Raw KNX data bytes.</param>
    /// <returns>Most likely DPT identifier or null if detection failed.</returns>
    string? DetectDpt(byte[] data);

    /// <summary>
    /// Checks if a DPT is supported by this service.
    /// </summary>
    /// <param name="dptId">Data Point Type identifier.</param>
    /// <returns>True if supported, false otherwise.</returns>
    bool IsDptSupported(string dptId);

    /// <summary>
    /// Gets all supported DPT identifiers.
    /// </summary>
    /// <returns>Collection of supported DPT identifiers.</returns>
    IEnumerable<string> GetSupportedDpts();
}
