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

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SnapDog2.Infrastructure.Serialization;

/// <summary>
/// Custom DateTime JSON converter that serializes DateTime values in standard ISO8601 format
/// with millisecond precision (3 decimal places) for compatibility with Swift's ISO8601DateFormatter.
/// 
/// This fixes the issue where .NET's default DateTime serialization uses microsecond precision
/// (7 decimal places) which is not compatible with standard ISO8601 parsers.
/// </summary>
public class Iso8601DateTimeConverter : JsonConverter<DateTime>
{
    private const string DateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var dateString = reader.GetString();
        if (string.IsNullOrEmpty(dateString))
        {
            throw new JsonException("DateTime value cannot be null or empty");
        }

        // Try to parse with standard ISO8601 format first
        if (DateTime.TryParseExact(dateString, DateTimeFormat, null, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var result))
        {
            return result;
        }

        // Fallback to default parsing for backward compatibility
        if (DateTime.TryParse(dateString, null, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out result))
        {
            return result;
        }

        throw new JsonException($"Unable to parse DateTime value: {dateString}");
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        // Always serialize in UTC with standard ISO8601 format (3 decimal places)
        var utcDateTime = value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
        writer.WriteStringValue(utcDateTime.ToString(DateTimeFormat));
    }
}
