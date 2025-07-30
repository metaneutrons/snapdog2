# SnapDog2

![SnapDog2 Logo](assets/icons/png/snapdog-128.png)

## Overview

SnapDog2 is an open-source .NET-based smart home automation API and controller platform. It provides endpoints for KNX, MQTT, and Snapcast services, designed with a modular, CQRS-driven architecture. This project is a work in progress; features may be broken or incomplete.

> [!WARNING]
> This is absolutely non-functional work in progress; it's not usable at the moment. Please give me some weeks!

## Development Environment

**Container-first development** with single-port access to all services via beautiful dashboard.

### Quick Start

```bash
# One-time setup
make dev-setup

# Start full development environment
make dev
```

**Access everything at:** <http://localhost:8000> 🎉

## Development Commands

### 🏗️ Development

- `make dev-setup` - Initial setup (pull images, restore packages)
- `make dev` - Full development environment (setup + start + urls)
- `make dev-stop` - Stop all services
- `make dev-status` - Show status of all services
- `make dev-logs` - Show logs from all services

### 📊 Monitoring

- `make monitoring-start` - Start Prometheus + Grafana
- `make monitoring-stop` - Stop monitoring services

### 🧪 Testing & Building

- `make test` - Run tests with services
- `make build` - Build the application
- `make clean` - Clean containers and volumes

### 🌐 Utilities

- `make urls` - Show all service URLs
- `make restart` - Quick restart

## Architecture

```plaintext
┌─────────────────────────────────────────────────────────────────┐
│                 Docker Network: 172.20.0.0/16                   │
│                                                                 │
│  ┌─────────────┐  ┌──────────────────────────────────────────┐  │
│  │ Caddy       │◄─┤         Single Port Access               │  │
│  │ 172.20.0.4  │  │         http://localhost:8000            │  │
│  │ :8000       │  └──────────────────────────────────────────┘  │
│  └─────────────┘                                                │
│         │                                                       │
│         ▼                                                       │
│  ┌─────────────┐  ┌──────────────┐  ┌─────────────────────────┐ │
│  │ SnapDog2    │  │ Snapcast     │  │    Snapcast Clients     │ │
│  │ 172.20.0.2  │◄─┤ Server       │◄─┤ Living│Kitchen│Bedroom  │ │
│  │ :5000       │  │ 172.20.0.5   │  │ .0.6  │ .0.7  │ .0.8    │ │
│  └─────────────┘  │ :1704/:1780  │  │ :1780 │ :1780 │ :1780   │ │
│         │         └──────────────┘  └─────────────────────────┘ │
│         ▼                                                       │
│  ┌─────────────┐  ┌──────────────┐  ┌─────────────────────────┐ │
│  │ Navidrome   │  │ MQTT Broker  │  │ KNX Gateway             │ │
│  │ 172.20.0.9  │  │ 172.20.0.3   │  │ 172.20.0.10             │ │
│  │ :4533       │  │ :1883        │  │ :6720                   │ │
│  └─────────────┘  └──────────────┘  └─────────────────────────┘ │
│                                                                 │
│  ┌─────────────┐  ┌──────────────┐  ┌─────────────────────────┐ │
│  │ Jaeger      │  │ Prometheus   │  │ Grafana                 │ │
│  │ 172.20.0.11 │  │ 172.20.0.12  │  │ 172.20.0.13             │ │
│  │ :16686      │  │ :9090        │  │ :3000                   │ │
│  └─────────────┘  └──────────────┘  └─────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
```

## Service Access

All services accessible through **single port 8000** via Caddy reverse proxy:

- **🏠 Main Dashboard**: <http://localhost:8000>
- **🎵 Snapcast Server**: <http://localhost:8000/server/>
- **💿 Navidrome Music**: <http://localhost:8000/music/>
- **🛋️ Living Room Client**: <http://localhost:8000/clients/living-room/>
- **🍽️ Kitchen Client**: <http://localhost:8000/clients/kitchen/>
- **🛏️ Bedroom Client**: <http://localhost:8000/clients/bedroom/>
- **🔍 Jaeger Tracing**: <http://localhost:8000/tracing/>
- **📊 Grafana Dashboards**: <http://localhost:8000/grafana/> (admin/snapdog-dev)
- **📈 Prometheus Metrics**: <http://localhost:8000/prometheus/>

## What's Included

### 🎵 Core Audio Services

- **Snapcast Server** - Multi-room audio streaming server
- **3x Snapcast Clients** - Living room, kitchen, bedroom (with fixed MACs)
- **Navidrome** - Subsonic-compatible music server
- **MQTT Broker** - IoT messaging for smart home integration
- **KNX Gateway** - Building automation protocol simulator

### 📊 Observability

- **Jaeger** - Distributed tracing
- **Prometheus** - Metrics collection
- **Grafana** - Metrics visualization

### 🌐 Infrastructure

- **Caddy** - Reverse proxy with beautiful dashboard
- **Internal networking** - All services communicate via container network

## Development Features

- ✅ **Hot Reload** - Edit code locally, changes reload automatically in container
- ✅ **Internal Networking** - All services communicate via container network
- ✅ **Debugging Support** - Attach debugger to containerized application
- ✅ **Single Port Access** - Everything through beautiful dashboard at :8000
- ✅ **Volume Caching** - NuGet packages and source code cached for performance
- ✅ **Minimal Port Forwarding** - Only port 8000 exposed to host
- ✅ **Real Snapcast Clients** - Fixed MAC addresses for realistic testing

## VS Code Integration

### Container Development

1. **Install Extensions:**
   - Dev Containers
   - C# Dev Kit
   - Docker

2. **Debug Container App:**

   ```bash
   make dev
   # Then: Ctrl/Cmd+Shift+P -> "Dev Containers: Attach to Running Container"
   # Select: snapdog-snapdog-1
   # Set breakpoints and debug normally
   ```

## Daily Development Workflow

```bash
# Start everything
make dev

# View what's running
make dev-status
make urls

# Monitor logs
make dev-logs

# When done
make dev-stop
```

### Testing Multi-Room Audio

```bash
# Start services and app
make dev

# Test with real Snapcast clients
curl http://snapcast-server:1704 -d '{"method":"Server.GetStatus","id":1}'

# Clients available at:
# Living Room: 172.20.0.6 (MAC: 02:42:ac:11:00:10)
# Kitchen:     172.20.0.7 (MAC: 02:42:ac:11:00:11)
# Bedroom:     172.20.0.8 (MAC: 02:42:ac:11:00:12)
```

## Prerequisites

- Docker and Docker Compose
- Make (for convenience commands)
- Git

## Troubleshooting

### Container Issues

```bash
# Check container logs
make dev-logs

# Rebuild containers
make clean
make dev-setup
make dev
```

## Why This Approach Works

✅ **Professional Setup** - Single port with beautiful dashboard
✅ **Realistic Testing** - Real Snapcast clients with MAC addresses
✅ **Internal Networking** - No localhost configuration needed
✅ **Hot Reload** - Edit code locally, auto-restart in container
✅ **Full Debugging** - VS Code debugger works perfectly
✅ **Isolated** - No system dependencies or port conflicts
✅ **Production-Like** - Same networking as production environment

## Documentation

- **Blueprints & Design**: [docs/blueprint](docs/blueprint/)
- **Implementation Details**: [docs/implementation](docs/implementation/)

## License

This project is licensed under the GNU LGPL v3.0. See [LICENSE](LICENSE) for details.
