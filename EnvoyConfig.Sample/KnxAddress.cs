using System;
using System.Globalization;

namespace EnvoyConfig.Sample
{
    /// <summary>
    /// Represents a KNX group address in the format Main/Middle/Sub (e.g., "2/1/1").
    /// </summary>
    public readonly struct KnxAddress : IEquatable<KnxAddress>
    {
        /// <summary>
        /// Gets the main group (0-31).
        /// </summary>
        public int Main { get; }

        /// <summary>
        /// Gets the middle group (0-7).
        /// </summary>
        public int Middle { get; }

        /// <summary>
        /// Gets the sub group (0-255).
        /// </summary>
        public int Sub { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="KnxAddress"/> struct.
        /// </summary>
        /// <param name="main">The main group (0-31).</param>
        /// <param name="middle">The middle group (0-7).</param>
        /// <param name="sub">The sub group (0-255).</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when any parameter is out of valid range.</exception>
        public KnxAddress(int main, int middle, int sub)
        {
            if (main < 0 || main > 31)
                throw new ArgumentOutOfRangeException(nameof(main), "Main group must be between 0 and 31.");
            if (middle < 0 || middle > 7)
                throw new ArgumentOutOfRangeException(nameof(middle), "Middle group must be between 0 and 7.");
            if (sub < 0 || sub > 255)
                throw new ArgumentOutOfRangeException(nameof(sub), "Sub group must be between 0 and 255.");

            Main = main;
            Middle = middle;
            Sub = sub;
        }

        /// <summary>
        /// Parses a KNX address string in the format "Main/Middle/Sub".
        /// </summary>
        /// <param name="address">The address string to parse.</param>
        /// <returns>A <see cref="KnxAddress"/> instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown when address is null.</exception>
        /// <exception cref="FormatException">Thrown when address format is invalid.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when any component is out of valid range.</exception>
        public static KnxAddress Parse(string address)
        {
            if (TryParse(address, out var result))
                return result;

            throw new FormatException(
                $"Invalid KNX address format: '{address}'. Expected format: 'Main/Middle/Sub' (e.g., '2/1/1')."
            );
        }

        /// <summary>
        /// Tries to parse a KNX address string in the format "Main/Middle/Sub".
        /// </summary>
        /// <param name="address">The address string to parse.</param>
        /// <param name="result">The parsed <see cref="KnxAddress"/> if successful.</param>
        /// <returns>True if parsing was successful; otherwise, false.</returns>
        public static bool TryParse(string? address, out KnxAddress result)
        {
            result = default;

            if (string.IsNullOrWhiteSpace(address))
                return false;

            var parts = address.Split('/');
            if (parts.Length != 3)
                return false;

            if (
                !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var main)
                || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var middle)
                || !int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out var sub)
            )
                return false;

            if (main < 0 || main > 31 || middle < 0 || middle > 7 || sub < 0 || sub > 255)
                return false;

            result = new KnxAddress(main, middle, sub);
            return true;
        }

        /// <summary>
        /// Returns the string representation of the KNX address in the format "Main/Middle/Sub".
        /// </summary>
        /// <returns>The string representation of the address.</returns>
        public override string ToString() => $"{Main}/{Middle}/{Sub}";

        /// <summary>
        /// Determines whether the specified object is equal to the current <see cref="KnxAddress"/>.
        /// </summary>
        /// <param name="obj">The object to compare.</param>
        /// <returns>True if the objects are equal; otherwise, false.</returns>
        public override bool Equals(object? obj) => obj is KnxAddress other && Equals(other);

        /// <summary>
        /// Determines whether the specified <see cref="KnxAddress"/> is equal to the current instance.
        /// </summary>
        /// <param name="other">The <see cref="KnxAddress"/> to compare.</param>
        /// <returns>True if the addresses are equal; otherwise, false.</returns>
        public bool Equals(KnxAddress other) => Main == other.Main && Middle == other.Middle && Sub == other.Sub;

        /// <summary>
        /// Returns the hash code for the current <see cref="KnxAddress"/>.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode() => HashCode.Combine(Main, Middle, Sub);

        /// <summary>
        /// Determines whether two <see cref="KnxAddress"/> instances are equal.
        /// </summary>
        /// <param name="left">The first address.</param>
        /// <param name="right">The second address.</param>
        /// <returns>True if the addresses are equal; otherwise, false.</returns>
        public static bool operator ==(KnxAddress left, KnxAddress right) => left.Equals(right);

        /// <summary>
        /// Determines whether two <see cref="KnxAddress"/> instances are not equal.
        /// </summary>
        /// <param name="left">The first address.</param>
        /// <param name="right">The second address.</param>
        /// <returns>True if the addresses are not equal; otherwise, false.</returns>
        public static bool operator !=(KnxAddress left, KnxAddress right) => !left.Equals(right);

        /// <summary>
        /// Implicitly converts a string to a <see cref="KnxAddress"/>.
        /// </summary>
        /// <param name="address">The address string.</param>
        /// <returns>A <see cref="KnxAddress"/> instance.</returns>
        /// <exception cref="FormatException">Thrown when the address format is invalid.</exception>
        public static implicit operator KnxAddress(string address) => Parse(address);

        /// <summary>
        /// Implicitly converts a <see cref="KnxAddress"/> to a string.
        /// </summary>
        /// <param name="address">The address.</param>
        /// <returns>The string representation of the address.</returns>
        public static implicit operator string(KnxAddress address) => address.ToString();
    }
}
