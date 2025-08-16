# SnapDog2 Observability Setup

## Overview

SnapDog2 uses **OpenTelemetry Protocol (OTLP)** for vendor-neutral telemetry export. The application only knows about OTLP - the choice of observability backend (Jaeger, SigNoz, etc.) is purely a deployment concern.

## Architecture

```
SnapDog2 Application → OTLP → Observability Backend
```

- **SnapDog2**: Only implements OTLP export
- **OTLP**: Vendor-neutral telemetry protocol
- **Backend**: Jaeger, SigNoz, Prometheus, etc. (deployment choice)

## Configuration

### Environment Variables

SnapDog2 uses structured environment variables with the `SNAPDOG_TELEMETRY_` prefix:

```bash
# Enable telemetry
SNAPDOG_TELEMETRY_ENABLED=true
SNAPDOG_TELEMETRY_SERVICE_NAME=SnapDog2
SNAPDOG_TELEMETRY_SAMPLING_RATE=1.0

# OTLP Configuration (vendor-neutral)
SNAPDOG_TELEMETRY_OTLP_ENABLED=true
SNAPDOG_TELEMETRY_OTLP_ENDPOINT=http://localhost:4317
SNAPDOG_TELEMETRY_OTLP_PROTOCOL=grpc
SNAPDOG_TELEMETRY_OTLP_HEADERS=key1=value1,key2=value2  # Optional
SNAPDOG_TELEMETRY_OTLP_TIMEOUT=30
```

### Backend-Specific Endpoints

Just change the endpoint to switch backends:

```bash
# Jaeger
SNAPDOG_TELEMETRY_OTLP_ENDPOINT=http://jaeger:14268/api/traces
SNAPDOG_TELEMETRY_OTLP_PROTOCOL=http/protobuf

# SigNoz
SNAPDOG_TELEMETRY_OTLP_ENDPOINT=http://signoz-otel-collector:4317
SNAPDOG_TELEMETRY_OTLP_PROTOCOL=grpc

# Local OpenTelemetry Collector
SNAPDOG_TELEMETRY_OTLP_ENDPOINT=http://localhost:4317
SNAPDOG_TELEMETRY_OTLP_PROTOCOL=grpc
```

## Development Usage

### Option 1: Jaeger (Legacy)
```bash
# Load Jaeger configuration
source .env.jaeger
make dev-with-monitoring
```
Access: http://localhost:8000/tracing/

### Option 2: SigNoz (Recommended)
```bash
# Load SigNoz configuration  
source .env.signoz
make dev-with-signoz
```
Access: http://localhost:8000/signoz/

### Option 3: No Observability
```bash
make dev
```

## What SnapDog2 Exports

### Traces
- HTTP requests (ASP.NET Core)
- HTTP client calls
- Custom application traces (SnapDog2.*)

### Metrics
- HTTP request metrics
- HTTP client metrics  
- Custom application metrics (SnapDog2.*)

### Logs
- Structured logs with trace correlation (via Serilog)

## Benefits of OTLP-Only Approach

✅ **Vendor Neutral**: Switch backends without code changes
✅ **Simple Configuration**: Just environment variables
✅ **Clean Separation**: Application vs. infrastructure concerns
✅ **Future Proof**: Works with any OTLP-compatible backend
✅ **Development Friendly**: Easy to enable/disable/switch

## Implementation Details

### OpenTelemetry Packages Used
- `OpenTelemetry` - Core library
- `OpenTelemetry.Extensions.Hosting` - ASP.NET Core integration
- `OpenTelemetry.Instrumentation.AspNetCore` - HTTP instrumentation
- `OpenTelemetry.Instrumentation.Http` - HTTP client instrumentation
- `OpenTelemetry.Exporter.OpenTelemetryProtocol` - OTLP exporter

### Configuration Class
```csharp
public class TelemetryConfig
{
    public bool Enabled { get; set; }
    public string ServiceName { get; set; }
    public double SamplingRate { get; set; }
    public OtlpConfig Otlp { get; set; }
}

public class OtlpConfig
{
    public bool Enabled { get; set; }
    public string Endpoint { get; set; }
    public string Protocol { get; set; }
    public string? Headers { get; set; }
    public int TimeoutSeconds { get; set; }
}
```

## Removed Legacy Configuration

The following configuration options were removed to keep SnapDog2 focused on OTLP:

- ❌ `PrometheusConfig` - Use OTLP → Collector → Prometheus instead
- ❌ `SeqConfig` - Use structured logging with OTLP correlation instead
- ❌ Jaeger-specific configuration - Use OTLP → Jaeger instead

This keeps SnapDog2 simple and vendor-neutral while supporting all observability backends through OTLP.
