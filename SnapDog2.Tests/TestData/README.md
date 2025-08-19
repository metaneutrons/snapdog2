# 📁 Test Data Directory

This directory contains all test-related data files and resources used by the SnapDog2 test suite.

## 🏗️ Directory Structure

```
TestData/
├── Docker/                         # Docker-related test resources
│   ├── docker-compose.test.yml     # Main integration test environment
│   ├── docker-compose.ci.yml       # CI-specific overrides (future)
│   └── configs/                    # Service configuration files
├── Fixtures/                       # Test data fixtures (future)
├── Samples/                        # Sample data files (future)
└── README.md                       # This file
```

## 🐳 Docker Directory

### `docker-compose.test.yml`
The main Docker Compose file used by integration tests. This file defines:
- **SnapDog2 Application**: Test instance with test environment variables
- **MQTT Broker**: For messaging integration tests
- **Snapcast Server**: For multi-room audio testing
- **Snapcast Clients**: Simulated audio clients (Living Room, Kitchen, Bedroom)
- **KNX Gateway**: For building automation protocol testing

### Configuration Files
Service-specific configuration files used by the test environment containers.

## 🔧 MSBuild Integration

All files in this directory are automatically copied to the test output directory via the configuration in `SnapDog2.Tests.csproj`:

```xml
<ItemGroup>
  <None Update="TestData/**/*">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

## 🎯 Best Practices

### ✅ DO:
- Place all test data files in appropriate subdirectories
- Use descriptive names for configuration files
- Document any special requirements or dependencies
- Keep test data separate from test code

### ❌ DON'T:
- Put sensitive data (passwords, keys) in test files
- Create large binary files that bloat the repository
- Mix test data with production configuration

## 🚀 Usage

Test fixtures automatically discover and use files from this directory:
- `DockerComposeTestFixture` uses `Docker/docker-compose.test.yml`
- Other fixtures can reference files via relative paths from the test output directory

## 📝 Adding New Test Data

1. Create appropriate subdirectory if needed
2. Add files with descriptive names
3. Update this README if adding new categories
4. Ensure files are included in MSBuild via the existing wildcard pattern
