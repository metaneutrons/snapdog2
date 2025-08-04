namespace SnapDog2.Core.Models;

/// <summary>
/// Audio format specification for SoundFlow processing.
/// </summary>
/// <param name="SampleRate">Sample rate in Hz (e.g., 48000)</param>
/// <param name="BitDepth">Bit depth in bits (e.g., 16)</param>
/// <param name="Channels">Number of channels (e.g., 2 for stereo)</param>
public record AudioFormat(int SampleRate, int BitDepth, int Channels)
{
    /// <summary>
    /// Returns a string representation in the format "SampleRate:BitDepth:Channels".
    /// </summary>
    public override string ToString() => $"{SampleRate}:{BitDepth}:{Channels}";

    /// <summary>
    /// Creates an AudioFormat from a string in the format "SampleRate:BitDepth:Channels".
    /// </summary>
    /// <param name="formatString">Format string (e.g., "48000:16:2")</param>
    /// <returns>AudioFormat instance</returns>
    /// <exception cref="ArgumentException">Thrown when format string is invalid</exception>
    public static AudioFormat Parse(string formatString)
    {
        if (string.IsNullOrWhiteSpace(formatString))
            throw new ArgumentException("Format string cannot be null or empty", nameof(formatString));

        var parts = formatString.Split(':');
        if (parts.Length != 3)
            throw new ArgumentException(
                $"Invalid format string '{formatString}'. Expected format: 'SampleRate:BitDepth:Channels'",
                nameof(formatString)
            );

        if (!int.TryParse(parts[0], out var sampleRate) || sampleRate <= 0)
            throw new ArgumentException($"Invalid sample rate '{parts[0]}'", nameof(formatString));

        if (!int.TryParse(parts[1], out var bitDepth) || bitDepth <= 0)
            throw new ArgumentException($"Invalid bit depth '{parts[1]}'", nameof(formatString));

        if (!int.TryParse(parts[2], out var channels) || channels <= 0)
            throw new ArgumentException($"Invalid channels '{parts[2]}'", nameof(formatString));

        return new AudioFormat(sampleRate, bitDepth, channels);
    }

    /// <summary>
    /// Tries to parse an AudioFormat from a string.
    /// </summary>
    /// <param name="formatString">Format string to parse</param>
    /// <param name="audioFormat">Parsed AudioFormat if successful</param>
    /// <returns>True if parsing was successful</returns>
    public static bool TryParse(string? formatString, out AudioFormat? audioFormat)
    {
        audioFormat = null;
        try
        {
            if (string.IsNullOrWhiteSpace(formatString))
                return false;

            audioFormat = Parse(formatString);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
