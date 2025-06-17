namespace SnapDog2.Core.Models.Enums;

/// <summary>
/// Represents supported audio codecs for audio streams.
/// Used to specify the encoding format of audio data in the SnapDog2 system.
/// </summary>
public enum AudioCodec
{
    /// <summary>
    /// Pulse Code Modulation - uncompressed audio format.
    /// </summary>
    PCM,

    /// <summary>
    /// Free Lossless Audio Codec - lossless compression.
    /// </summary>
    FLAC,

    /// <summary>
    /// MPEG Audio Layer III - lossy compression.
    /// </summary>
    MP3,

    /// <summary>
    /// Advanced Audio Coding - lossy compression.
    /// </summary>
    AAC,

    /// <summary>
    /// Ogg Vorbis - open-source lossy compression.
    /// </summary>
    OGG,
}
