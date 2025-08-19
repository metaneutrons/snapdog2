# 📁 Test Data Directory - Self-Contained Test Infrastructure

This directory contains a **fully self-contained test infrastructure** for SnapDog2 integration tests, **based on proven devcontainer implementations** but optimized for testing.

## 🎯 **IMPORTANT: Based on Devcontainer Templates**

All test containers are **adapted from the proven devcontainer implementations** to ensure:
- ✅ **Same proven approach** - Uses the working devcontainer logic
- ✅ **Test optimizations** - Enhanced for testing reliability and speed
- ✅ **No reinvention** - Leverages existing, tested container definitions

## 🏗️ Directory Structure

```
TestData/
├── Docker/                                    # Self-contained Docker test environment
│   ├── docker-compose.test.yml               # Main test environment orchestration
│   ├── containers/                           # Test containers based on devcontainer templates
│   │   ├── app/
│   │   │   └── Dockerfile.test               # Test-optimized SnapDog2 container
│   │   ├── mqtt/
│   │   │   ├── Dockerfile                    # Based on devcontainer MQTT setup
│   │   │   └── mosquitto.test.conf           # Adapted from devcontainer/mosquitto/mosquitto.conf
│   │   ├── snapcast-server/
│   │   │   ├── Dockerfile                    # Based on devcontainer/snapcast-server/Dockerfile
│   │   │   └── snapserver.test.conf          # Test-optimized Snapcast configuration
│   │   ├── snapcast-client/
│   │   │   ├── Dockerfile                    # Based on devcontainer/snapcast-client/Dockerfile
│   │   │   ├── start.test.sh                 # Adapted from devcontainer/snapcast-client/start.sh
│   │   │   └── supervisord.test.conf         # Adapted from devcontainer/snapcast-client/supervisord.conf
│   │   └── knxd/
│   │       ├── Dockerfile                    # Based on devcontainer/knxd/Dockerfile
│   │       └── knxd.test.conf                # Test-specific KNX configuration
│   └── configs/                              # Shared test configurations
└── README.md                                 # This file
```

## 🔄 **Devcontainer Template Adaptations**

### **MQTT Broker** (`mqtt/`)
- **Template**: `devcontainer/mosquitto/mosquitto.conf`
- **Adaptations**: 
  - Disabled persistence for clean test state
  - Enhanced logging for test debugging
  - Simplified authentication (snapdog/snapdog)
  - Optimized timeouts for test reliability

### **Snapcast Server** (`snapcast-server/`)
- **Template**: `devcontainer/snapcast-server/Dockerfile`
- **Adaptations**:
  - Same Alpine + compiled approach
  - Same build dependencies and process
  - Test-specific named pipes for audio streams
  - Enhanced health checks for test reliability

### **Snapcast Clients** (`snapcast-client/`)
- **Template**: `devcontainer/snapcast-client/Dockerfile` + `start.sh` + `supervisord.conf`
- **Adaptations**:
  - Same Alpine + supervisor approach
  - Same MAC address detection logic
  - Test-specific MAC addresses (02:42:ac:14:00:xx)
  - Same null audio configuration
  - Enhanced health checks

### **KNX Daemon** (`knxd/`)
- **Template**: `devcontainer/knxd/Dockerfile`
- **Adaptations**:
  - Same Alpine edge + knxd approach
  - Same user and directory setup
  - Test-specific dummy backend configuration
  - Enhanced health checks for test reliability

## 🎯 Self-Contained Design Principles

### ✅ **Proven Foundation**
- All containers based on working devcontainer implementations
- Same build processes and dependencies
- Same runtime configurations adapted for testing

### ✅ **Test-Optimized Enhancements**
- **Health checks** - Added to all services for reliability
- **Clean state** - No persistence between test runs
- **Fast startup** - Optimized configurations for speed
- **Enhanced logging** - Better debugging for test failures

### ✅ **Isolated Test Environment**
- **Separate network** - `172.24.0.0/16` (different from dev `172.25.0.0/16`)
- **Test-specific MACs** - `02:42:ac:14:00:xx` pattern (different from dev)
- **No conflicts** - Completely isolated from development environment

## 🐳 Container Details

### **Application Container** (`app/Dockerfile.test`)
- **Purpose**: Test-optimized SnapDog2 application
- **Base**: `mcr.microsoft.com/dotnet/aspnet:9.0`
- **Features**: Health checks, test environment variables

### **MQTT Broker** (`mqtt/Dockerfile`)
- **Template**: Devcontainer mosquitto setup
- **Base**: `eclipse-mosquitto:2.0-openssl`
- **Adaptations**: Test-optimized configuration, no persistence

### **Snapcast Server** (`snapcast-server/Dockerfile`)
- **Template**: Devcontainer snapcast-server (Alpine + compiled)
- **Base**: `alpine:3.21` with compiled Snapcast v0.31.0
- **Adaptations**: Test audio streams, enhanced health checks

### **Snapcast Clients** (`snapcast-client/Dockerfile`)
- **Template**: Devcontainer snapcast-client (Alpine + supervisor)
- **Base**: `alpine:latest` with supervisor
- **Adaptations**: Test MAC addresses, enhanced health checks

### **KNX Daemon** (`knxd/Dockerfile`)
- **Template**: Devcontainer knxd (Alpine edge)
- **Base**: `alpine:edge` with knxd from testing repo
- **Adaptations**: Dummy backend for testing, enhanced health checks

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

### ✅ **Proven Foundation**
- **No reinvention** - Uses working devcontainer implementations
- **Battle-tested** - Same containers used in development
- **Consistent behavior** - Same runtime characteristics as dev environment

### ✅ **Test Optimizations**
- **Enhanced reliability** - Health checks on all services
- **Clean state** - No persistence between test runs
- **Fast execution** - Optimized for CI/CD environments
- **Better debugging** - Enhanced logging for test failures

### ✅ **Self-Contained & Explicit**
- **No hidden dependencies** - All infrastructure visible in test project
- **Development isolation** - Changes to dev environment don't break tests
- **Clear understanding** - Easy to see what tests require

## 📝 Template Maintenance

When updating devcontainer implementations:
1. **Review changes** in devcontainer files
2. **Adapt test versions** to incorporate improvements
3. **Test thoroughly** to ensure compatibility
4. **Update documentation** to reflect changes

This approach ensures test containers stay current with proven devcontainer implementations while maintaining test-specific optimizations.

The test infrastructure now leverages **proven devcontainer templates** with test-specific enhancements! 🎯
