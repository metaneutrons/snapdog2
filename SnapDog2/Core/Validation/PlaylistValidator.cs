using FluentValidation;
using SnapDog2.Core.Models.Entities;

namespace SnapDog2.Core.Validation;

/// <summary>
/// FluentValidation validator for Playlist entity.
/// Validates all properties and business rules for music playlists.
/// </summary>
public sealed class PlaylistValidator : AbstractValidator<Playlist>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlaylistValidator"/> class.
    /// </summary>
    public PlaylistValidator()
    {
        // Required string properties
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Playlist ID is required.")
            .MaximumLength(100)
            .WithMessage("Playlist ID cannot exceed 100 characters.")
            .Matches(@"^[a-zA-Z0-9_-]+$")
            .WithMessage("Playlist ID can only contain alphanumeric characters, underscores, and hyphens.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Playlist name is required.")
            .MaximumLength(200)
            .WithMessage("Playlist name cannot exceed 200 characters.")
            .MinimumLength(1)
            .WithMessage("Playlist name must be at least 1 character long.");

        // Track IDs validation
        RuleFor(x => x.TrackIds)
            .NotNull()
            .WithMessage("Track IDs collection cannot be null.")
            .Must(HaveUniqueTrackIds)
            .WithMessage("Playlist cannot contain duplicate track IDs.")
            .Must(HaveValidTrackIdFormat)
            .WithMessage("All track IDs must be valid format.");

        RuleForEach(x => x.TrackIds)
            .NotEmpty()
            .WithMessage("Track ID cannot be empty.")
            .MaximumLength(100)
            .WithMessage("Track ID cannot exceed 100 characters.")
            .Matches(@"^[a-zA-Z0-9_-]+$")
            .WithMessage("Track ID can only contain alphanumeric characters, underscores, and hyphens.");

        // Optional description validation
        RuleFor(x => x.Description)
            .MaximumLength(1000)
            .WithMessage("Description cannot exceed 1000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));

        // Optional owner validation
        RuleFor(x => x.Owner)
            .MaximumLength(100)
            .WithMessage("Owner cannot exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.Owner));

        // Optional tags validation
        RuleFor(x => x.Tags)
            .MaximumLength(500)
            .WithMessage("Tags cannot exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.Tags));

        // Optional cover art path validation
        RuleFor(x => x.CoverArtPath)
            .MaximumLength(500)
            .WithMessage("Cover art path cannot exceed 500 characters.")
            .Must(BeValidPath)
            .WithMessage("Cover art path must be a valid file path or URL.")
            .When(x => !string.IsNullOrEmpty(x.CoverArtPath));

        // Total duration validation
        RuleFor(x => x.TotalDurationSeconds)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Total duration cannot be negative.")
            .LessThanOrEqualTo(86400) // 24 hours max
            .WithMessage("Total duration cannot exceed 24 hours (86400 seconds).")
            .When(x => x.TotalDurationSeconds.HasValue);

        // Play count validation
        RuleFor(x => x.PlayCount)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Play count cannot be negative.")
            .LessThanOrEqualTo(1000000)
            .WithMessage("Play count cannot exceed 1,000,000.");

        // Timestamp validations
        RuleFor(x => x.CreatedAt)
            .NotEmpty()
            .WithMessage("Created timestamp is required.")
            .LessThanOrEqualTo(DateTime.UtcNow.AddMinutes(5))
            .WithMessage("Created timestamp cannot be in the future.");

        RuleFor(x => x.UpdatedAt)
            .GreaterThanOrEqualTo(x => x.CreatedAt)
            .WithMessage("Updated timestamp must be after or equal to created timestamp.")
            .LessThanOrEqualTo(DateTime.UtcNow.AddMinutes(5))
            .WithMessage("Updated timestamp cannot be in the future.")
            .When(x => x.UpdatedAt.HasValue);

        RuleFor(x => x.LastPlayedAt)
            .GreaterThanOrEqualTo(x => x.CreatedAt)
            .WithMessage("Last played timestamp must be after or equal to created timestamp.")
            .LessThanOrEqualTo(DateTime.UtcNow.AddMinutes(5))
            .WithMessage("Last played timestamp cannot be in the future.")
            .When(x => x.LastPlayedAt.HasValue);

        // Business rules
        RuleFor(x => x.TrackIds)
            .Must(NotExceedMaximumTracks)
            .WithMessage("Playlist cannot contain more than 10,000 tracks.");

        // Business rule: System playlists have specific constraints
        RuleFor(x => x.Owner)
            .Equal("system")
            .WithMessage("System playlists must have 'system' as the owner.")
            .When(x => x.IsSystem);

        RuleFor(x => x.IsPublic).Equal(true).WithMessage("System playlists must be public.").When(x => x.IsSystem);

        // Business rule: Play count consistency
        RuleFor(x => x.LastPlayedAt)
            .NotNull()
            .WithMessage("Playlists with play count > 0 must have a last played timestamp.")
            .When(x => x.PlayCount > 0);

        // Business rule: Duration consistency with track count
        RuleFor(x => x)
            .Must(HaveReasonableDurationForTrackCount)
            .WithMessage("Total duration seems unreasonable for the number of tracks.")
            .When(x => x.TotalDurationSeconds.HasValue && x.TrackIds.Count > 0);
    }

    /// <summary>
    /// Validates that all track IDs are unique within the playlist.
    /// </summary>
    /// <param name="trackIds">The collection of track IDs to validate.</param>
    /// <returns>True if all track IDs are unique; otherwise, false.</returns>
    private static bool HaveUniqueTrackIds(IEnumerable<string> trackIds)
    {
        if (trackIds == null)
        {
            return true;
        }

        var trackList = trackIds.ToList();
        return trackList.Count == trackList.Distinct().Count();
    }

    /// <summary>
    /// Validates that all track IDs have valid format.
    /// </summary>
    /// <param name="trackIds">The collection of track IDs to validate.</param>
    /// <returns>True if all track IDs have valid format; otherwise, false.</returns>
    private static bool HaveValidTrackIdFormat(IEnumerable<string> trackIds)
    {
        if (trackIds == null)
        {
            return true;
        }

        var trackIdRegex = new System.Text.RegularExpressions.Regex(@"^[a-zA-Z0-9_-]+$");
        return trackIds.All(id => !string.IsNullOrEmpty(id) && id.Length <= 100 && trackIdRegex.IsMatch(id));
    }

    /// <summary>
    /// Validates that the path is a valid file path or URL.
    /// </summary>
    /// <param name="path">The path to validate.</param>
    /// <returns>True if the path is valid; otherwise, false.</returns>
    private static bool BeValidPath(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return true;
        }

        // Check if it's a valid URL
        if (Uri.TryCreate(path, UriKind.Absolute, out var uri))
        {
            return uri.Scheme == "http" || uri.Scheme == "https" || uri.Scheme == "file";
        }

        // Check if it's a valid relative path (basic validation)
        try
        {
            // This will throw if the path contains invalid characters
            var fullPath = Path.GetFullPath(path);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validates that the playlist doesn't exceed the maximum number of tracks.
    /// </summary>
    /// <param name="trackIds">The collection of track IDs to validate.</param>
    /// <returns>True if the track count is within limits; otherwise, false.</returns>
    private static bool NotExceedMaximumTracks(IEnumerable<string> trackIds)
    {
        if (trackIds == null)
        {
            return true;
        }

        return trackIds.Count() <= 10000; // Reasonable limit for playlist size
    }

    /// <summary>
    /// Validates that the total duration is reasonable for the number of tracks.
    /// </summary>
    /// <param name="playlist">The playlist to validate.</param>
    /// <returns>True if the duration is reasonable; otherwise, false.</returns>
    private static bool HaveReasonableDurationForTrackCount(Playlist playlist)
    {
        if (!playlist.TotalDurationSeconds.HasValue || playlist.TrackIds.Count == 0)
        {
            return true;
        }

        var averageDurationPerTrack = playlist.TotalDurationSeconds.Value / (double)playlist.TrackIds.Count;

        // Average track should be between 30 seconds and 30 minutes
        return averageDurationPerTrack >= 30 && averageDurationPerTrack <= 1800;
    }
}
