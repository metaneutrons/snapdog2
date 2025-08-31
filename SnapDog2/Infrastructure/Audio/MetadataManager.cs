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
            LogStartingMetadataExtraction(this._logger, media.Mrl);

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
                LogMediaParsingIncomplete(this._logger, parseResult.ToString());

                // If parsing failed, try with minimal network parsing
                if (media.Mrl.StartsWith("http") && parseResult == MediaParsedStatus.Failed)
                {
                    LogRetryingWithMinimalParsing(this._logger, media.Mrl);
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

            LogMetadataExtractionCompleted(
                this._logger,
                metadata.Title ?? "Unknown",
                metadata.Artist ?? "Unknown",
                metadata.Duration
            );

            return metadata;
        }
        catch (Exception ex)
        {
            LogFailedToExtractMetadata(this._logger, ex, media.Mrl);

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

            LogMetadataSaved(this._logger, filePath);
        }
        catch (Exception ex)
        {
            LogFailedToSaveMetadata(this._logger, ex, filePath);
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
            LogFailedToExtractTechnicalDetails(this._logger, ex);
            return null;
        }
    }

    // LoggerMessage methods for high-performance logging
    [LoggerMessage(
        EventId = 2400,
        Level = LogLevel.Debug,
        Message = "Starting metadata extraction for media: {MediaMrl}"
    )]
    private static partial void LogStartingMetadataExtraction(ILogger logger, string mediaMrl);

    [LoggerMessage(
        EventId = 2401,
        Level = LogLevel.Warning,
        Message = "Media parsing incomplete. Status: {ParseStatus}"
    )]
    private static partial void LogMediaParsingIncomplete(ILogger logger, string parseStatus);

    [LoggerMessage(
        EventId = 2402,
        Level = LogLevel.Debug,
        Message = "Metadata extraction completed. Title: {Title}, Artist: {Artist}, Duration: {Duration}ms"
    )]
    private static partial void LogMetadataExtractionCompleted(
        ILogger logger,
        string title,
        string artist,
        long duration
    );

    [LoggerMessage(
        EventId = 2403,
        Level = LogLevel.Error,
        Message = "Failed to extract metadata from media: {MediaMrl}"
    )]
    private static partial void LogFailedToExtractMetadata(ILogger logger, Exception ex, string mediaMrl);

    [LoggerMessage(
        EventId = 2404,
        Level = LogLevel.Debug,
        Message = "Metadata saved to: {FilePath}"
    )]
    private static partial void LogMetadataSaved(ILogger logger, string filePath);

    [LoggerMessage(
        EventId = 2405,
        Level = LogLevel.Error,
        Message = "Failed to save metadata to: {FilePath}"
    )]
    private static partial void LogFailedToSaveMetadata(ILogger logger, Exception ex, string filePath);

    [LoggerMessage(
        EventId = 2406,
        Level = LogLevel.Warning,
        Message = "Failed to extract technical details from media"
    )]
    private static partial void LogFailedToExtractTechnicalDetails(ILogger logger, Exception ex);

    [LoggerMessage(
        EventId = 2407,
        Level = LogLevel.Debug,
        Message = "Retrying metadata extraction with minimal parsing for: {MediaUrl}"
    )]
    private static partial void LogRetryingWithMinimalParsing(ILogger logger, string mediaUrl);
}
