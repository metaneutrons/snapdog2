# Multi-stage Dockerfile for SnapDog2
# Supports both development (with debugging) and production builds

# Development stage - for debugging and hot reload
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS development
WORKDIR /app

# Install development tools for container debugging
RUN apt-get update && apt-get install -y \
    curl \
    git \
    procps \
    iputils-ping \
    telnet \
    && rm -rf /var/lib/apt/lists/*

# Create a non-root user for development (consistent with volume permissions)
RUN useradd -m -s /bin/bash -u 1000 vscode && \
    mkdir -p /home/vscode/.nuget/packages && \
    chown -R vscode:vscode /home/vscode

# Organize project files and fix permissions
#RUN for file in $(ls *.csproj); do mkdir -p ${file%.*}/ && mv $file ${file%.*}/; done && \
#    chown -R vscode:vscode /app

# Switch to vscode user for development
USER vscode

# Skip restore during build - dotnet watch will handle it at runtime with bind-mounted local packages
# RUN dotnet restore

# Switch back to root for remaining setup
USER root

# Expose only HTTP port
EXPOSE 5000

# Development entrypoint with hot reload (HTTP only for internal networking)
ENTRYPOINT ["sh", "-c", "dotnet watch --project SnapDog2"]

# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

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

# Install dependencies if needed (audio libraries, etc.)
RUN apt-get update && apt-get install -y --no-install-recommends \
    ca-certificates \
    procps \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .

# Create a non-root user to run the application
RUN useradd -M -s /bin/bash -u 1000 snapdog && \
    chown -R snapdog:snapdog /app

USER snapdog

# Set the entry point
ENTRYPOINT ["dotnet", "SnapDog2.dll"]
