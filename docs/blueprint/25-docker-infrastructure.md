# 31. Docker Infrastructure

SnapDog2 is designed primarily for containerized deployment using Docker and Docker Compose. This approach provides process isolation, simplifies dependency management (especially for native libraries like LibVLC and external services like Snapcast/MQTT), ensures environment consistency, and facilitates scalable deployments.

## 31.1. Container Architecture Overview

The recommended production deployment consists of several collaborating containers orchestrated by Docker Compose, running on a shared Docker network (e.g., `snapdog_net`) for inter-service communication.

**Core Containers:**

1. **`snapdog`**: The main application container built from `/docker/snapdog/Dockerfile`. Runs the .NET 9 SnapDog2 application (`SnapDog2.Worker`). Connects to other services on the Docker network. Requires access to mounted FIFO pipes for audio output if `snapserver` sinks are file-based.
2. **`snapserver`**: Runs the Snapcast server. This typically uses a **customized Docker image** (built from `/docker/snapserver/Dockerfile` or similar) to ensure correct configuration for SnapDog2's zones/sink requirements.
3. **`mosquitto`**: Runs the Eclipse Mosquitto MQTT broker (or another MQTT broker). Used for command/status communication if MQTT is enabled.

**Optional Supporting Containers (Enabled via Docker Compose Profiles):**

4. **`navidrome`**: Runs the Navidrome media server (Subsonic API compatible) if Subsonic integration is enabled. Requires volume mounts for its configuration/database and the music library.
5. **`signoz-clickhouse`**: ClickHouse database for SigNoz observability platform (if telemetry enabled).
6. **`signoz-otel-collector`**: OpenTelemetry Collector that receives traces/metrics via OTLP from the `snapdog` container and forwards to ClickHouse.
7. **`signoz-query-service`**: SigNoz query service for processing observability data.
8. **`signoz-frontend`**: SigNoz web UI for unified observability (traces, metrics, logs).

```mermaid
graph TD
    subgraph "Docker Host / Network: snapdog_net"
        APP[snapdog Container\n(.NET App)]
        SNAP[snapserver Container\n(Customized Snapcast)]
        MQTT[mosquitto Container\n(MQTT Broker)]
        SUB[navidrome Container\n(Optional - profile:media)]
        OTEL[signoz-otel-collector\n(Optional - profile:observability)]
        CLICKHOUSE[signoz-clickhouse\n(Optional - profile:observability)]
        QUERY[signoz-query-service\n(Optional - profile:observability)]
        FRONTEND[signoz-frontend\n(Optional - profile:observability)]

        APP --> SNAP;
        APP --> MQTT;
        APP --> SUB;
        APP --> OTEL[OTLP Endpoint];

        OTEL --> CLICKHOUSE;
        CLICKHOUSE --> QUERY;
        QUERY --> FRONTEND;

        subgraph "Mounted Volumes / Paths"
            direction LR
            FIFOS[/snapsinks]
            MUSIC[/music]
            CONFIGS[/configs]
            LOGS[/logs]
            DB_DATA[DB/Data Volumes]
        end

        APP -- writes audio --> FIFOS;
        SNAP -- reads audio --> FIFOS;
        SNAP -- streams --> CLIENTS[Snapcast Clients];
        SUB -- reads --> MUSIC;
        APP -- reads --> CONFIGS;
        APP -- writes --> LOGS;
        MQTT -- uses --> DB_DATA;
        SUB -- uses --> DB_DATA;
        CLICKHOUSE -- uses --> DB_DATA;


    end

    subgraph "External"
        KNX[KNX Gateway]
        CLIENTS[Snapcast Clients]
        USER[User via API/MQTT]
        ADMIN[Admin via SigNoz UI]
    end

    USER --> APP;
    USER --> MQTT;
    APP --> KNX;
    ADMIN --> FRONTEND;


    classDef extern fill:#EFEFEF,stroke:#666
```

## 31.2. `snapserver` Container Customization

A standard Snapcast server image is usualy not sufficent. A customized image (built from `/docker/snapserver/Dockerfile`) is recommended to implement the required zone-to-sink architecture.

**Key Customizations:**

1. **Named Pipe (FIFO) Sinks:** The Snapcast server configuration (`snapserver.conf`) must define `pipe` sources, one for each configured SnapDog2 zone. The paths for these pipes must match the `SNAPDOG_ZONE_n_SINK` environment variables used by the `snapdog` application (e.g., `/snapsinks/zone1`, `/snapsinks/zone2`). These paths should typically be mounted as a volume shared between the `snapdog` and `snapserver` containers or exist within the `snapserver` container where the `snapdog` container can write to them (less common).

    ```ini
    # Example section in snapserver.conf (generated dynamically from env vars)
    [stream]
    source = pipe:///snapsinks/zone1?name=Zone1&sampleformat=48000:16:2
    # ... other stream settings ...

    [stream]
    source = pipe:///snapsinks/zone2?name=Zone2&sampleformat=48000:16:2
    # ... other stream settings ...
    ```

2. **Dynamic Configuration:** An entrypoint script within the `snapserver` container can dynamically generate the relevant `[stream]` sections in `snapserver.conf` based on environment variables passed during container startup (e.g., `ZONE_COUNT`, `ZONE_1_NAME`, `ZONE_1_SINK_PATH`, etc.), mirroring the SnapDog2 zone configuration.
3. **AirPlay Integration (Optional via Shairport Sync):** If AirPlay support per zone is desired, the `snapserver` container needs:
    * `shairport-sync` installed.
    * `avahi-daemon` and `dbus` for service discovery (mDNS/Bonjour).
    * Multiple instances of `shairport-sync` running, each configured with a unique name (e.g., "Living Room AirPlay") and outputting to a separate named pipe (e.g., `/shairport-sync/zone1-airplay`).
    * The Snapcast configuration needs *additional* `pipe` sources for these AirPlay sinks and potentially `meta` streams to combine the direct FIFO sink and the AirPlay sink for a single zone output group. **Note:** This blueprint assumes AirPlay is managed entirely within the Snapcast/Shairport setup and SnapDog2 does not directly interact with AirPlay streams.
4. **Process Management:** Use `supervisord` or a similar process manager within the `snapserver` container to manage `snapserver`, `avahi-daemon` (if needed), and multiple `shairport-sync` instances (if needed).

## 31.3. `snapdog` Container (`/docker/snapdog/Dockerfile`)

Builds the .NET application for production deployment.

* Uses multi-stage builds (SDK for build/publish, ASP.NET Runtime for final stage).
* Installs **native LibVLC dependencies** required by `LibVLCSharp` for the target runtime (e.g., `libvlc-dev`, `vlc-plugin-*` for Debian/Ubuntu bases; `vlc-dev`, `vlc` for Alpine if available/compatible). **Crucially, only the Linux native libraries are strictly required for the Alpine/Debian Docker target.**
* Copies published application artifacts to the final stage.
* Sets the `ENTRYPOINT ["dotnet", "SnapDog2.dll"]`.
* Exposes the application port (e.g., 8080).
* Optionally creates and runs as a non-root user (`appuser`).

```dockerfile
# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

# Copy project and solution files
COPY ["SnapDog2/SnapDog2.csproj", "src/SnapDog2/"]
# Add COPY commands for other projects if structure differs (.Core, .Server, .Infrastructure, etc.)
COPY ["Directory.Packages.props", "./"]
COPY ["NuGet.config", "./"]

# Restore dependencies
RUN dotnet restore "src/SnapDog2/SnapDog2.csproj"

# Copy remaining source code
COPY . .

# Build and publish application
WORKDIR "/src/SnapDog2"
RUN dotnet publish "SnapDog2.csproj" -c Release -o /app/publish --no-restore /p:UseAppHost=false

# Stage 2: Final Runtime Image
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS final
WORKDIR /app
EXPOSE 8080

# Install native LibVLC dependencies for Alpine
# Check specific package names for Alpine's current repositories
RUN apk add --no-cache vlc-dev vlc vlc-libs

# Create non-root user (optional but recommended)
RUN addgroup -S appgroup && adduser -S appuser -G appgroup
USER appuser

# Copy published artifacts from build stage
COPY --from=build /app/publish .

# Set entrypoint
ENTRYPOINT ["dotnet", "SnapDog2.dll"]
```

*(Note: Alpine package names for VLC might differ slightly or require enabling community repositories)*

## 31.4. Docker Compose Configuration (Production Example)

Uses a base `docker-compose.yml` and optional overrides (e.g., `docker-compose.prod.yml`). Leverages `.env` files for configuration and profiles for optional services.

```yaml
# docker-compose.yml (Base Definition)

networks:
  snapdog_net:
    driver: bridge
    name: snapdog_net # Explicit network name

volumes: # Define named volumes for persistent data
  mosquitto_data:
  mosquitto_log:
  navidrome_data:
  grafana_data: # If using Grafana
  # snapsinks: # Define if using named volume for sinks

services:
  # SnapDog2 Application Service
  snapdog:
    image: snapdog2/app:${SNAPDOG_VERSION:-latest} # Use image variable
    container_name: snapdog
    restart: unless-stopped
    networks:
      - snapdog_net
    ports:
      - "127.0.0.1:8080:8080" # Expose only to host by default
    volumes:
      # Mount logs for external access/rotation
      - ./logs:/app/logs # Example bind mount, adjust as needed
      # Mount configs if externalizing appsettings.json (alternative to env vars)
      # - ./config:/app/config:ro
    env_file:
      - .env # Load production environment variables
    depends_on:
      - mosquitto
      - snapserver
      # Add Navidrome if it's a hard dependency, otherwise handle failures via resilience
    healthcheck:
        test: ["CMD", "wget", "--quiet", "--tries=1", "--spider", "http://localhost:8080/health/live"]
        interval: 30s
        timeout: 10s
        retries: 3
        start_period: 60s

  # Snapcast Server Service
  snapserver:
    image: snapdog2/snapserver:${SNAPSERVER_VERSION:-latest} # Use custom image
    container_name: snapserver
    restart: unless-stopped
    networks:
      - snapdog_net
    ports: # Expose necessary Snapcast ports to host if needed, or keep internal
      - "127.0.0.1:1705:1705" # Control port
      - "127.0.0.1:1780:1780" # Web UI
      # - "1704:1704" # Stream port (usually not needed on host)
    volumes:
      # Mount host directory for FIFO pipes if using file output from SnapDog
      # Ensure SnapDog container also mounts this path or uses appropriate network write method
      - /mnt/host_snapsinks:/snapsinks # Example bind mount for FIFOs
    env_file:
      - .env # Pass ZONE_ configs to entrypoint script if using dynamic config

  # MQTT Broker Service
  mosquitto:
    image: eclipse-mosquitto:2.0 # Pin version
    container_name: mosquitto
    restart: unless-stopped
    networks:
      - snapdog_net
    ports:
      - "127.0.0.1:1883:1883" # Expose standard MQTT port only to host
    volumes:
      - ./config/mosquitto/mosquitto.conf:/mosquitto/config/mosquitto.conf:ro
      - ./config/mosquitto/passwd:/mosquitto/config/passwd:ro # Mount password file if using auth
      - mosquitto_data:/mosquitto/data
      - mosquitto_log:/mosquitto/log
    command: mosquitto -c /mosquitto/config/mosquitto.conf

  # --- Optional Services (Activated by Profiles) ---

  # Navidrome (Subsonic) Service
  navidrome:
    image: deluan/navidrome:latest # Or pin version
    container_name: navidrome
    restart: unless-stopped
    networks:
      - snapdog_net
    ports:
      - "127.0.0.1:4533:4533"
    volumes:
      - navidrome_data:/data
      - /mnt/host_music:/music:ro # Mount music library read-only
    env_file:
      - .env # Load ND_* variables
    profiles: ["media"]

  # SigNoz ClickHouse Database
  signoz-clickhouse:
    image: clickhouse/clickhouse-server:23.7
    container_name: signoz-clickhouse
    restart: unless-stopped
    networks:
      - snapdog_net
    environment:
      - CLICKHOUSE_DB=signoz
      - CLICKHOUSE_USER=signoz
      - CLICKHOUSE_PASSWORD=signoz
    volumes:
      - signoz_clickhouse_data:/var/lib/clickhouse
    profiles: ["observability"]

  # SigNoz OpenTelemetry Collector
  signoz-otel-collector:
    image: signoz/signoz-otel-collector:0.88.11
    container_name: signoz-otel-collector
    restart: unless-stopped
    networks:
      - snapdog_net
    ports:
      - "127.0.0.1:4317:4317" # OTLP gRPC receiver
      - "127.0.0.1:4318:4318" # OTLP HTTP receiver
    volumes:
      - ./config/signoz/otel-collector-config.yaml:/etc/otel-collector-config.yaml:ro
    command: --config=/etc/otel-collector-config.yaml
    depends_on:
      - signoz-clickhouse
    profiles: ["observability"]

  # SigNoz Query Service
  signoz-query-service:
    image: signoz/query-service:0.39.0
    container_name: signoz-query-service
    restart: unless-stopped
    networks:
      - snapdog_net
    environment:
      - ClickHouseUrl=tcp://signoz-clickhouse:9000/?database=signoz
      - STORAGE=clickhouse
      - GODEBUG=netdns=go
      - TELEMETRY_ENABLED=true
      - DEPLOYMENT_TYPE=docker-standalone
    depends_on:
      - signoz-clickhouse
      - signoz-otel-collector
    profiles: ["observability"]

  # SigNoz Frontend
  signoz-frontend:
    image: signoz/frontend:0.39.0
    container_name: signoz-frontend
    restart: unless-stopped
    networks:
      - snapdog_net
    ports:
      - "127.0.0.1:3301:3301" # SigNoz UI
    environment:
      - FRONTEND_API_ENDPOINT=http://signoz-query-service:8080
    depends_on:
      - signoz-query-service
    profiles: ["observability"]

# Define named volumes for persistent data
volumes:
  mosquitto_data:
  mosquitto_log:
  navidrome_data:
  signoz_clickhouse_data: # SigNoz database storage
```

## 31.5. Docker Compose Profiles

Use profiles to manage optional service groups:

* **`default`** (implied): `snapdog`, `snapserver`, `mosquitto`.
* **`media`**: Adds `navidrome`.
* **`observability`**: Adds `signoz-clickhouse`, `signoz-otel-collector`, `signoz-query-service`, `signoz-frontend`.

Run commands:

* Core only: `docker compose up -d`
* Core + Media: `docker compose --profile media up -d`
* Core + Media + Observability: `docker compose --profile media --profile observability up -d`

## 31.6. Environment Variables for Production

Key environment variables for production deployment:

```bash
# Core Application
SNAPDOG_TELEMETRY_ENABLED=true
SNAPDOG_TELEMETRY_SERVICE_NAME=SnapDog2
SNAPDOG_TELEMETRY_OTLP_ENDPOINT=http://signoz-otel-collector:4317
SNAPDOG_TELEMETRY_OTLP_PROTOCOL=grpc

# Services
SNAPDOG_SERVICES_SNAPCAST_ADDRESS=snapserver
SNAPDOG_SERVICES_MQTT_BROKER_ADDRESS=mosquitto
SNAPDOG_SERVICES_SUBSONIC_URL=http://navidrome:4533

# Zones and Clients (example)
SNAPDOG_ZONE_1_NAME=Living Room
SNAPDOG_ZONE_1_SINK=/snapsinks/zone1
SNAPDOG_CLIENT_1_NAME=Living Room Speaker
SNAPDOG_CLIENT_1_DEFAULT_ZONE=1
```
