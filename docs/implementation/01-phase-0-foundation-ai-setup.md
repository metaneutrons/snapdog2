# Phase 0: Foundation & AI Setup

## Overview

Phase 0 establishes the development foundation, AI collaboration framework, and test-driven development infrastructure. This phase ensures proper project setup, configuration management, and AI-enhanced development workflow.

**Deliverable**: Basic console application that loads and validates configuration using EnvoyConfig.

## Objectives

### Primary Goals

- [ ] Establish project structure following blueprint architecture
- [ ] Implement EnvoyConfig-based configuration system
- [ ] Setup comprehensive testing infrastructure
- [ ] Create AI collaboration templates and context management
- [ ] Validate development environment and tooling
- [ ] Implement basic logging and error handling

### Success Criteria

- Console application successfully loads configuration from environment variables
- All tests pass with 90%+ code coverage
- AI collaboration templates are functional and validated
- Project structure follows blueprint specifications
- Development environment is fully operational

## Prerequisites

### Development Environment

- .NET 9 SDK installed
- IDE configured (Visual Studio, VS Code, or Rider)
- Docker Desktop for container development
- Git for version control

### Knowledge Requirements

- Understanding of SnapDog blueprint documents (especially 02, 03, 10, 15)
- Familiarity with EnvoyConfig library
- Basic understanding of TDD principles
- AI collaboration tool access (Claude, Copilot, etc.)

## Implementation Steps

### Step 1: Project Structure Setup

#### 1.1 Create Solution and Projects

```bash
# Create solution
dotnet new sln -n SnapDog

# Create main project following blueprint structure
dotnet new console -n SnapDog
dotnet sln add SnapDog/SnapDog.csproj

# Create test project
dotnet new xunit -n SnapDog.Tests
dotnet sln add SnapDog.Tests/SnapDog.Tests.csproj
```

#### 1.2 Establish Folder Structure

```
SnapDog/
├── Worker/           # Entry point and composition root
│   └── Program.cs
├── Api/             # API layer (placeholder for Phase 4)
├── Server/          # Business logic layer (placeholder for Phase 3)
│   └── Features/
├── Infrastructure/  # External services (placeholder for Phase 2)
└── Core/           # Domain and abstractions (placeholder for Phase 1)
    ├── Models/
    ├── Abstractions/
    └── Configuration/
```

#### 1.3 AI Collaboration: Project Structure Creation

**AI Prompt Template**:

```
Context: I'm implementing SnapDog, a multi-room audio streaming system following a layered architecture.

Blueprint Reference:
- Worker layer: Entry point and composition root
- API layer: Controllers and HTTP handling
- Server layer: Business logic with MediatR
- Infrastructure layer: External service implementations
- Core layer: Domain models and abstractions

Task: Help me create the folder structure for a .NET console application that follows this architecture. Include appropriate placeholder files and initial structure.

Requirements:
- Follow .NET naming conventions
- Include appropriate gitkeep files for empty directories
- Add basic namespace structure
- Ensure clean layer dependencies (Core has no dependencies, Infrastructure depends on Core, etc.)
```

### Step 2: Configuration System Implementation

#### 2.1 Add Dependencies

```xml
<!-- SnapDog.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="EnvoyConfig" Version="Latest" />
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="9.0.0" />
    <PackageReference Include="Microsoft.Extensions.Configuration" Version="9.0.0" />
    <PackageReference Include="Microsoft.Extensions.Configuration.EnvironmentVariables" Version="9.0.0" />
    <PackageReference Include="Serilog" Version="4.0.0" />
    <PackageReference Include="Serilog.Extensions.Hosting" Version="8.0.0" />
    <PackageReference Include="Serilog.Sinks.Console" Version="6.0.0" />
  </ItemGroup>
</Project>
```

#### 2.2 Create Configuration Models

Create `Core/Configuration/SnapDogConfiguration.cs`:

#### 2.3 AI Collaboration: Configuration Implementation

**AI Prompt Template**:

```
Context: Implementing configuration system for SnapDog multi-room audio streaming application.

Blueprint References:
- Document 10: Configuration System - Environment variable based configuration
- EnvoyConfig integration for structured configuration loading
- Support for nested configurations (clients, radio stations, zones)

Existing EnvoyConfig Sample:
[Include relevant parts of EnvoyConfig.Sample/SnapdogConfig.cs]

Task: Create a comprehensive configuration model that includes:
1. Core SnapDog settings (logging, telemetry, API configuration)
2. Snapcast server connection settings
3. Client configurations with MQTT and KNX mappings
4. Radio station configurations
5. Environment variable validation

Requirements:
- Use EnvoyConfig attributes for environment variable mapping
- Include validation attributes where appropriate
- Follow blueprint configuration patterns
- Ensure type safety with strongly-typed configuration classes
```

### Step 3: Testing Infrastructure Setup

#### 3.1 Test Project Dependencies

```xml
<!-- SnapDog.Tests.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.0" />
    <PackageReference Include="xunit" Version="2.9.0" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
    <PackageReference Include="coverlet.collector" Version="6.0.2" />
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
    <PackageReference Include="Moq" Version="4.20.70" />
    <PackageReference Include="Microsoft.Extensions.Configuration.EnvironmentVariables" Version="9.0.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\SnapDog\SnapDog.csproj" />
  </ItemGroup>
</Project>
```

#### 3.2 Create Test Base Classes

Create `SnapDog.Tests/TestBase.cs` with common test setup and utilities.

#### 3.3 AI Collaboration: Test Infrastructure

**AI Prompt Template**:

```
Context: Setting up comprehensive testing infrastructure for SnapDog using xUnit, FluentAssertions, and Moq.

Blueprint Reference:
- Document 18: Testing Strategy - Unit tests with 90%+ coverage
- TDD approach with test-first development
- Test categorization (Unit, Integration, E2E)

Task: Create test infrastructure including:
1. Base test class with common setup/teardown
2. Test utilities for configuration mocking
3. Test data builders/factories
4. Custom assertions for domain-specific validation
5. Test categorization attributes

Requirements:
- Follow xUnit best practices
- Include proper disposal patterns
- Support for async testing
- Configuration testing utilities
- Clean test organization
```

### Step 4: AI Collaboration Framework

#### 4.1 Create AI Context Templates

Create `docs/implementation/ai-templates/` directory with:

- `project-context.md` - Complete project context for AI
- `phase-0-context.md` - Phase-specific context
- `code-standards-reference.md` - Quick reference for coding standards

#### 4.2 AI Collaboration: Context Templates

**AI Prompt Template**:

```
Context: Creating AI collaboration templates for SnapDog development.

Blueprint Integration: Need to reference all 20+ blueprint documents in AI context templates.

Task: Create comprehensive AI context templates that include:
1. Project overview and architecture summary
2. Current phase objectives and constraints
3. Coding standards and conventions reference
4. Architecture decision quick reference
5. Common prompting patterns for different development tasks

Requirements:
- Concise but comprehensive context
- Easy to copy-paste into AI conversations
- Phase-specific variations
- Architecture compliance validation prompts
```

### Step 5: Basic Application Implementation

#### 5.1 Implement Program.cs

Create basic console application with:

- Configuration loading
- Logging setup
- Dependency injection container
- Basic error handling

#### 5.2 Create Configuration Validation

Implement configuration validation with proper error messages.

#### 5.3 AI Collaboration: Application Implementation

**AI Prompt Template**:

```
Context: Implementing basic console application for SnapDog Phase 0.

Blueprint References:
- Document 15: Development Environment
- Document 10: Configuration System
- Worker layer as entry point and composition root

Current Setup:
- EnvoyConfig for configuration loading
- Serilog for logging
- Microsoft.Extensions.Hosting for application lifecycle

Task: Implement Program.cs that:
1. Loads configuration from environment variables using EnvoyConfig
2. Sets up structured logging with Serilog
3. Configures dependency injection container
4. Validates configuration on startup
5. Provides clear error messages for configuration issues
6. Follows .NET hosting patterns

Requirements:
- Clean error handling and logging
- Proper application lifecycle management
- Configuration validation with helpful error messages
- Async/await patterns where appropriate
```

### Step 6: Testing and Validation

#### 6.1 Write Configuration Tests

Test configuration loading, validation, and error scenarios.

#### 6.2 Write Application Startup Tests

Test application initialization and error handling.

#### 6.3 Validate Code Coverage

Ensure 90%+ code coverage for all implemented components.

#### 6.4 AI Collaboration: Test Implementation

**AI Prompt Template**:

```
Context: Writing comprehensive tests for SnapDog Phase 0 components.

Test Strategy:
- Unit tests for configuration models and validation
- Integration tests for configuration loading from environment
- Application startup tests
- Error scenario testing

Task: Create comprehensive test suite including:
1. Configuration model tests with various scenarios
2. Environment variable loading tests
3. Configuration validation tests (valid/invalid scenarios)
4. Application startup and shutdown tests
5. Error handling tests

Requirements:
- 90%+ code coverage
- Clear test naming following pattern: MethodName_StateUnderTest_ExpectedBehavior
- Proper test organization and categorization
- Use FluentAssertions for readable assertions
- Mock external dependencies appropriately
```

## Expected Deliverable

### Working Console Application

```
SnapDog Configuration Validation Tool
=====================================

✓ Configuration loaded successfully
✓ Environment variables validated
✓ Logging configuration verified
✓ Application ready for Phase 1 development

Configuration Summary:
- Environment: Development
- Log Level: Information
- Clients Configured: 0
- Radio Stations: 0
- Telemetry: Enabled

Press any key to exit...
```

### Test Results

```
Test Results:
- Total Tests: 15
- Passed: 15
- Failed: 0
- Code Coverage: 92%

Test Categories:
- Configuration Tests: 8/8 passed
- Application Tests: 4/4 passed
- Validation Tests: 3/3 passed
```

## Quality Gates

### Code Quality Checklist

- [ ] All code follows blueprint coding standards
- [ ] Configuration properly validated
- [ ] Error handling implemented
- [ ] Logging properly configured
- [ ] Tests achieve 90%+ coverage
- [ ] No code smells or security issues

### Architecture Validation

- [ ] Project structure follows blueprint
- [ ] Layer dependencies are correct
- [ ] Configuration system matches blueprint specification
- [ ] Worker layer properly implemented as composition root

### AI Collaboration Validation

- [ ] Context templates are comprehensive and accurate
- [ ] Prompting patterns produce consistent results
- [ ] AI-generated code meets quality standards
- [ ] Documentation is complete and helpful

## Next Steps

Upon successful completion of Phase 0:

1. **Review and validate** all deliverables against success criteria
2. **Document lessons learned** from AI collaboration
3. **Prepare for Phase 1** by reviewing domain modeling requirements
4. **Update AI context templates** with Phase 0 implementation details
5. **Begin Phase 1** with confidence in solid foundation

## Troubleshooting

### Common Issues

- **Configuration not loading**: Check environment variable naming and EnvoyConfig attributes
- **Test failures**: Verify test data and mocking setup
- **Coverage issues**: Ensure all code paths are tested
- **AI context problems**: Validate template accuracy and completeness

### Resolution Patterns

- Use structured debugging with logging
- Validate configuration step-by-step
- Test individual components in isolation
- Iterate on AI prompts for better results

This phase establishes the critical foundation for all subsequent development phases, ensuring proper setup, configuration management, and AI-enhanced development workflow.
