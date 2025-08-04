# KNX Simulator Container using knxd

Custom Alpine Edge-based KNX daemon container for SnapDog2 development.

## Features

- **Security**: Runs as non-root user (knxd:1000)
- **Alpine-based**: Minimal footprint with Alpine Edge
- **Proper init**: Uses tini as PID 1 for proper signal handling
- **Health checks**: Built-in health monitoring
- **Configurable**: Environment-based configuration
- **Working**: Successfully runs knxd with dummy interface

## Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `ADDRESS` | `1.1.128` | KNX daemon address |
| `CLIENT_ADDRESS` | `1.1.129:8` | Client address range |
| `INTERFACE` | `dummy` | KNX interface type |
| `DEBUG_LEVEL` | `info` | Debug/log level |
| `FILTERS` | `single,pace:queue` | KNX filters |

## Ports

- **3671/udp**: KNX/IP multicast port
- **6720/tcp**: KNX daemon TCP interface

## Configuration

The container creates a minimal working knxd configuration:

```ini
[main]
addr = 1.1.128
client-addrs = 1.1.129:8
connections = A.tcp,dummy

[A.tcp]
server = knxd_tcp
port = 6720

[dummy]
driver = dummy
```

## Usage

The container is automatically built and used by docker-compose.dev.yml.

### Manual Testing

```bash
# Build the container
docker build -t snapdog-knxd ./devcontainer/knxd

# Run standalone
docker run -p 3671:3671/udp -p 6720:6720/tcp snapdog-knxd

# Test TCP connection
nc -z localhost 6720
```

### Integration Testing

```bash
# Start development environment
make dev

# Check KNX simulator status
docker logs snapdog-knxd-1

# Test connectivity from within the network
docker exec snapdog-app-1 nc -z knxd 6720
```

## Security

- Runs as non-root user (UID/GID 1000)
- Drops all capabilities except NET_BIND_SERVICE
- Uses tini for proper process management
- Configuration files stored in user home directory
- No root warnings or security issues

## Troubleshooting

### Container Status

```bash
# Check if container is healthy
docker ps | grep knxd

# View logs
docker logs snapdog-knxd-1

# Check port binding
netstat -ln | grep 6720
```

### Common Issues

1. **Port conflicts**: Ensure ports 3671 and 6720 are not in use
2. **Permission issues**: Container runs as non-root, no permission problems
3. **Configuration errors**: Check logs for knxd configuration issues
