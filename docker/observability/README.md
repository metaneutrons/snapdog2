# SnapDog2 Observability Infrastructure

This directory contains Docker configuration for the observability infrastructure used by SnapDog2, including:
- Prometheus - for metrics collection
- Jaeger - for distributed tracing
- Grafana - for visualization dashboards

## Quick Start

1. Start the observability stack:
   ```bash
   cd docker/observability
   docker-compose up -d
   ```

2. Configure SnapDog2 to send telemetry:
   ```
   # Set environment variables
   SNAPDOG_TELEMETRY_ENABLED=true
   SNAPDOG_TELEMETRY_SERVICE_NAME=SnapDog2
   SNAPDOG_TELEMETRY_SAMPLING_RATE=1.0
   SNAPDOG_PROMETHEUS_ENABLED=true
   SNAPDOG_PROMETHEUS_PATH=/metrics
   SNAPDOG_PROMETHEUS_PORT=9091
   SNAPDOG_JAEGER_ENABLED=true
   SNAPDOG_JAEGER_AGENT_HOST=localhost
   SNAPDOG_JAEGER_AGENT_PORT=6831
   ```

3. Access the dashboards:
   - Prometheus: http://localhost:9090
   - Jaeger UI: http://localhost:16686
   - Grafana: http://localhost:3000 (admin/admin)

## Infrastructure Components

### Prometheus

Prometheus is configured to scrape metrics from the SnapDog2 API's `/metrics` endpoint on port 9091.

The configuration is in `prometheus/prometheus.yml`.

### Jaeger

Jaeger collects and visualizes distributed traces, allowing you to follow requests as they flow through the system.

The Jaeger UI is available at http://localhost:16686. Use it to:
- Search for traces
- View trace timelines
- Analyze performance bottlenecks

### Grafana

Grafana provides dashboards for visualizing metrics and traces. Default credentials are admin/admin.

## Collected Metrics

SnapDog2 collects the following metrics via OpenTelemetry:

- **API metrics**:
  - Request counts, durations, and error rates
  - Response status codes

- **MediatR metrics**:
  - Command/query execution counts and durations
  - Pipeline behavior performance

- **Integration metrics**:
  - Snapcast, MQTT, KNX connections and operations
  - External API call performance

- **System metrics**:
  - Runtime metrics (memory, GC, thread pool)
  - Resource utilization