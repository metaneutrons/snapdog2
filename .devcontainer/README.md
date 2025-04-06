# SnapDog Devcontainer Setup

This directory contains customized configurations for running SnapDog in a development container with isolated Snapcast server and clients.

## Design Decisions

The devcontainer setup is designed to address several issues:

1. **Network Isolation** - All services run in a Docker bridge network with fixed IP addresses, eliminating host network mode and its associated issues.
2. **No mDNS/Avahi/Airplay** - The development setup intentionally disables these features that can cause problems in containerized environments.
3. **Consistent Client Identification** - Each Snapcast client has a fixed MAC address and meaningful client ID to ensure reliable identification in SnapDog.
4. **Web Interface Access** - Caddy serves as a reverse proxy with a landing page, providing access to all web interfaces.

## Architecture

- **Snapcast Server** (172.20.0.4)
  - Simplified without Airplay/mDNS dependencies
  - Creates stream sources using VLC with pipe:// streams
  - Accessible via WebSocket on port 1704 and HTTP on port 1780

- **Snapcast Clients** (172.20.0.5-7)
  - Each has a fixed MAC address and meaningful client ID
  - Connect directly to the server via fixed IP address
  - Use null audio output (no actual sound in dev container)

- **Caddy Reverse Proxy** (172.20.0.8)
  - Exposes a landing page accessible at http://localhost:8000
  - Routes to server UI at /server
  - Routes to client UIs at /clients/1, /clients/2, /clients/3

## Usage

1. Open the project in VS Code
2. When prompted, select "Reopen in Container"
3. The environment will start with the Snapcast server and three clients
4. Access the web interfaces at http://localhost:8000

## Development vs. Production

This setup is intentionally different from the production setup in `/docker` to provide:
- Better isolation for development
- Elimination of problematic services (mDNS/Airplay) 
- Fixed networking for reliable development
- Simplified containerization without host network dependencies

## Troubleshooting

- If you encounter connectivity issues, check the logs with `docker logs snapcast-server` or `docker logs snapcast-client-X`
- To restart a service, use `docker restart snapcast-server` or the appropriate container name
- For more persistent issues, rebuilding the devcontainer with "Rebuild Container" in VS Code usually resolves them