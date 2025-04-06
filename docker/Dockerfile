# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["SnapDog.sln", "./"]
COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]
COPY ["global.json", "./"]
COPY ["stylecop.json", "./"]
COPY ["SnapDog/SnapDog.csproj", "./SnapDog/"]
RUN dotnet restore

# Copy the rest of the code
COPY . .

# Build the application
RUN dotnet build "SnapDog/SnapDog.csproj" -c Release -o /app/build
RUN dotnet publish "SnapDog/SnapDog.csproj" -c Release -o /app/publish

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

# Install VLC and dependencies
RUN apt-get update && apt-get install -y --no-install-recommends \
    libvlc-dev \
    vlc \
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
ENTRYPOINT ["dotnet", "SnapDog.dll"]