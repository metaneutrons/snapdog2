namespace SnapDog2.Infrastructure.Audio;

using System.Text.Json;
using LibVLCSharp.Shared;
using Microsoft.Extensions.Logging;
using SnapDog2.Core.Models;

/// <summary>
/// Manages metadata extraction from media files using LibVLC.
/// </summary>
public sealed class MetadataManager(LibVLC libvlc, ILogger<MetadataManager> logger)
{
    private readonly LibVLC _libvlc = libvlc ?? throw new ArgumentNullException(nameof(libvlc));
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
            this._logger.LogDebug("Starting metadata extraction for media: {MediaMrl}", media.Mrl);

            // Parse the media to extract metadata (asynchronous in LibVLCSharp)
            var parseResult = await media.Parse(MediaParseOptions.ParseNetwork);

            if (parseResult != MediaParsedStatus.Done)
            {
                this._logger.LogWarning("Media parsing incomplete. Status: {ParseStatus}", parseResult);
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

            this._logger.LogDebug(
                "Metadata extraction completed. Title: {Title}, Artist: {Artist}, Duration: {Duration}ms",
                metadata.Title,
                metadata.Artist,
                metadata.Duration
            );

            return metadata;
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Failed to extract metadata from media: {MediaMrl}", media.Mrl);

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

            this._logger.LogDebug("Metadata saved to: {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Failed to save metadata to: {FilePath}", filePath);
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
            if (tracks == null || tracks.Length == 0)
            {
                return null;
            }

            // Find the first audio track
            var audioTracks = tracks.Where(t => t.TrackType == TrackType.Audio).ToList();
            if (!audioTracks.Any())
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
            this._logger.LogWarning(ex, "Failed to extract technical details from media");
            return null;
        }
    }
}
