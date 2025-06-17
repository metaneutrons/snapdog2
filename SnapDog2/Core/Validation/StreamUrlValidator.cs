using FluentValidation;
using SnapDog2.Core.Models.ValueObjects;

namespace SnapDog2.Core.Validation;

/// <summary>
/// FluentValidation validator for StreamUrl value object.
/// Validates stream URL format and business rules for audio streaming.
/// </summary>
public sealed class StreamUrlValidator : AbstractValidator<StreamUrl>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StreamUrlValidator"/> class.
    /// </summary>
    public StreamUrlValidator()
    {
        // Value validation - URL format
        RuleFor(x => x.Value)
            .NotNull()
            .WithMessage("Stream URL value is required.")
            .Must(BeValidUri)
            .WithMessage("Stream URL must be a valid URI.");

        // Scheme validation
        RuleFor(x => x.Scheme)
            .NotEmpty()
            .WithMessage("Stream URL scheme is required.")
            .Must(BeSupportedScheme)
            .WithMessage("Stream URL scheme must be supported (http, https, file, ftp, rtsp, rtmp).");

        // Host validation for network URLs
        RuleFor(x => x.Host)
            .NotEmpty()
            .WithMessage("Host is required for network URLs.")
            .Must(BeValidHost)
            .WithMessage("Host must be a valid hostname or IP address.")
            .When(x => RequiresHost(x.Scheme));

        // Port validation
        RuleFor(x => x.Port)
            .InclusiveBetween(1, 65535)
            .WithMessage("Port must be between 1 and 65535.")
            .Must(BeAppropriatePortForScheme)
            .WithMessage("Port is not appropriate for the specified scheme.")
            .When(x => x.Port > 0 && RequiresHost(x.Scheme));

        // Path validation
        RuleFor(x => x.Path).Must(BeValidPath).WithMessage("URL path contains invalid characters or format.");

        // Business rules
        RuleFor(x => x)
            .Must(BeAccessibleUrl)
            .WithMessage("Stream URL should be accessible for audio streaming.")
            .Must(HaveAppropriateFileExtension)
            .WithMessage("URL should point to an audio file or streaming endpoint.")
            .Must(NotBeLocalhost)
            .WithMessage("Production stream URLs should not use localhost.")
            .When(x => IsProductionEnvironment());

        // Security rules
        RuleFor(x => x)
            .Must(BeSecureForProduction)
            .WithMessage("Production streams should use secure protocols (HTTPS) when possible.")
            .When(x => IsProductionEnvironment());
    }

    /// <summary>
    /// Validates that the URI is valid.
    /// </summary>
    /// <param name="uri">The URI to validate.</param>
    /// <returns>True if the URI is valid; otherwise, false.</returns>
    private static bool BeValidUri(Uri? uri)
    {
        return uri != null && uri.IsAbsoluteUri;
    }

    /// <summary>
    /// Validates that the scheme is supported for audio streaming.
    /// </summary>
    /// <param name="scheme">The scheme to validate.</param>
    /// <returns>True if the scheme is supported; otherwise, false.</returns>
    private static bool BeSupportedScheme(string scheme)
    {
        if (string.IsNullOrEmpty(scheme))
        {
            return false;
        }

        var supportedSchemes = new[] { "http", "https", "file", "ftp", "rtsp", "rtmp" };
        return supportedSchemes.Contains(scheme.ToLowerInvariant());
    }

    /// <summary>
    /// Validates that the host is valid.
    /// </summary>
    /// <param name="host">The host to validate.</param>
    /// <returns>True if the host is valid; otherwise, false.</returns>
    private static bool BeValidHost(string host)
    {
        if (string.IsNullOrEmpty(host))
        {
            return false;
        }

        // Check if it's a valid IP address
        if (System.Net.IPAddress.TryParse(host, out _))
        {
            return true;
        }

        // Check if it's a valid hostname
        return IsValidHostname(host);
    }

    /// <summary>
    /// Validates that the hostname follows DNS naming conventions.
    /// </summary>
    /// <param name="hostname">The hostname to validate.</param>
    /// <returns>True if the hostname is valid; otherwise, false.</returns>
    private static bool IsValidHostname(string hostname)
    {
        if (string.IsNullOrEmpty(hostname) || hostname.Length > 253)
        {
            return false;
        }

        // Hostname validation: letters, digits, hyphens, dots
        // Cannot start or end with hyphen, cannot have consecutive dots
        if (hostname.StartsWith('-') || hostname.EndsWith('-') || hostname.Contains(".."))
        {
            return false;
        }

        var labels = hostname.Split('.');
        foreach (var label in labels)
        {
            if (string.IsNullOrEmpty(label) || label.Length > 63)
            {
                return false;
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(label, @"^[a-zA-Z0-9-]+$"))
            {
                return false;
            }

            if (label.StartsWith('-') || label.EndsWith('-'))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Validates that the port is appropriate for the scheme.
    /// </summary>
    /// <param name="streamUrl">The stream URL to validate.</param>
    /// <param name="port">The port to validate.</param>
    /// <returns>True if the port is appropriate; otherwise, false.</returns>
    private static bool BeAppropriatePortForScheme(StreamUrl streamUrl, int port)
    {
        if (port <= 0)
        {
            return true; // Default port will be used
        }

        return streamUrl.Scheme.ToLowerInvariant() switch
        {
            "http" => port != 443, // HTTP should not use HTTPS default port
            "https" => port != 80, // HTTPS should not use HTTP default port
            "ftp" => port >= 1 && port <= 65535, // FTP can use various ports
            "rtsp" => port >= 1 && port <= 65535, // RTSP typically uses 554 but can vary
            "rtmp" => port >= 1 && port <= 65535, // RTMP typically uses 1935 but can vary
            "file" => true, // File URLs don't use ports
            _ => true,
        };
    }

    /// <summary>
    /// Validates that the URL path is valid.
    /// </summary>
    /// <param name="path">The path to validate.</param>
    /// <returns>True if the path is valid; otherwise, false.</returns>
    private static bool BeValidPath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return true; // Empty path is valid
        }

        // Check for obviously invalid characters
        var invalidChars = new[] { '<', '>', '"', '|', '\0', '\b', '\f', '\n', '\r', '\t', '\v' };
        return !invalidChars.Any(path.Contains);
    }

    /// <summary>
    /// Determines if the scheme requires a host.
    /// </summary>
    /// <param name="scheme">The scheme to check.</param>
    /// <returns>True if the scheme requires a host; otherwise, false.</returns>
    private static bool RequiresHost(string scheme)
    {
        if (string.IsNullOrEmpty(scheme))
        {
            return false;
        }

        var schemesRequiringHost = new[] { "http", "https", "ftp", "rtsp", "rtmp" };
        return schemesRequiringHost.Contains(scheme.ToLowerInvariant());
    }

    /// <summary>
    /// Validates that the URL is accessible for streaming.
    /// </summary>
    /// <param name="streamUrl">The stream URL to validate.</param>
    /// <returns>True if the URL appears accessible; otherwise, false.</returns>
    private static bool BeAccessibleUrl(StreamUrl streamUrl)
    {
        if (streamUrl.Value == null)
        {
            return false;
        }

        // Basic accessibility checks
        if (streamUrl.IsFile)
        {
            // For file URLs, just check that the path looks reasonable
            return !string.IsNullOrEmpty(streamUrl.Path);
        }

        if (streamUrl.IsHttp)
        {
            // For HTTP URLs, ensure host is not obviously invalid
            return !string.IsNullOrEmpty(streamUrl.Host)
                && streamUrl.Host != "localhost"
                && streamUrl.Host != "127.0.0.1";
        }

        // For other schemes, basic validation
        return !string.IsNullOrEmpty(streamUrl.Host);
    }

    /// <summary>
    /// Validates that the URL has an appropriate file extension for audio.
    /// </summary>
    /// <param name="streamUrl">The stream URL to validate.</param>
    /// <returns>True if the URL has an appropriate extension; otherwise, false.</returns>
    private static bool HaveAppropriateFileExtension(StreamUrl streamUrl)
    {
        if (streamUrl.Value == null)
        {
            return false;
        }

        var path = streamUrl.Path.ToLowerInvariant();

        // If no extension, might be a streaming endpoint (like Icecast)
        if (!path.Contains('.'))
        {
            return true;
        }

        // Common audio file extensions and streaming formats
        var audioExtensions = new[]
        {
            ".mp3",
            ".flac",
            ".wav",
            ".aac",
            ".ogg",
            ".m4a",
            ".wma",
            ".opus",
            ".aiff",
            ".m3u",
            ".m3u8",
            ".pls",
            ".asx",
            ".ram",
            ".ra",
        };

        return audioExtensions.Any(ext => path.EndsWith(ext));
    }

    /// <summary>
    /// Validates that the URL doesn't use localhost in production.
    /// </summary>
    /// <param name="streamUrl">The stream URL to validate.</param>
    /// <returns>True if the URL doesn't use localhost; otherwise, false.</returns>
    private static bool NotBeLocalhost(StreamUrl streamUrl)
    {
        if (streamUrl.Value == null || !streamUrl.IsHttp)
        {
            return true;
        }

        var host = streamUrl.Host.ToLowerInvariant();
        var localhostNames = new[] { "localhost", "127.0.0.1", "::1", "0.0.0.0" };
        return !localhostNames.Contains(host);
    }

    /// <summary>
    /// Validates that the URL uses secure protocols in production.
    /// </summary>
    /// <param name="streamUrl">The stream URL to validate.</param>
    /// <returns>True if the URL is secure or acceptable; otherwise, false.</returns>
    private static bool BeSecureForProduction(StreamUrl streamUrl)
    {
        if (streamUrl.Value == null)
        {
            return false;
        }

        // HTTPS is preferred for production
        if (streamUrl.IsHttp)
        {
            return streamUrl.IsSecure; // HTTPS
        }

        // Other protocols may have their own security mechanisms
        return true;
    }

    /// <summary>
    /// Determines if this is a production environment.
    /// </summary>
    /// <returns>True if this is production; otherwise, false.</returns>
    private static bool IsProductionEnvironment()
    {
        // This could be enhanced to check environment variables or configuration
        // For now, we'll be lenient and assume development
        var environment =
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? "Development";

        return environment.Equals("Production", StringComparison.OrdinalIgnoreCase);
    }
}
