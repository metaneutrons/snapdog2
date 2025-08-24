//
// SnapDog
// The Snapcast-based Smart Home Audio System with MQTT & KNX integration
// Copyright (C) 2025 Fabian Schmieder
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <https://www.gnu.org/licenses/>.
//
namespace SnapDog2.Core.Models;

/// <summary>
/// Comprehensive audio metadata extracted from media files.
/// </summary>
public class AudioMetadata
{
    public string? Title { get; set; }
    public string? Artist { get; set; }
    public string? Album { get; set; }
    public int Year { get; set; }
    public string? Genre { get; set; }
    public long Duration { get; set; }
    public int TrackNumber { get; set; }
    public string? Description { get; set; }
    public float Rating { get; set; }
    public DateTime Date { get; set; }
    public string? Set { get; set; }
    public string? NowPlaying { get; set; }
    public string[]? Publishers { get; set; }
    public string? EncodedBy { get; set; }
    public string? ArtworkUrl { get; set; }
    public TechnicalDetails? TechnicalDetails { get; set; }
}

/// <summary>
/// Technical details about the audio stream.
/// </summary>
public class TechnicalDetails
{
    public string? Codec { get; set; }
    public int Bitrate { get; set; }
    public int SampleRate { get; set; }
    public int Channels { get; set; }
}

/// <summary>
/// Audio processing configuration for LibVLC output.
/// </summary>
public class AudioProcessingConfig
{
    public int SampleRate { get; set; } = 48000;
    public int BitsPerSample { get; set; } = 16;
    public int Channels { get; set; } = 2;
    public string Format { get; set; } = "raw";
}

/// <summary>
/// Result of audio processing operation.
/// </summary>
public class AudioProcessingResult
{
    public bool Success { get; set; }
    public string? OutputFilePath { get; set; }
    public string? MetadataPath { get; set; }
    public string? SourceId { get; set; }
    public AudioProcessingConfig? Config { get; set; }
    public AudioMetadata? Metadata { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Audio source type enumeration.
/// </summary>
public enum AudioSourceType
{
    Url,
    File,
    Stream,
}
