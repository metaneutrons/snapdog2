# 24. Dependencies

This section meticulously documents the external software dependencies required by the SnapDog2 project, including the .NET framework version, essential NuGet packages, and necessary native libraries or external services. Managing these dependencies effectively is crucial for application stability, security, and maintainability.

## 24.1. .NET Framework

* **Target Framework:** SnapDog2 is built targeting **.NET 9.0**. Implementation should leverage the relevant features of the framework and the corresponding C# language version.
* **Core Components Utilized:**
  * ASP.NET Core: For hosting the REST API (Section 11) and potentially background services.
  * Generic Host: For application bootstrapping, configuration, logging, and dependency injection management (`/Worker/Program.cs`).
  * Dependency Injection: Built-in container (`Microsoft.Extensions.DependencyInjection`) used extensively for service registration and resolution.
  * Configuration: `Microsoft.Extensions.Configuration` used to load settings primarily from environment variables (Section 10).
  * Logging: `Microsoft.Extensions.Logging` abstractions used throughout the application, with Serilog as the backend provider (Section 5.2).
  * HttpClientFactory: Used for managing `HttpClient` instances, incorporating resilience policies (Section 7).

## 24.2. Core NuGet Packages

Dependencies are managed centrally via **Central Package Management (CPM)** using a `Directory.Packages.props` file at the solution root. Specific versions listed below are confirmed target versions; ensure `Directory.Packages.props` reflects these.

### 24.2.1. Application Framework & Utilities

These packages provide fundamental application building blocks and utility functions.

| Package                           | Specific Version | Purpose                                          | Notes                               |
| :-------------------------------- | :--------------- | :----------------------------------------------- | :---------------------------------- |
| `Microsoft.Extensions.Hosting`    | `9.0.3`          | Application hosting, DI, Configuration, Logging | Core requirement                    |
| `Microsoft.AspNetCore.App.Ref`    | `9.0.3`          | ASP.NET Core framework reference (for API Layer) | Metapackage for web functionalities |
| `System.Reactive`                 | `6.0.1`          | Reactive Extensions library                    | *(Only if explicitly used for complex event processing)* |
| `Cortex.Mediator`                 | `1.7.0`          | Mediator pattern implementation (CQRS)         | Core to application architecture      |
| `FluentValidation.AspNetCore`     | `11.11.0`        | Validation library & ASP.NET Core integration   | Used by ValidationBehavior & API    |
| `EnvoyConfig`                     | `1.0.5`          | Environment variable configuration binding      | Replaces custom EnvConfigHelper     |

### 24.2.2. Resilience

Packages providing fault-tolerance capabilities, primarily used in the Infrastructure layer.

| Package                         | Specific Version | Purpose                             | Notes                         |
| :------------------------------ | :--------------- | :---------------------------------- | :---------------------------- |
| `Polly`                         | `8.5.2`          | Resilience policies                 | Foundational resilience library |
| `Polly.Extensions.Http`         | `3.0.0`          | HTTP-specific policy helpers        | Used with HttpClientFactory   |
| `Microsoft.Extensions.Http.Polly` | `9.0.3`          | `HttpClientFactory` Polly integration | Enables policy registration   |
| `Polly.Contrib.WaitAndRetry`    | *(Included via Polly 8+)* | Provides advanced retry strategies (e.g., Jitter) | Ensure correct version if referenced separately |

### 24.2.3. Logging

Packages enabling structured logging via Serilog as the backend provider.

| Package                         | Specific Version | Purpose                     | Notes          |
| :------------------------------ | :--------------- | :-------------------------- | :------------- |
| `Serilog.AspNetCore`            | `9.0.0`          | Serilog ASP.NET Core integration | For request logging, context |
| `Serilog.Sinks.Console`         | `6.0.0`          | Console logging sink        | Essential for Docker logs |
| `Serilog.Sinks.File`            | `6.0.0`          | File logging sink           | For persistent logs |
| `Serilog.Sinks.Seq`             | `9.0.0`          | Seq logging sink (optional) | For centralized logging server |
| `Serilog.Enrichers.Environment` | `3.0.1`          | Enrich logs with env info   | Adds context       |
| `Serilog.Enrichers.Thread`      | `4.0.0`          | Enrich logs with thread ID  | Adds context       |

### 24.2.4. External System Integration

Libraries used to communicate with specific external services and protocols.

| Package                          | Specific Version   | Purpose                     | Notes                                    |
| :------------------------------- | :----------------- | :-------------------------- | :--------------------------------------- |
| `MQTTnet.Extensions.ManagedClient` | **`5.0.1.1416`**   | MQTT client library         | Use v5 due to breaking changes       |
| `Knx.Falcon.Sdk`                 | **`6.3.7959`**     | KNX integration SDK         | Confirmed version                        |
| `Sturd.SnapcastNet`              | **`0.0.4`**        | Snapcast control client     | Confirmed version                        |
| `LibVLCSharp`                    | **`3.8.2`**        | LibVLC bindings             | Confirmed version                        |
| `VideoLAN.LibVLC.Linux`          | `3.0.20`           | LibVLC native binaries (Linux) | Needed for target Docker runtime (Alpine might need different base or manual install) |
| `SubSonicMedia`                  | **`1.0.4-beta.1`** | Subsonic API client         | Confirmed (Monitor for stable release) |

### 24.2.5. Observability (OpenTelemetry)

Packages for implementing distributed tracing and metrics collection.

| Package                             | Specific Version   | Purpose                          | Notes                     |
| :---------------------------------- | :----------------- | :------------------------------- | :------------------------ |
| `OpenTelemetry.Extensions.Hosting`  | `1.11.2`           | Core OpenTelemetry integration   |                           |
| `OpenTelemetry.Instrumentation.AspNetCore` | `1.11.1`     | ASP.NET Core instrumentation     | For API Traces/Metrics    |
| `OpenTelemetry.Instrumentation.Http`| `1.11.1`           | HttpClient instrumentation       | For Subsonic calls etc.   |
| `OpenTelemetry.Instrumentation.Runtime` | `1.11.1`         | .NET Runtime metrics             | GC, Threading stats       |
| `OpenTelemetry.Exporter.Prometheus.AspNetCore` | `1.11.2-beta.1`| Prometheus metrics exporter    | Exposes `/metrics`        |
| `OpenTelemetry.Exporter.OpenTelemetryProtocol` | `1.11.2` | **OTLP Exporter**                | Replaces Jaeger Exporter  |
| `OpenTelemetry.Exporter.Console`    | `1.11.2`           | Console exporter                 | For debugging             |
| `OpenTelemetry.Api`                 | `1.11.2`           | Core API (`ActivitySource`, etc.)| For manual instrumentation|

### 24.2.6. Code Quality & Analysis

Packages providing static analysis during build, referenced as Development Dependencies / Analyzers.

| Package                             | Specific Version   | Purpose                      | Notes                         |
| :---------------------------------- | :----------------- | :--------------------------- | :---------------------------- |
| `StyleCop.Analyzers`                | `1.2.0-beta.556`   | Code style rules             | Include via `<PrivateAssets>` |
| `SonarAnalyzer.CSharp`              | `10.7.0.110445`    | Code quality/security rules  | Include via `<PrivateAssets>` |
| `Microsoft.CodeAnalysis.NetAnalyzers` | `9.0.0`            | Core .NET analysis rules     | Include via `<PrivateAssets>` |
| `Microsoft.VisualStudio.Threading.Analyzers` | `17.13.61`   | Async best practice rules    | Include via `<PrivateAssets>` |

### 24.2.7. Testing Dependencies

Packages used exclusively in the test project (`SnapDog2.Tests.csproj`).

| Package                          | Specific Version | Purpose                              | Scope      |
| :------------------------------- | :--------------- | :----------------------------------- | :--------- |
| `xunit`                          | `2.9.3`          | Unit testing framework               | Test Projs |
| `xunit.runner.visualstudio`      | `3.0.2`          | VS test execution                  | Test Projs |
| `Moq`                            | `4.20.72`        | Mocking framework                    | Test Projs |
| `FluentAssertions`               | `8.2.0`          | Fluent assertions                    | Test Projs |
| `Microsoft.NET.Test.Sdk`         | `17.13.0`        | Test SDK                             | Test Projs |
| `Microsoft.AspNetCore.Mvc.Testing` | `9.0.3`          | API integration testing          | Test Projs |
| `coverlet.collector`             | `6.0.4`          | Code coverage collection           | Test Projs |
| `Testcontainers`                 | `4.3.0`          | Docker containers for integration tests | Test Projs |

## 24.3. Centralized Package Management (`Directory.Packages.props`)

All NuGet package versions listed above **must** be defined centrally in the `Directory.Packages.props` file at the solution root to ensure consistency across projects (even though there is currently only one main project and one test project).

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <!-- Framework & Core -->
    <PackageVersion Include="Microsoft.Extensions.Hosting" Version="9.0.3" />
    <PackageVersion Include="Cortex.Mediator" Version="1.7.0" />
    <PackageVersion Include="FluentValidation.AspNetCore" Version="11.11.0" />
    <PackageVersion Include="EnvoyConfig" Version="1.0.0" />
    <!-- Resilience -->
    <PackageVersion Include="Polly" Version="8.5.2" />
    <PackageVersion Include="Polly.Extensions.Http" Version="3.0.0" />
    <PackageVersion Include="Microsoft.Extensions.Http.Polly" Version="9.0.3" />
    <!-- Logging -->
    <PackageVersion Include="Serilog.AspNetCore" Version="9.0.0" />
    <PackageVersion Include="Serilog.Sinks.Console" Version="6.0.0" />
    <PackageVersion Include="Serilog.Sinks.File" Version="6.0.0" />
    <PackageVersion Include="Serilog.Sinks.Seq" Version="9.0.0" /> <!-- Optional -->
    <PackageVersion Include="Serilog.Enrichers.Environment" Version="3.0.1" />
    <PackageVersion Include="Serilog.Enrichers.Thread" Version="4.0.0" />
    <!-- Integration -->
    <PackageVersion Include="MQTTnet.Extensions.ManagedClient" Version="5.0.1.1416" />
    <PackageVersion Include="Knx.Falcon.Sdk" Version="6.3.7959" />
    <PackageVersion Include="Sturd.SnapcastNet" Version="0.0.4" /> <!-- Confirm latest 0.0.x or 0.5.x if intended -->
    <PackageVersion Include="LibVLCSharp" Version="3.8.2" />
    <PackageVersion Include="VideoLAN.LibVLC.Linux" Version="3.0.20" />
    <PackageVersion Include="SubSonicMedia" Version="1.0.4-beta.1" />
    <!-- Observability -->
    <PackageVersion Include="OpenTelemetry.Extensions.Hosting" Version="1.11.2" />
    <PackageVersion Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.11.1" />
    <PackageVersion Include="OpenTelemetry.Instrumentation.Http" Version="1.11.1" />
    <PackageVersion Include="OpenTelemetry.Instrumentation.Runtime" Version="1.11.1" />
    <PackageVersion Include="OpenTelemetry.Exporter.Prometheus.AspNetCore" Version="1.11.2-beta.1" />
    <PackageVersion Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.11.2" />
    <PackageVersion Include="OpenTelemetry.Exporter.Console" Version="1.11.2" />
    <PackageVersion Include="OpenTelemetry.Api" Version="1.11.2" />
    <!-- Analysis -->
    <PackageVersion Include="StyleCop.Analyzers" Version="1.2.0-beta.556" />
    <PackageVersion Include="SonarAnalyzer.CSharp" Version="10.7.0.110445" />
    <PackageVersion Include="Microsoft.CodeAnalysis.NetAnalyzers" Version="9.0.0" />
    <PackageVersion Include="Microsoft.VisualStudio.Threading.Analyzers" Version="17.13.61" />
    <!-- Testing -->
    <PackageVersion Include="xunit" Version="2.9.3" />
    <PackageVersion Include="xunit.runner.visualstudio" Version="3.0.2" />
    <PackageVersion Include="Moq" Version="4.20.72" />
    <PackageVersion Include="FluentAssertions" Version="8.2.0" />
    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="17.13.0" />
    <PackageVersion Include="Microsoft.AspNetCore.Mvc.Testing" Version="9.0.3" />
    <PackageVersion Include="coverlet.collector" Version="6.0.4" />
    <PackageVersion Include="Testcontainers" Version="4.3.0" />
  </ItemGroup>
</Project>
```

Project files (`.csproj`) then reference packages without version numbers:

```xml
<!-- Example in SnapDog2.csproj -->
<ItemGroup>
  <PackageReference Include="Cortex.Mediator" />
  <PackageReference Include="Serilog.AspNetCore" />
  <PackageReference Include="Sturd.SnapcastNet" />
  <PackageReference Include="Knx.Falcon.Sdk" />
  <PackageReference Include="MQTTnet.Extensions.ManagedClient" />
  <PackageReference Include="LibVLCSharp" />
  <PackageReference Include="VideoLAN.LibVLC.Linux" />
  <PackageReference Include="SubSonicMedia" />
  <PackageReference Include="Polly" />
  <PackageReference Include="Microsoft.Extensions.Http.Polly" />
  <PackageReference Include="OpenTelemetry.Extensions.Hosting" />
  <!-- etc. -->
</ItemGroup>

<ItemGroup>
  <!-- Analyzers -->
  <PackageReference Include="StyleCop.Analyzers" PrivateAssets="all" />
  <PackageReference Include="SonarAnalyzer.CSharp" PrivateAssets="all" />
  <PackageReference Include="Microsoft.CodeAnalysis.NetAnalyzers" PrivateAssets="all" />
  <PackageReference Include="Microsoft.VisualStudio.Threading.Analyzers" PrivateAssets="all" />
</ItemGroup>
```

## 24.4. Dependency Management Strategy

* **Versioning**: Use CPM with specific, tested versions. Avoid wildcards. Update dependencies deliberately as part of planned maintenance, testing compatibility thoroughly.
* **Validation**: CI pipeline **must** include `dotnet list package --vulnerable --include-transitive` to check for known security vulnerabilities in direct and transitive dependencies. Optionally include license checking tools (e.g., `ClearlyDefined`, `GitHub License Check Action`).
* **Transitive Dependencies**: Regularly review the `project.assets.json` file or use `dotnet list package --include-transitive` to understand the full dependency graph. If problematic transitive versions are pulled in, explicitly define a `PackageVersion` for that transitive dependency in `Directory.Packages.props` to override it.

## 24.5. Native Dependencies

These are required at runtime in the execution environment (e.g., within the Docker container or on the host machine).

* **LibVLC**: Required by `LibVLCSharp`. The **native LibVLC libraries** (version compatible with the `VideoLAN.LibVLC.Linux` package, e.g., v3.0.x) must be installed in the runtime environment. The Dockerfiles (Sections 15 & 17) handle this installation for containerized deployments (e.g., using `apk add vlc-dev vlc` on Alpine or `apt-get install -y libvlc-dev libvlc5 ...` on Debian-based images).
* **Snapcast Server**: External dependency. Version `0.27.0` or higher recommended. Must be running and network-accessible from the SnapDog2 application container/host. Configuration (sinks, streams) managed as described in Section 17.
* **KNX Interface**: Requires a compatible physical KNX gateway (IP Tunneling/Routing) or USB Interface connected to the network or host machine accessible by SnapDog2. Appropriate OS-level drivers may be required for USB interfaces.
* **`knxd` (Testing Only):** Required **for running KNX integration tests** without physical hardware. Should be run within a Docker container managed by Testcontainers during the test execution lifecycle.
