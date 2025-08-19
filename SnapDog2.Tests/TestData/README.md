# 📁 Test Data Directory - Self-Contained Test Infrastructure

This directory contains a **fully self-contained test infrastructure** for SnapDog2 integration tests. No external dependencies on development containers or configurations.

## 🏗️ Directory Structure

```
TestData/
├── Docker/                                    # Self-contained Docker test environment
│   ├── docker-compose.test.yml               # Main test environment orchestration
│   ├── containers/                           # Dedicated test container definitions
│   │   ├── app/
│   │   │   └── Dockerfile.test               # Test-optimized SnapDog2 container
│   │   ├── mqtt/
│   │   │   ├── Dockerfile                    # Minimal MQTT broker
│   │   │   └── mosquitto.test.conf           # MQTT test configuration
│   │   ├── snapcast-server/
│   │   │   ├── Dockerfile                    # Test Snapcast server
│   │   │   └── snapserver.test.conf          # Snapcast test configuration
│   │   ├── snapcast-client/
│   │   │   └── Dockerfile                    # Test Snapcast clients
│   │   └── knxd/
│   │       ├── Dockerfile                    # Test KNX daemon
│   │       └── knxd.test.conf                # KNX test configuration
│   └── configs/                              # Shared test configurations
│       ├── mosquitto.test.conf               # MQTT broker config
│       ├── snapserver.test.conf              # Snapcast server config
│       └── knxd.test.conf                    # KNX daemon config
└── README.md                                 # This file
```

## 🎯 Self-Contained Design Principles

### ✅ **Fully Explicit Dependencies**
- All container definitions are in the test project
- No references to external devcontainer files
- All configurations are test-specific and optimized

### ✅ **Test-Optimized Containers**
- **Minimal size** - Only essential components installed
- **Fast startup** - Optimized for quick test execution
- **Health checks** - Reliable service readiness detection
- **Test-specific configs** - Simplified settings for testing

### ✅ **Isolated Test Environment**
- **Separate network** - `172.24.0.0/16` (different from dev `172.25.0.0/16`)
- **Test-specific MACs** - `02:42:ac:14:00:xx` pattern
- **No persistence** - Clean state for each test run
- **Dedicated ports** - Avoid conflicts with development environment

## 🐳 Container Details

### **Application Container** (`app/Dockerfile.test`)
- **Base**: `mcr.microsoft.com/dotnet/aspnet:9.0`
- **Purpose**: Test-optimized SnapDog2 application
- **Features**: Health checks, non-root user, minimal dependencies
- **Environment**: Test-specific configuration

### **MQTT Broker** (`mqtt/Dockerfile`)
- **Base**: `eclipse-mosquitto:2.0-openssl`
- **Purpose**: Lightweight message broker for testing
- **Features**: Simple auth (snapdog/snapdog), no persistence
- **Config**: Optimized for test reliability

### **Snapcast Server** (`snapcast-server/Dockerfile`)
- **Base**: `debian:bookworm-slim`
- **Purpose**: Multi-room audio server for testing
- **Features**: Named pipes for audio streams, web interface
- **Config**: Two test zones with health monitoring

### **Snapcast Clients** (`snapcast-client/Dockerfile`)
- **Base**: `debian:bookworm-slim`
- **Purpose**: Simulated audio playback devices
- **Features**: Null audio player (no actual audio), fixed MAC addresses
- **Instances**: Living Room, Kitchen, Bedroom

### **KNX Daemon** (`knxd/Dockerfile`)
- **Base**: `debian:bookworm-slim`
- **Purpose**: Building automation protocol testing
- **Features**: Dummy backend (no real hardware), tunnel interface
- **Config**: Test-friendly KNX setup

## 🔧 MSBuild Integration

All files are automatically copied to the test output directory:

```xml
<ItemGroup>
  <None Update="TestData/**/*">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

## 🚀 Usage

### **Running Tests**
The `DockerComposeTestFixture` automatically:
1. Discovers the test environment in `TestData/Docker/`
2. Starts all services with health checks
3. Waits for services to be ready
4. Runs integration tests
5. Cleans up containers after tests

### **Manual Testing**
```bash
# Start test environment manually
cd SnapDog2.Tests/TestData/Docker
docker compose -f docker-compose.test.yml up -d

# Access services
curl http://localhost:5001/health        # SnapDog2 app
mosquitto_pub -h localhost -p 1883 -u snapdog -P snapdog -t test -m hello
curl http://localhost:1780              # Snapcast web interface

# Stop test environment
docker compose -f docker-compose.test.yml down -v
```

## 🎯 Benefits Achieved

### ✅ **Explicit Dependencies**
- All test infrastructure is visible and contained
- No hidden dependencies on development files
- Easy to understand what tests require

### ✅ **Stable & Reliable**
- Changes to development environment don't break tests
- Test-specific optimizations improve reliability
- Health checks ensure services are ready before testing

### ✅ **Fast & Efficient**
- Minimal containers start faster
- No unnecessary services or features
- Optimized for CI/CD environments

### ✅ **Maintainable**
- Clear separation between dev and test infrastructure
- Self-documenting through explicit container definitions
- Easy to modify test-specific configurations

## 📝 Adding New Test Services

1. **Create container directory**: `containers/new-service/`
2. **Add Dockerfile**: Optimized for testing
3. **Add configuration**: Test-specific settings
4. **Update docker-compose.test.yml**: Add service definition
5. **Update this README**: Document the new service

## 🔍 Troubleshooting

### **Container Build Issues**
```bash
# Build individual container
docker build -t test-app containers/app -f containers/app/Dockerfile.test

# Check logs
docker compose -f docker-compose.test.yml logs app
```

### **Service Health Issues**
```bash
# Check service health
docker compose -f docker-compose.test.yml ps

# Inspect specific service
docker compose -f docker-compose.test.yml exec app curl http://localhost:5000/health
```

The test infrastructure is now **fully self-contained and explicit** - no more hidden dependencies! 🎯
