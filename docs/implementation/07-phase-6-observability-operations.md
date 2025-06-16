# Phase 6: Observability & Operations

## Overview

Phase 6 implements production-grade observability with OpenTelemetry, Docker infrastructure, and comprehensive monitoring. This phase prepares SnapDog for production deployment with full operational visibility.

**Deliverable**: Production-ready system with complete observability, monitoring, and Docker deployment.

## Objectives

### Primary Goals

- [ ] Implement complete OpenTelemetry integration (traces, metrics, logs)
- [ ] Create Docker infrastructure with compose orchestration
- [ ] Setup production monitoring with Prometheus and Grafana
- [ ] Implement comprehensive health checks and alerting
- [ ] Create deployment automation and CI/CD pipeline
- [ ] Establish operational runbooks and documentation

### Success Criteria

- Full observability stack operational
- Docker deployment working in production
- Monitoring dashboards providing insights
- Alerting system responding to issues
- CI/CD pipeline deploying successfully
- Performance monitoring meeting requirements

## Implementation Steps

### Step 1: OpenTelemetry Integration

#### 1.1 Telemetry Configuration

```csharp
namespace SnapDog.Infrastructure.Telemetry;

public static class TelemetryExtensions
{
    public static IServiceCollection AddSnapDogTelemetry(
        this IServiceCollection services,
        SnapDogConfiguration config)
    {
        if (!config.TelemetryEnabled) return services;

        services.AddOpenTelemetry()
            .WithTracing(builder =>
            {
                builder
                    .AddSource("SnapDog.*")
                    .SetSampler(new TraceIdRatioBasedSampler(config.TelemetrySamplingRate))
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.Filter = (httpContext) =>
                            !httpContext.Request.Path.Value?.Contains("/health") == true;
                        options.EnrichWithHttpRequest = EnrichWithHttpRequest;
                        options.EnrichWithHttpResponse = EnrichWithHttpResponse;
                    })
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation()
                    .AddJaegerExporter(options =>
                    {
                        options.Endpoint = new Uri(config.JaegerEndpoint ?? "http://localhost:14268");
                    });
            })
            .WithMetrics(builder =>
            {
                builder
                    .AddMeter("SnapDog.*")
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddPrometheusExporter(options =>
                    {
                        options.HttpListenerPrefixes = new[] { $"http://+:{config.PrometheusPort}/metrics/" };
                    });
            });

        // Custom metrics
        services.AddSingleton<SnapDogMetrics>();

        return services;
    }

    private static void EnrichWithHttpRequest(Activity activity, HttpRequest httpRequest)
    {
        activity.SetTag("http.request.client_ip", httpRequest.HttpContext.Connection.RemoteIpAddress?.ToString());
        activity.SetTag("http.request.user_agent", httpRequest.Headers.UserAgent.ToString());
    }

    private static void EnrichWithHttpResponse(Activity activity, HttpResponse httpResponse)
    {
        activity.SetTag("http.response.status_code", httpResponse.StatusCode);
    }
}

/// <summary>
/// Custom metrics for SnapDog system monitoring.
/// </summary>
public class SnapDogMetrics
{
    private readonly Meter _meter;
    private readonly Counter<long> _streamStartCounter;
    private readonly Histogram<double> _audioLatencyHistogram;
    private readonly ObservableGauge<int> _activeStreamsGauge;
    private readonly ObservableGauge<int> _connectedClientsGauge;
    private readonly Counter<long> _protocolCommandCounter;

    public SnapDogMetrics()
    {
        _meter = new Meter("SnapDog.Metrics", "1.0.0");

        _streamStartCounter = _meter.CreateCounter<long>(
            "snapdog_streams_started_total",
            "Total number of audio streams started");

        _audioLatencyHistogram = _meter.CreateHistogram<double>(
            "snapdog_audio_latency_ms",
            "Audio processing latency in milliseconds");

        _activeStreamsGauge = _meter.CreateObservableGauge<int>(
            "snapdog_active_streams",
            "Number of currently active audio streams",
            GetActiveStreamsCount);

        _connectedClientsGauge = _meter.CreateObservableGauge<int>(
            "snapdog_connected_clients",
            "Number of connected audio clients",
            GetConnectedClientsCount);

        _protocolCommandCounter = _meter.CreateCounter<long>(
            "snapdog_protocol_commands_total",
            "Total number of protocol commands processed");
    }

    public void RecordStreamStart(string streamName, string codec)
    {
        _streamStartCounter.Add(1, new TagList
        {
            ["stream_name"] = streamName,
            ["codec"] = codec
        });
    }

    public void RecordAudioLatency(double latencyMs, string operation)
    {
        _audioLatencyHistogram.Record(latencyMs, new TagList
        {
            ["operation"] = operation
        });
    }

    public void RecordProtocolCommand(string protocol, string command, bool success)
    {
        _protocolCommandCounter.Add(1, new TagList
        {
            ["protocol"] = protocol,
            ["command"] = command,
            ["success"] = success.ToString()
        });
    }
}
```

### Step 2: Docker Infrastructure

#### 2.1 Production Dockerfile

```dockerfile
# Production Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5000
EXPOSE 5001
EXPOSE 9090

# Install LibVLC for audio processing
RUN apt-get update && apt-get install -y \
    vlc-bin \
    vlc-plugin-base \
    vlc-plugin-video-output \
    && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["SnapDog/SnapDog.csproj", "SnapDog/"]
RUN dotnet restore "SnapDog/SnapDog.csproj"
COPY . .
WORKDIR "/src/SnapDog"
RUN dotnet build "SnapDog.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SnapDog.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:5000/health || exit 1

ENTRYPOINT ["dotnet", "SnapDog.dll"]
```

#### 2.2 Docker Compose Configuration

```yaml
# docker-compose.yml
version: '3.8'

services:
  snapdog:
    build: .
    container_name: snapdog
    ports:
      - "5000:5000"
      - "5001:5001"
      - "9090:9090"
    environment:
      - SNAPDOG_ENVIRONMENT=Production
      - SNAPDOG_LOG_LEVEL=Information
      - SNAPCAST_SERVER_HOST=snapcast-server
      - MQTT_BROKER_HOST=mosquitto
      - DATABASE_CONNECTION_STRING=Server=sqlserver;Database=SnapDog;User=sa;Password=YourStrong@Passw0rd;
      - JAEGER_ENDPOINT=http://jaeger:14268
    depends_on:
      - sqlserver
      - mosquitto
      - snapcast-server
      - jaeger
    networks:
      - snapdog-network
    restart: unless-stopped
    volumes:
      - ./logs:/app/logs
      - /tmp/snapdog:/tmp/snapdog

  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    container_name: snapdog-db
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=YourStrong@Passw0rd
      - MSSQL_PID=Express
    ports:
      - "1433:1433"
    volumes:
      - sqlserver_data:/var/opt/mssql
    networks:
      - snapdog-network

  mosquitto:
    image: eclipse-mosquitto:2.0
    container_name: snapdog-mqtt
    ports:
      - "1883:1883"
      - "9001:9001"
    volumes:
      - ./docker/mosquitto/mosquitto.conf:/mosquitto/config/mosquitto.conf
      - mosquitto_data:/mosquitto/data
      - mosquitto_logs:/mosquitto/log
    networks:
      - snapdog-network

  snapcast-server:
    image: saiyato/snapserver:latest
    container_name: snapdog-snapcast
    ports:
      - "1704:1704"
      - "1705:1705"
    volumes:
      - ./docker/snapcast-server/snapserver.conf:/etc/snapserver.conf
      - /tmp/snapdog:/tmp/snapcast
    networks:
      - snapdog-network

  prometheus:
    image: prom/prometheus:latest
    container_name: snapdog-prometheus
    ports:
      - "9091:9090"
    volumes:
      - ./docker/observability/prometheus/prometheus.yml:/etc/prometheus/prometheus.yml
      - prometheus_data:/prometheus
    command:
      - '--config.file=/etc/prometheus/prometheus.yml'
      - '--storage.tsdb.path=/prometheus'
      - '--web.console.libraries=/etc/prometheus/console_libraries'
      - '--web.console.templates=/etc/prometheus/consoles'
      - '--storage.tsdb.retention.time=200h'
      - '--web.enable-lifecycle'
    networks:
      - snapdog-network

  grafana:
    image: grafana/grafana:latest
    container_name: snapdog-grafana
    ports:
      - "3000:3000"
    environment:
      - GF_SECURITY_ADMIN_PASSWORD=admin
    volumes:
      - grafana_data:/var/lib/grafana
      - ./docker/observability/grafana/dashboards:/etc/grafana/provisioning/dashboards
      - ./docker/observability/grafana/datasources:/etc/grafana/provisioning/datasources
    networks:
      - snapdog-network

  jaeger:
    image: jaegertracing/all-in-one:latest
    container_name: snapdog-jaeger
    ports:
      - "16686:16686"
      - "14268:14268"
    environment:
      - COLLECTOR_OTLP_ENABLED=true
    networks:
      - snapdog-network

volumes:
  sqlserver_data:
  mosquitto_data:
  mosquitto_logs:
  prometheus_data:
  grafana_data:

networks:
  snapdog-network:
    driver: bridge
```

### Step 3: Monitoring Configuration

#### 3.1 Prometheus Configuration

```yaml
# docker/observability/prometheus/prometheus.yml
global:
  scrape_interval: 15s
  evaluation_interval: 15s

rule_files:
  - "alert_rules.yml"

alerting:
  alertmanagers:
    - static_configs:
        - targets:
          - alertmanager:9093

scrape_configs:
  - job_name: 'snapdog'
    static_configs:
      - targets: ['snapdog:9090']
    scrape_interval: 5s
    metrics_path: /metrics

  - job_name: 'snapcast'
    static_configs:
      - targets: ['snapcast-server:1705']
    scrape_interval: 10s

  - job_name: 'prometheus'
    static_configs:
      - targets: ['localhost:9090']
```

#### 3.2 Grafana Dashboard Configuration

```json
{
  "dashboard": {
    "title": "SnapDog System Overview",
    "tags": ["snapdog", "audio", "monitoring"],
    "panels": [
      {
        "title": "Active Audio Streams",
        "type": "stat",
        "targets": [
          {
            "expr": "snapdog_active_streams",
            "legendFormat": "Active Streams"
          }
        ]
      },
      {
        "title": "Audio Processing Latency",
        "type": "graph",
        "targets": [
          {
            "expr": "histogram_quantile(0.95, rate(snapdog_audio_latency_ms_bucket[5m]))",
            "legendFormat": "95th percentile"
          },
          {
            "expr": "histogram_quantile(0.50, rate(snapdog_audio_latency_ms_bucket[5m]))",
            "legendFormat": "50th percentile"
          }
        ]
      },
      {
        "title": "Protocol Command Success Rate",
        "type": "graph",
        "targets": [
          {
            "expr": "rate(snapdog_protocol_commands_total{success=\"true\"}[5m]) / rate(snapdog_protocol_commands_total[5m]) * 100",
            "legendFormat": "Success Rate %"
          }
        ]
      }
    ]
  }
}
```

### Step 4: CI/CD Pipeline

#### 4.1 GitHub Actions Workflow

```yaml
# .github/workflows/deploy.yml
name: Build and Deploy SnapDog

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

env:
  DOCKER_IMAGE: snapdog/snapdog-system
  DOCKER_TAG: ${{ github.sha }}

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v3

    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 9.0.x

    - name: Restore dependencies
      run: dotnet restore

    - name: Build
      run: dotnet build --no-restore

    - name: Run tests
      run: dotnet test --no-build --verbosity normal --collect:"XPlat Code Coverage"

    - name: Upload coverage to Codecov
      uses: codecov/codecov-action@v3

  build-and-push:
    needs: test
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main'

    steps:
    - uses: actions/checkout@v3

    - name: Set up Docker Buildx
      uses: docker/setup-buildx-action@v2

    - name: Login to Docker Hub
      uses: docker/login-action@v2
      with:
        username: ${{ secrets.DOCKER_USERNAME }}
        password: ${{ secrets.DOCKER_PASSWORD }}

    - name: Build and push Docker image
      uses: docker/build-push-action@v4
      with:
        context: .
        push: true
        tags: |
          ${{ env.DOCKER_IMAGE }}:latest
          ${{ env.DOCKER_IMAGE }}:${{ env.DOCKER_TAG }}
        cache-from: type=gha
        cache-to: type=gha,mode=max

  deploy:
    needs: build-and-push
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main'

    steps:
    - name: Deploy to production
      uses: appleboy/ssh-action@v0.1.5
      with:
        host: ${{ secrets.PRODUCTION_HOST }}
        username: ${{ secrets.PRODUCTION_USER }}
        key: ${{ secrets.PRODUCTION_SSH_KEY }}
        script: |
          cd /opt/snapdog
          docker-compose pull
          docker-compose up -d
          docker system prune -f
```

## Expected Deliverable

### Production Deployment Status

```
SnapDog Production Deployment Status
===================================
🟢 Container Runtime  - Docker Swarm
   ├── Services: 7    - All healthy
   ├── Replicas: 3    - Load balanced
   └── Uptime: 99.9%  - SLA met

🟢 Observability Stack - Full monitoring
   ├── Traces: ✅     - Jaeger (sub-second queries)
   ├── Metrics: ✅    - Prometheus (5s resolution)
   ├── Logs: ✅       - Centralized logging
   └── Dashboards: ✅ - Grafana (real-time)

🟢 Performance Metrics - Within targets
   ├── API Latency: 45ms avg (target <100ms)
   ├── Audio Latency: 8ms avg (target <10ms)
   ├── Memory Usage: 512MB (target <1GB)
   └── CPU Usage: 35% avg (target <50%)

🟢 Health Checks     - All systems green
   ├── Database: ✅   - Response time 12ms
   ├── External APIs: ✅ - All reachable
   ├── Protocol Services: ✅ - All connected
   └── System Resources: ✅ - Within limits

🟢 Security         - Production ready
   ├── TLS: ✅        - Certificates valid
   ├── Authentication: ✅ - JWT working
   ├── Firewalls: ✅   - Properly configured
   └── Vulnerability Scan: ✅ - No critical issues

CI/CD Pipeline: ✅ Automated deployment working
Backup Strategy: ✅ Daily automated backups
Monitoring Alerts: ✅ Real-time notifications
Documentation: ✅ Runbooks and procedures
```

### Test Results

```
Phase 6 Test Results:
===================
Observability Tests: 25/25 passed
Docker Infrastructure: 15/15 passed
Monitoring Integration: 20/20 passed
CI/CD Pipeline Tests: 12/12 passed
Performance Tests: 18/18 passed
Security Tests: 10/10 passed

Total Tests: 100/100 passed
Code Coverage: 92%
Infrastructure Score: 100%
Security Score: A+
```

## Quality Gates

- [ ] Complete observability stack operational
- [ ] Docker deployment working in production
- [ ] Performance monitoring meeting requirements
- [ ] Security standards implemented
- [ ] CI/CD pipeline functioning
- [ ] Operational procedures documented

## Next Steps

Phase 6 delivers a production-ready system with comprehensive observability. Proceed to Phase 7 for advanced features and optimization.
