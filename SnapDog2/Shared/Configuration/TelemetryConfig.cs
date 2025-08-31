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
namespace SnapDog2.Shared.Configuration;

using EnvoyConfig.Attributes;

/// <summary>
/// Telemetry and observability configuration.
/// SnapDog2 uses OpenTelemetry Protocol (OTLP) for vendor-neutral telemetry export.
/// The backend (Jaeger, SigNoz, etc.) is configured in the deployment environment.
/// </summary>
public class TelemetryConfig
{
    /// <summary>
    /// Whether telemetry is enabled.
    /// Maps to: SNAPDOG_TELEMETRY_ENABLED
    /// </summary>
    [Env(Key = "ENABLED", Default = false)]
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Service name for telemetry.
    /// Maps to: SNAPDOG_TELEMETRY_SERVICE_NAME
    /// </summary>
    [Env(Key = "SERVICE_NAME", Default = "SnapDog2")]
    public string ServiceName { get; set; } = "SnapDog2";

    /// <summary>
    /// Sampling rate for traces (0.0 to 1.0).
    /// Maps to: SNAPDOG_TELEMETRY_SAMPLING_RATE
    /// </summary>
    [Env(Key = "SAMPLING_RATE", Default = 1.0)]
    public double SamplingRate { get; set; } = 1.0;

    /// <summary>
    /// OTLP configuration for unified telemetry export.
    /// Maps environment variables with prefix: SNAPDOG_TELEMETRY_OTLP_*
    /// </summary>
    [Env(NestedPrefix = "OTLP_")]
    public OtlpConfig Otlp { get; set; } = new();
}

/// <summary>
/// OTLP configuration for vendor-neutral telemetry export.
/// Supports any OTLP-compatible backend (Jaeger, SigNoz, etc.).
/// </summary>
public class OtlpConfig
{
    /// <summary>
    /// OTLP endpoint URL for traces and metrics.
    /// Maps to: SNAPDOG_TELEMETRY_OTLP_ENDPOINT
    /// Examples:
    /// - Jaeger: http://jaeger:14268/api/traces
    /// - SigNoz: http://otel-collector:4317
    /// - Local collector: http://localhost:4317
    /// </summary>
    [Env(Key = "ENDPOINT", Default = "http://localhost:4317")]
    public string Endpoint { get; set; } = "http://localhost:4317";

    /// <summary>
    /// OTLP protocol (grpc or http/protobuf).
    /// Maps to: SNAPDOG_TELEMETRY_OTLP_PROTOCOL
    /// </summary>
    [Env(Key = "PROTOCOL", Default = "grpc")]
    public string Protocol { get; set; } = "grpc";

    /// <summary>
    /// OTLP headers for authentication (e.g., API keys).
    /// Maps to: SNAPDOG_TELEMETRY_OTLP_HEADERS
    /// Format: "key1=value1,key2=value2"
    /// </summary>
    [Env(Key = "HEADERS")]
    public string? Headers { get; set; }

    /// <summary>
    /// Export timeout in seconds.
    /// Maps to: SNAPDOG_TELEMETRY_OTLP_TIMEOUT
    /// </summary>
    [Env(Key = "TIMEOUT", Default = 30)]
    public int TimeoutSeconds { get; set; } = 30;
}
