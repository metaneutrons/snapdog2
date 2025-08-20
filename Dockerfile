# Multi-stage Dockerfile for SnapDog2
# Supports both development (with debugging) and production builds

# Development stage - for debugging and hot reload
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS development
WORKDIR /app

# Install development tools and LibVLC dependencies (both dev and runtime)
RUN apt-get update && apt-get install -y \
    curl \
    git \
    procps \
    iputils-ping \
    telnet \
    libvlc-dev \
    libvlccore-dev \
    libvlc5 \
    libvlccore9 \
    vlc-plugin-base \
    && rm -rf /var/lib/apt/lists/* \
    && /usr/lib/aarch64-linux-gnu/vlc/vlc-cache-gen /usr/lib/aarch64-linux-gnu/vlc/plugins

# Set LibVLC environment variables for ARM64 architecture
ENV VLC_PLUGIN_PATH=/usr/lib/aarch64-linux-gnu/vlc/plugins
ENV LD_LIBRARY_PATH=/usr/lib/aarch64-linux-gnu

# Create a non-root user for development (consistent with volume permissions)
RUN useradd -m -s /bin/bash -u 1000 vscode && \
    mkdir -p /home/vscode/.nuget/packages && \
    mkdir -p /home/vscode/.nuget/local && \
    chown -R vscode:vscode /home/vscode

# Switch to vscode user for development
USER vscode

# Expose only HTTP port
EXPOSE 5000

# Direct entrypoint - no script needed!
# Volume mounts provide source code and local packages at runtime
ENTRYPOINT ["dotnet", "watch", "--project", "SnapDog2/SnapDog2.csproj", "run"]

# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Install LibVLC development dependencies for build
RUN apt-get update && apt-get install -y \
    libvlc-dev \
    libvlccore-dev \
    && rm -rf /var/lib/apt/lists/*

# Copy csproj and restore dependencies
COPY ["SnapDog2.sln", "./"]
COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]
COPY ["global.json", "./"]
COPY ["stylecop.json", "./"]
COPY ["nuget.config", "./"]
COPY ["SnapDog2/SnapDog2.csproj", "./SnapDog2/"]
COPY ["SnapDog2.Tests/SnapDog2.Tests.csproj", "./SnapDog2.Tests/"]
RUN dotnet restore

# Copy the rest of the code
COPY . .

# Build the application
RUN dotnet build "SnapDog2/SnapDog2.csproj" -c Release -o /app/build
RUN dotnet publish "SnapDog2/SnapDog2.csproj" -c Release -o /app/publish

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS production

# Install LibVLC runtime dependencies (not development packages)
RUN apt-get update && apt-get install -y --no-install-recommends \
    ca-certificates \
    procps \
    libvlc5 \
    libvlccore9 \
    vlc-plugin-base \
    && rm -rf /var/lib/apt/lists/* \
    && /usr/lib/aarch64-linux-gnu/vlc/vlc-cache-gen /usr/lib/aarch64-linux-gnu/vlc/plugins

# Set LibVLC environment variables for ARM64 architecture
ENV VLC_PLUGIN_PATH=/usr/lib/aarch64-linux-gnu/vlc/plugins
ENV LD_LIBRARY_PATH=/usr/lib/aarch64-linux-gnu

WORKDIR /app
COPY --from=build /app/publish .

# Create a non-root user to run the application
RUN useradd -M -s /bin/bash -u 1000 snapdog && \
    chown -R snapdog:snapdog /app

USER snapdog

# Set the entry point
ENTRYPOINT ["dotnet", "SnapDog2.dll"]
