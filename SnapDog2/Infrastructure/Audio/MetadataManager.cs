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
namespace SnapDog2.Infrastructure.Audio;

using System.Text.Json;
using LibVLCSharp.Shared;
using SnapDog2.Shared.Models;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

/// <summary>
/// Manages metadata extraction from media files using LibVLC.
/// </summary>
public sealed partial class MetadataManager(ILogger<MetadataManager> logger)
{
    private readonly ILogger<MetadataManager> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Extracts metadata from a media source asynchronously.
    /// </summary>
    /// <param name="media">The LibVLC media object to extract metadata from.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Extracted audio metadata.</returns>
    public async Task<AudioMetadata> ExtractMetadataAsync(Media media, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("StartingMetadataExtraction: {Details}", media.Mrl);

            // Use faster parsing options with timeout
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(5)); // 5 second timeout for metadata

            // Use ParseLocal for faster parsing, fallback to ParseNetwork if needed
            var parseOptions = media.Mrl.StartsWith("http")
                ? MediaParseOptions.ParseNetwork | MediaParseOptions.FetchLocal
                : MediaParseOptions.ParseLocal;

            var parseResult = await media.Parse(parseOptions, cancellationToken: timeoutCts.Token);

            if (parseResult != MediaParsedStatus.Done)
            {
                _logger.LogInformation("MediaParsingIncomplete: {Details}", parseResult.ToString());

                // If parsing failed, try with minimal network parsing
                if (media.Mrl.StartsWith("http") && parseResult == MediaParsedStatus.Failed)
                {
                    _logger.LogInformation("RetryingWithMinimalParsing: {Details}", media.Mrl);
                    parseResult = await media.Parse(MediaParseOptions.FetchLocal, cancellationToken: timeoutCts.Token);
                }
            }

            var metadata = new AudioMetadata
            {
                Title = media.Meta(MetadataType.Title),
                Artist = media.Meta(MetadataType.Artist),
                Album = media.Meta(MetadataType.Album),
                Genre = media.Meta(MetadataType.Genre),
                Description = media.Meta(MetadataType.Description),
                EncodedBy = media.Meta(MetadataType.EncodedBy),
                ArtworkUrl = media.Meta(MetadataType.ArtworkURL),
                NowPlaying = media.Meta(MetadataType.NowPlaying),
                Duration = media.Duration,
                Date = DateTime.TryParse(media.Meta(MetadataType.Date), out var date) ? date : DateTime.MinValue,
            };

            // Parse numeric metadata with fallbacks
            if (int.TryParse(media.Meta(MetadataType.Date), out var year))
            {
                metadata.Year = year;
            }

            if (int.TryParse(media.Meta(MetadataType.TrackNumber), out var trackNumber))
            {
                metadata.TrackNumber = trackNumber;
            }

            if (float.TryParse(media.Meta(MetadataType.Rating), out var rating))
            {
                metadata.Rating = rating;
            }

            // Extract publishers (if available)
            var publisher = media.Meta(MetadataType.Publisher);
            if (!string.IsNullOrEmpty(publisher))
            {
                metadata.Publishers = new[] { publisher };
            }

            // Extract technical details from tracks
            metadata.TechnicalDetails = this.ExtractTechnicalDetails(media);

            _logger.LogInformation("MetadataExtractionCompleted: {Title} {Artist} {Duration}", metadata.Title ?? "Unknown", metadata.Artist ?? "Unknown", metadata.Duration);

            return metadata;
        }
        catch (Exception ex)
        {
            LogFailedToExtractMetadata(ex.Message, media.Mrl);

            // Return minimal metadata on error
            return new AudioMetadata
            {
                Title = "Unknown",
                Artist = "Unknown",
                Duration = media.Duration,
                TechnicalDetails = new TechnicalDetails(),
            };
        }
    }

    /// <summary>
    /// Saves metadata to a JSON file.
    /// </summary>
    /// <param name="metadata">The metadata to save.</param>
    /// <param name="filePath">The file path to save to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task SaveMetadataAsync(
        AudioMetadata metadata,
        string filePath,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };

            var json = JsonSerializer.Serialize(metadata, options);
            await File.WriteAllTextAsync(filePath, json, cancellationToken);

            _logger.LogInformation("MetadataSaved: {Details}", filePath);
        }
        catch (Exception ex)
        {
            LogFailedToSaveMetadata(ex.Message, filePath);
            throw;
        }
    }

    /// <summary>
    /// Extracts technical details from media tracks.
    /// </summary>
    /// <param name="media">The media object to extract technical details from.</param>
    /// <returns>Technical details or null if not available.</returns>
    private TechnicalDetails? ExtractTechnicalDetails(Media media)
    {
        try
        {
            var tracks = media.Tracks;
            if (tracks.Length == 0)
            {
                return null;
            }

            // Find the first audio track
            var audioTracks = tracks.Where(t => t.TrackType == TrackType.Audio).ToList();
            if (audioTracks.Count == 0)
            {
                return null;
            }

            var audioTrack = audioTracks.First();
            var audioData = audioTrack.Data.Audio;

            return new TechnicalDetails
            {
                Codec = audioTrack.Codec.ToString(),
                Bitrate = (int)audioTrack.Bitrate,
                SampleRate = (int)audioData.Rate,
                Channels = (int)audioData.Channels,
            };
        }
        catch (Exception ex)
        {
            _logger.LogInformation("FailedToExtractTechnicalDetails: {Details}", ex);
            return null;
        }
    }

    // LoggerMessage methods for high-performance logging
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Failed to extract metadata: {Details} for {FilePath}")]
    private partial void LogFailedToExtractMetadata(string? details, string? filePath);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Failed to save metadata: {Details} for {FilePath}")]
    private partial void LogFailedToSaveMetadata(string? details, string? filePath);
}
