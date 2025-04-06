# SnapDog2 Docker Infrastructure

This directory contains Docker configuration files for running SnapDog2 services.

## Core Containers

The SnapDog2 system consists of three core containers:

1. **SnapDog2**: The main application container running the .NET application
2. **Snapcast Server**: The multi-room audio synchronization server with custom configuration
3. **Mosquitto**: The MQTT broker for message communication

## Optional Metrics Containers

The system also includes optional metrics and telemetry containers that can be enabled as needed:

1. **Jaeger**: Distributed tracing system
2. **Prometheus**: Metrics collection and storage
3. **Grafana**: Metrics visualization and dashboarding

## Running the System

### Standard Deployment

To run the standard deployment without metrics:

```bash
docker-compose up -d
```

### With Metrics Enabled

To run the deployment with metrics containers:

```bash
docker-compose --profile metrics up -d
```

Or alternatively:

```bash
ENABLE_METRICS=true docker-compose --profile metrics up -d
```

## Development Environment

For development, use the dev environment configuration:

```bash
docker-compose -f docker-compose.dev.yml up -d
```

To enable metrics in development:

```bash
docker-compose -f docker-compose.dev.yml --profile metrics up -d
```

## Container Configuration

### Snapcast Server

The Snapcast server is built with a custom Dockerfile that includes:

- Multiple audio streams (one per zone)
- FIFO pipes for each zone that SnapDog writes to
- AirPlay integration with each zone getting an AirPlay receiver
- Dynamic configuration via environment variables

### Metrics Container Ports

- Jaeger UI: Port 16686
- Prometheus: Port 9090
- Grafana: Port 3000

## VS Code Integration

The project includes a devcontainer.json file for VS Code development. To enable metrics in VS Code:

1. Open the `.devcontainer/devcontainer.json` file
2. Uncomment the `ENABLE_METRICS` environment variable
3. Uncomment the additional port forwarding entries
4. Reopen the project in the container

## Environment Variables

- `ENABLE_METRICS`: Set to "true" to enable metrics containers
- `MOSQUITTO_HOST` and `MOSQUITTO_PORT`: Configure MQTT broker connection
- `SNAPCAST_SERVER_HOST` and `SNAPCAST_SERVER_PORT`: Configure Snapcast server connection