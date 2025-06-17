using System;

namespace SnapDog2.Core.Models.ValueObjects;

/// <summary>
/// Represents a validated URL for audio streams.
/// Immutable value object that ensures URL format correctness and supports common audio stream protocols.
/// </summary>
public readonly struct StreamUrl : IEquatable<StreamUrl>
{
    /// <summary>
    /// Gets the underlying URI instance.
    /// </summary>
    public Uri Value { get; }

    /// <summary>
    /// Gets the scheme of the URL (http, https, file, etc.).
    /// </summary>
    public string Scheme => Value.Scheme;

    /// <summary>
    /// Gets the host component of the URL.
    /// </summary>
    public string Host => Value.Host;

    /// <summary>
    /// Gets the port number of the URL.
    /// </summary>
    public int Port => Value.Port;

    /// <summary>
    /// Gets the path component of the URL.
    /// </summary>
    public string Path => Value.AbsolutePath;

    /// <summary>
    /// Gets a value indicating whether this is an HTTP or HTTPS URL.
    /// </summary>
    public bool IsHttp =>
        string.Equals(Scheme, "http", StringComparison.OrdinalIgnoreCase)
        || string.Equals(Scheme, "https", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether this is a file URL.
    /// </summary>
    public bool IsFile => string.Equals(Scheme, "file", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether this is a secure HTTPS URL.
    /// </summary>
    public bool IsSecure => string.Equals(Scheme, "https", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamUrl"/> struct.
    /// </summary>
    /// <param name="url">The URL string to validate and parse.</param>
    /// <exception cref="ArgumentException">Thrown when the URL format is invalid or unsupported.</exception>
    public StreamUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("Stream URL cannot be null or empty.", nameof(url));
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            throw new ArgumentException($"Invalid URL format: '{url}'.", nameof(url));
        }

        if (!IsSupportedScheme(uri.Scheme))
        {
            throw new ArgumentException(
                $"Unsupported URL scheme: '{uri.Scheme}'. Supported schemes: http, https, file, ftp, rtsp, rtmp.",
                nameof(url)
            );
        }

        Value = uri;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamUrl"/> struct.
    /// </summary>
    /// <param name="uri">The URI instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when the URI is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the URI scheme is unsupported.</exception>
    public StreamUrl(Uri uri)
    {
        if (uri == null)
        {
            throw new ArgumentNullException(nameof(uri));
        }

        if (!IsSupportedScheme(uri.Scheme))
        {
            throw new ArgumentException(
                $"Unsupported URL scheme: '{uri.Scheme}'. Supported schemes: http, https, file, ftp, rtsp, rtmp.",
                nameof(uri)
            );
        }

        Value = uri;
    }

    /// <summary>
    /// Determines if a URL scheme is supported for audio streams.
    /// </summary>
    /// <param name="scheme">The URL scheme to check.</param>
    /// <returns>True if the scheme is supported; otherwise, false.</returns>
    private static bool IsSupportedScheme(string scheme)
    {
        return scheme.ToLowerInvariant() switch
        {
            "http" => true,
            "https" => true,
            "file" => true,
            "ftp" => true,
            "rtsp" => true,
            "rtmp" => true,
            _ => false,
        };
    }

    /// <summary>
    /// Validates if a string represents a valid stream URL.
    /// </summary>
    /// <param name="url">The URL string to validate.</param>
    /// <returns>True if the URL is valid; otherwise, false.</returns>
    public static bool IsValid(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        return IsSupportedScheme(uri.Scheme);
    }

    /// <summary>
    /// Parses a URL string and returns a <see cref="StreamUrl"/> instance.
    /// </summary>
    /// <param name="url">The URL string to parse.</param>
    /// <returns>A <see cref="StreamUrl"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the URL format is invalid.</exception>
    public static StreamUrl Parse(string url)
    {
        return new StreamUrl(url);
    }

    /// <summary>
    /// Tries to parse a URL string and returns a <see cref="StreamUrl"/> instance.
    /// </summary>
    /// <param name="url">The URL string to parse.</param>
    /// <param name="result">The parsed <see cref="StreamUrl"/> if successful.</param>
    /// <returns>True if parsing was successful; otherwise, false.</returns>
    public static bool TryParse(string? url, out StreamUrl result)
    {
        result = default;

        if (!IsValid(url))
        {
            return false;
        }

        try
        {
            result = new StreamUrl(url!);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Returns the string representation of the stream URL.
    /// </summary>
    /// <returns>The absolute URL string.</returns>
    public override string ToString() => Value.AbsoluteUri;

    /// <summary>
    /// Determines whether the specified object is equal to the current <see cref="StreamUrl"/>.
    /// </summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns>True if the objects are equal; otherwise, false.</returns>
    public override bool Equals(object? obj) => obj is StreamUrl other && Equals(other);

    /// <summary>
    /// Determines whether the specified <see cref="StreamUrl"/> is equal to the current instance.
    /// </summary>
    /// <param name="other">The <see cref="StreamUrl"/> to compare.</param>
    /// <returns>True if the URLs are equal; otherwise, false.</returns>
    public bool Equals(StreamUrl other) => Value.Equals(other.Value);

    /// <summary>
    /// Returns the hash code for the current <see cref="StreamUrl"/>.
    /// </summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode() => Value.GetHashCode();

    /// <summary>
    /// Determines whether two <see cref="StreamUrl"/> instances are equal.
    /// </summary>
    /// <param name="left">The first stream URL.</param>
    /// <param name="right">The second stream URL.</param>
    /// <returns>True if the URLs are equal; otherwise, false.</returns>
    public static bool operator ==(StreamUrl left, StreamUrl right) => left.Equals(right);

    /// <summary>
    /// Determines whether two <see cref="StreamUrl"/> instances are not equal.
    /// </summary>
    /// <param name="left">The first stream URL.</param>
    /// <param name="right">The second stream URL.</param>
    /// <returns>True if the URLs are not equal; otherwise, false.</returns>
    public static bool operator !=(StreamUrl left, StreamUrl right) => !left.Equals(right);

    /// <summary>
    /// Implicitly converts a string to a <see cref="StreamUrl"/>.
    /// </summary>
    /// <param name="url">The URL string.</param>
    /// <returns>A <see cref="StreamUrl"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the URL format is invalid.</exception>
    public static implicit operator StreamUrl(string url) => new(url);

    /// <summary>
    /// Implicitly converts a <see cref="StreamUrl"/> to a string.
    /// </summary>
    /// <param name="streamUrl">The stream URL.</param>
    /// <returns>The string representation of the URL.</returns>
    public static implicit operator string(StreamUrl streamUrl) => streamUrl.ToString();

    /// <summary>
    /// Implicitly converts a URI to a <see cref="StreamUrl"/>.
    /// </summary>
    /// <param name="uri">The URI instance.</param>
    /// <returns>A <see cref="StreamUrl"/> instance.</returns>
    public static implicit operator StreamUrl(Uri uri) => new(uri);

    /// <summary>
    /// Implicitly converts a <see cref="StreamUrl"/> to a URI.
    /// </summary>
    /// <param name="streamUrl">The stream URL.</param>
    /// <returns>The URI instance.</returns>
    public static implicit operator Uri(StreamUrl streamUrl) => streamUrl.Value;
}
