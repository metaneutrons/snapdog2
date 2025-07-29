# 15. Development Environment

## 15.1. Overview

To ensure a consistent, reproducible, and dependency-managed development experience for all contributors across different operating systems (Windows, macOS, Linux), SnapDog2 mandates the use of **Visual Studio Code Development Containers (Dev Containers)**. This approach utilizes Docker containers to encapsulate the entire development environment, including the required .NET SDK, native dependencies (like LibVLC), necessary development tools (Git, formatters), pre-configured services (Snapcast Server, MQTT Broker, Subsonic Server), and IDE settings/extensions.

Developers only need Docker Desktop (or a compatible Docker engine) and Visual Studio Code with the "Dev Containers" extension installed on their host machine. Opening the project folder in VS Code will prompt the user to reopen it inside the configured container, automatically building the image (if necessary) and starting the associated services defined in Docker Compose.

## 15.2. Dev Container Setup

The configuration resides within the `.devcontainer` folder at the root of the repository.

### 15.2.1. Configuration Files

* **`.devcontainer/devcontainer.json`**: The primary configuration file read by the VS Code Dev Containers extension. It defines the container setup, services, VS Code settings, extensions, post-create commands, and environment variables specific to the development container.
* **`.devcontainer/Dockerfile`**: Defines the instructions to build the custom Docker image for the main `app` service where the SnapDog2 code runs and development occurs. This image includes the .NET SDK, LibVLC native dependencies, and other necessary tools.
* **`.devcontainer/docker-compose.yml`**: Defines the multi-container setup for the development environment using Docker Compose. It specifies the `app` service (built from the `Dockerfile`), dependent services like `snapserver`, `mosquitto`, `navidrome`, networking, and volume mounts.
* **`.devcontainer/snapserver.conf`**: (Example) A specific configuration file for the Snapcast server used *only* during development within the container.
* **`.devcontainer/mosquitto.conf`**: (Example) A specific configuration file for the Mosquitto broker used *only* during development.

### 15.2.2. `.devcontainer/devcontainer.json`

This file orchestrates the Dev Container setup for VS Code.

```json
{
    // Name displayed in VS Code UI
    "name": "SnapDog2 Development Environment",

    // Use Docker Compose to define the services
    "dockerComposeFile": "docker-compose.yml",

    // The primary service VS Code will connect to and where the workspace is mounted
    "service": "app",

    // The path inside the container where the project source code will be mounted
    "workspaceFolder": "/workspace",

    // VS Code settings applied specifically inside the Dev Container
    "settings": {
        // Shell Configuration
        "terminal.integrated.shell.linux": "/bin/zsh", // Use Zsh (installed by common-utils feature)
        "terminal.integrated.defaultProfile.linux": "zsh",

        // Formatting & Linting (align with project standards)
        "editor.formatOnSave": true,
        "editor.defaultFormatter": "ms-dotnettools.csharp", // Use C# Dev Kit formatter
        "editor.codeActionsOnSave": {
            "source.fixAll": "explicit" // Enable fix-all on save if desired
        },
        "editor.rulers": [120], // Visual guide for line length
        "dotnet.format.enable": true, // Ensure dotnet-format integration works

        // C# / OmniSharp / DevKit Settings
        "omnisharp.enableRoslynAnalyzers": true, // Ensure analyzers run
        "omnisharp.enableEditorConfigSupport": true,
        // Add C# Dev Kit specific settings if needed

        // File Settings
        "files.trimTrailingWhitespace": true,
        "files.insertFinalNewline": true,
        "files.eol": "\n", // Enforce Linux line endings

        // Other useful settings
        "workbench.colorTheme": "Default Dark Modern",
        "csharp.inlayHints.enableAllHints": true,
        "editor.unicodeHighlight.nonBasicASCII": false // Avoid highlighting common chars in logs/comments
    },

    // VS Code extensions automatically installed inside the Dev Container
    "extensions": [
        "ms-dotnettools.csharp", // C# Dev Kit (includes language service, debugger)
        "ms-azuretools.vscode-docker", // Docker integration
        "editorconfig.editorconfig", // EditorConfig support
        "streetsidesoftware.code-spell-checker", // Spell checking
        "github.copilot", // AI code assistance (optional)
        "github.copilot-chat", // AI chat (optional)
        "bierner.markdown-mermaid", // Preview Mermaid diagrams in Markdown
        "k--kato.docomment" // Helper for generating XML Doc Comments
    ],

    // The user VS Code runs as inside the container
    "remoteUser": "vscode", // Matches user created in Dockerfile

    // Install additional features/tools into the container OS
    "features": {
        // Installs common utilities like git, curl, openssl, process tools, etc.
        // Also configures a non-root user 'vscode' (can be customized)
        "ghcr.io/devcontainers/features/common-utils:2": {
             "installZsh": "true", // Install Zsh shell
             "configureZshAsDefaultShell": "true", // Set Zsh as default
             "username": "vscode",
             "uid": "1000",
             "gid": "1000",
             "upgradePackages": "true" // Keep packages updated
        },
        // Installs GitHub CLI
        "ghcr.io/devcontainers/features/github-cli:1": {}
        // Add other features if needed, e.g.:
        // "ghcr.io/devcontainers/features/node:1": {} // If Node.js needed
    },

    // Command(s) executed after the container is created but before VS Code connects
    // Useful for restoring dependencies, pre-compiling, etc.
    "postCreateCommand": "dotnet restore",

    // Environment variables passed specifically to the VS Code process/terminal inside the container
    "remoteEnv": {
        // Standard .NET environment variables for development
        "DOTNET_ENVIRONMENT": "Development",
        "ASPNETCORE_ENVIRONMENT": "Development",
        // Default logging level for development builds
        "SNAPDOG_LOG_LEVEL": "Debug",
        // Port the SnapDog2 ASP.NET Core app listens on INSIDE the container
        "ASPNETCORE_URLS": "http://+:8080",
        // --- Development Service Connection Strings ---
        // Pointing to service names defined in docker-compose.yml
        "SNAPDOG_SUBSONIC_ENABLED": "true",
        "SNAPDOG_SUBSONIC_SERVER": "http://navidrome:4533",
        "SNAPDOG_SUBSONIC_USERNAME": "admin",
        "SNAPDOG_SUBSONIC_PASSWORD": "devpassword", // Use non-prod password for dev instance
        "SNAPDOG_MQTT_ENABLED": "true",
        "SNAPDOG_MQTT_SERVER": "mosquitto",
        "SNAPDOG_MQTT_PORT": "1883",
        // Add MQTT dev credentials if configured in mosquitto.conf
        // "SNAPDOG_MQTT_USERNAME": "devuser",
        // "SNAPDOG_MQTT_PASSWORD": "devpass",
        "SNAPDOG_SNAPCAST_HOST": "snapserver",
        "SNAPDOG_SNAPCAST_CONTROL_PORT": "1705",
        // KNX is harder to simulate reliably, disable by default in dev container
        "SNAPDOG_KNX_ENABLED": "false",
        // Telemetry usually points to local containers in dev
        "SNAPDOG_TELEMETRY_ENABLED": "true", // Enable OTel for dev debugging
        "SNAPDOG_TELEMETRY_OTLP_ENDPOINT": "http://jaeger:4317", // Point to Jaeger container
        "SNAPDOG_PROMETHEUS_ENABLED": "true" // Enable Prometheus for dev debugging
        // Define sample Zone/Client config for development startup
        // "SNAPDOG_ZONE_1_NAME": "Dev Zone 1",
        // "SNAPDOG_ZONE_1_SINK": "/snapsinks/devzone1",
        // "SNAPDOG_CLIENT_1_NAME": "Dev Client 1",
        // "SNAPDOG_CLIENT_1_DEFAULT_ZONE": "1"
    }
}
```

### 15.2.3. `.devcontainer/docker-compose.yml` (Dev Environment)

Defines the interconnected services for the development environment, using a **shared Docker network** (`snapdog_dev_net`) for easy service discovery via container names.

```yaml
networks:
  # Define a custom bridge network for all development services
  snapdog_dev_net:
    driver: bridge
    name: snapdog_dev_net # Explicit name

services:
  # The application development service VS Code connects to
  app:
    # Build the image using the Dockerfile in the current (.devcontainer) directory
    build:
      context: .
      dockerfile: Dockerfile
    # Mount the entire project root directory into /workspace inside the container
    # Use 'cached' for better performance on macOS/Windows Docker Desktop
    volumes:
      - ../:/workspace:cached
      # Persist NuGet packages outside the container build layers for faster restores
      # Map to the non-root user's home directory (.nuget is typically there)
      - nuget_packages:/home/vscode/.nuget/packages
    # Keep the container running indefinitely for VS Code to connect to
    command: sleep infinity
    # Connect this service to the shared network
    networks:
      - snapdog_dev_net
    # Ensure dependency services are started first (optional, but helps ensure availability)
    depends_on:
      - snapserver
      - navidrome
      - mosquitto
      - jaeger # If telemetry is commonly used in dev
      - prometheus # If telemetry is commonly used in dev
    # Inherit environment variables defined in devcontainer.json's remoteEnv
    # Can add specific overrides here if needed, but remoteEnv is usually sufficient
    environment:
      - DOTNET_ENVIRONMENT=${DOTNET_ENVIRONMENT:-Development} # Use var from host/devcontainer.json or default
      - ASPNETCORE_ENVIRONMENT=${ASPNETCORE_ENVIRONMENT:-Development}
      # Forward necessary connection vars (already set in devcontainer.json remoteEnv)

  # Snapcast Server for development
  snapserver:
    # Use a specific version or the custom built image if applicable
    image: ghcr.io/badaix/snapcast:latest
    container_name: snapserver_dev # Consistent naming for dev
    restart: unless-stopped # Optional: restart if it crashes
    volumes:
      # Mount a development-specific configuration file
      - ./snapserver.conf:/etc/snapserver.conf:ro # Mount read-only
      # Mount a local directory for named pipe sinks if testing interaction
      - ./snapsinks_dev:/snapsinks # Host path needs to exist or be created
    ports: # Expose ports to the host machine for debugging/access
      - "1780:1780" # Snapweb UI (Access via http://localhost:1780)
      - "1705:1705" # Control Port
    networks:
      - snapdog_dev_net
    # Ensure the server starts with the specific configuration
    command: snapserver -c /etc/snapserver.conf

  # Navidrome (Subsonic compatible) for development
  navidrome:
    image: deluan/navidrome:latest
    container_name: navidrome_dev
    restart: unless-stopped
    ports:
      - "4533:4533" # Access via http://localhost:4533
    volumes:
      - navidrome_data_dev:/data # Use named volume for persistent data
      - ../sample-music:/music:ro # Mount sample music from project root (adjust path)
    networks:
      - snapdog_dev_net
    environment:
      - ND_SCANSCHEDULE=1m # Scan frequently in dev
      - ND_LOGLEVEL=debug
      # Set admin user/pass matching SNAPDOG_SUBSONIC_* in devcontainer.json
      # Navidrome typically uses ND_ADMIN_USERNAME / ND_ADMIN_PASSWORD on first run
      # Or set explicitly if needed:
      - ND_DEFAULTADMINPASSWORD=devpassword # Simplest for dev

  # Mosquitto MQTT Broker for development
  mosquitto:
    image: eclipse-mosquitto:2.0 # Use specific version
    container_name: mosquitto_dev
    restart: unless-stopped
    ports:
      - "1883:1883" # Standard MQTT port
      # - "9001:9001" # Optional: Expose websocket port
    volumes:
      # Mount development config, password file (if used), and data/log volumes
      - ./mosquitto.conf:/mosquitto/config/mosquitto.conf:ro
      # - ./mosquitto.passwd:/mosquitto/config/mosquitto.passwd:ro # If using auth
      - mosquitto_data_dev:/mosquitto/data
      - mosquitto_log_dev:/mosquitto/log
    networks:
      - snapdog_dev_net
    command: mosquitto -c /mosquitto/config/mosquitto.conf

  # --- Optional Observability Stack ---
  # Uses Docker Compose Profiles for activation

  jaeger:
    image: jaegertracing/all-in-one:1.53 # Use specific version
    container_name: jaeger_dev
    restart: unless-stopped
    ports:
      - "16686:16686"  # Jaeger UI (http://localhost:16686)
      - "14268:14268"  # Collector HTTP OTLP endpoint (often used if app sends direct)
      - "4317:4317"    # Collector gRPC OTLP endpoint (used by default OTel exporter)
      - "6831:6831/udp" # Agent Compact Thrift (if using agent)
    networks:
      - snapdog_dev_net
    environment:
      - COLLECTOR_OTLP_ENABLED=true # Ensure OTLP receiver is enabled
    profiles: ["metrics"] # Activate with --profile metrics

  prometheus:
    image: prom/prometheus:v2.48.1 # Use specific version
    container_name: prometheus_dev
    restart: unless-stopped
    ports:
      - "9090:9090" # Prometheus UI/API (http://localhost:9090)
    volumes:
      - ./prometheus.yml:/etc/prometheus/prometheus.yml:ro # Mount dev prometheus config
    networks:
      - snapdog_dev_net
    command: # Standard Prometheus command
      - '--config.file=/etc/prometheus/prometheus.yml'
      - '--storage.tsdb.path=/prometheus'
      - '--web.console.libraries=/usr/share/prometheus/console_libraries'
      - '--web.console.templates=/usr/share/prometheus/consoles'
      - '--web.enable-lifecycle' # Allows config reload via API
    profiles: ["metrics"]

  grafana:
    image: grafana/grafana:10.2.2 # Use specific version
    container_name: grafana_dev
    restart: unless-stopped
    ports:
      - "3000:3000" # Grafana UI (http://localhost:3000)
    volumes:
      # Mount provisioning folder for automatic datasources/dashboards setup
      - ./grafana_provisioning:/etc/grafana/provisioning:ro
      # Use named volume for Grafana database/plugin persistence
      - grafana_data_dev:/var/lib/grafana
    networks:
      - snapdog_dev_net
    environment:
      - GF_SECURITY_ADMIN_PASSWORD=devpassword # Set default admin password for dev
      # Add other Grafana config as needed
    depends_on:
      - prometheus # Ensure Prometheus is available for datasource provisioning
    profiles: ["metrics"]


# Define named volumes for persistent data in development
volumes:
  nuget_packages: # Persists downloaded NuGet packages
  navidrome_data_dev:
  mosquitto_data_dev:
  mosquitto_log_dev:
  grafana_data_dev: # Only needed if metrics profile is active

```

### 15.2.4. `.devcontainer/Dockerfile` (Dev Image)

Builds the image for the `app` service, including SDK, tools, and native dependencies.

```dockerfile
# Use the official .NET SDK image matching the project target framework (net9.0)
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS base

# Avoid prompts during package installation
ENV DEBIAN_FRONTEND=noninteractive

# Update package lists and install necessary tools and dependencies
RUN apt-get update && apt-get install -y --no-install-recommends \
    # Common development tools
    git \
    procps \
    curl \
    wget \
    unzip \
    jq \
    zsh \
    sudo \
    # LibVLC dependencies (adjust based on 'VideoLAN.LibVLC.Linux' needs and base image OS - this is for Debian/Ubuntu based)
    libvlc-dev \
    libvlccore-dev \
    vlc-plugin-base \
    vlc-plugin-access-extra \
    libavcodec-extra \
    # Clean up APT cache to reduce image size
    && apt-get autoremove -y && apt-get clean -y && rm -rf /var/lib/apt/lists/*

# Install dotnet-format tool globally (use a specific stable version)
RUN dotnet tool install --global dotnet-format --version 8.*

# Create a non-root user 'vscode' to match common dev container practices
ARG USERNAME=vscode
ARG USER_UID=1000
ARG USER_GID=$USER_UID
RUN groupadd --gid $USER_GID $USERNAME \
    && useradd -s /bin/bash --uid $USER_UID --gid $USER_GID -m $USERNAME \
    # Grant sudo privileges without password prompt (convenient for dev container)
    && echo $USERNAME ALL=\(root\) NOPASSWD:ALL > /etc/sudoers.d/$USERNAME \
    && chmod 0440 /etc/sudoers.d/$USERNAME

# Switch to the non-root user
USER $USERNAME

# Set the working directory for the user
WORKDIR /home/vscode

# Add dotnet tools directory to the PATH for the user
ENV PATH="/home/vscode/.dotnet/tools:${PATH}"

# Expose the application's default listening port (for documentation/reference)
EXPOSE 8080
```

### 15.2.5. Dev Container Benefits

1. **Consistency**: Guarantees every developer uses identical SDK versions, native libraries (LibVLC), external services (Snapcast, MQTT, Subsonic versions defined in compose file), tools, and IDE settings. Eliminates "works on my machine" issues.
2. **Pre-configured Services**: Development dependencies (Snapcast server, MQTT broker, Subsonic server, optional OTel stack) are automatically started, configured (via mounted dev configs), and networked for immediate use by the application container.
3. **IDE Integration**: VS Code automatically applies configured settings (`settings` in `devcontainer.json`) and installs required extensions (`extensions` list) within the containerized environment, providing a tailored IDE experience out-of-the-box.
4. **Isolation**: Development occurs entirely within containers, preventing conflicts with libraries or tools installed on the host operating system and keeping the host clean.
5. **Simplified Onboarding**: New developers only need Docker and VS Code. Cloning the repository and reopening in the container provides the complete, ready-to-run development environment in minutes, drastically reducing setup time and complexity.

## 15.3. Building the Application

Utilizes standard .NET CLI tooling, enhanced by scripts or IDE tasks for convenience.

### 15.3.1. Local Build Steps (Inside Dev Container)

These commands are run within the Dev Container's terminal:

1. **Restore Dependencies**: `dotnet restore SnapDog2.sln` (or project file). Usually run automatically by `postCreateCommand` or the IDE.
2. **Build**: `dotnet build SnapDog2.sln` (builds in `Debug` configuration by default). Use `dotnet build SnapDog2.sln -c Release` for Release build checks.
3. **Run Tests**: `dotnet test SnapDog2.Tests/SnapDog2.Tests.csproj` (or solution file). Runs all tests discovered in the specified project/solution. Add `-c Release` for Release config.
4. **Format Code**: `dotnet format SnapDog2.sln` Checks formatting against `.editorconfig`. Use `--verify-no-changes` in CI.
5. **Run Application**: `dotnet run --project SnapDog2/SnapDog2.csproj`. Starts the main application using Kestrel, listening on the port defined by `ASPNETCORE_URLS` (e.g., 8080 inside the container).

### 15.3.2. Build Automation (CI/CD - e.g., GitHub Actions)

Continuous Integration ensures code quality and produces build artifacts. A typical workflow (`.github/workflows/build-test.yml`) includes:

1. **Checkout**: Fetch the source code.
2. **Setup .NET SDK**: Install the required .NET 9 SDK version.
3. **Restore**: Run `dotnet restore`.
4. **Format Check**: Run `dotnet format --verify-no-changes`.
5. **Build**: Compile the application in `Release` configuration (`dotnet build -c Release --no-restore`). Enable TreatWarningsAsErrors for stricter checks.
6. **Test**: Run all tests (`dotnet test -c Release --no-build --logger "trx;LogFileName=test_results.trx"`). Collect code coverage using Coverlet (`--collect:"XPlat Code Coverage"`).
7. **Upload Artifacts**: Upload test results and coverage reports.
8. **(Optional) Analyze**: Integrate with SonarCloud/SonarQube for deeper analysis.
9. **(Optional - Separate Workflow/Job) Container Build**: Build production Docker image (See Section 17).
10. **(Optional - Separate Workflow/Job) Publish**: Push Docker image to registry (e.g., Docker Hub, GHCR).
