# =============================================================================
# Architecture:
# - Builder stage: Full compilation environment for snapcast + shairport-sync
# - Runtime stage: Minimal Alpine with SnapDog2 configuration system
# - Environment-driven configuration for zones and settings
# - Supervisor orchestration for multiple services
# - Named pipes for multiple audio sources
# - Avahi/mDNS for network discovery
# - D-Bus integration for metadata and control
# =============================================================================

# =============================================================================
# 🏗️ BUILDER STAGE - Compilation Environment
# =============================================================================
FROM alpine:3.21 AS builder

# Build arguments for optimization
ARG SNAPCAST_VERSION=0.31.0
ARG SHAIRPORT_SYNC_VERSION=4.3.7

# Install comprehensive build dependencies
RUN apk add --no-cache \
    # Core build tools
    build-base \
    cmake \
    git \
    ccache \
    pkgconfig \
    # Audio libraries
    boost-dev \
    alsa-lib-dev \
    avahi-dev \
    flac-dev \
    libvorbis-dev \
    opus-dev \
    soxr-dev \
    # Network and security
    openssl-dev \
    # FFmpeg for advanced codec support
    ffmpeg-dev \
    # Additional dependencies for shairport-sync
    autoconf \
    automake \
    libtool \
    libconfig-dev \
    libplist-dev \
    libsodium-dev \
    libgcrypt-dev \
    dbus-dev \
    expat-dev \
    libuuid \
    util-linux-dev \
    popt-dev

# Configure ccache for faster rebuilds
ENV CCACHE_DIR=/tmp/ccache
ENV PATH="/usr/lib/ccache/bin:$PATH"
RUN mkdir -p $CCACHE_DIR

# =============================================================================
# 📻 Build Snapcast Server from Source
# =============================================================================
WORKDIR /build/snapcast

# Clone and build snapcast with full feature support
RUN git clone --depth 1 --branch v${SNAPCAST_VERSION} \
    https://github.com/badaix/snapcast.git . && \
    mkdir build && cd build && \
    cmake .. \
        -DCMAKE_BUILD_TYPE=Release \
        -DCMAKE_INSTALL_PREFIX=/usr/local \
        -DCMAKE_CXX_FLAGS="-march=native -mtune=native -O3" \
        -DBUILD_WITH_FLAC=ON \
        -DBUILD_WITH_VORBIS=ON \
        -DBUILD_WITH_OPUS=ON \
        -DBUILD_WITH_AVAHI=ON \
        -DBUILD_WITH_EXPAT=ON \
        -DBUILD_TESTS=OFF \
        -DBUILD_CLIENT=OFF && \
    make -j$(nproc) && \
    make install

# =============================================================================
# 🍎 Build Shairport-Sync for AirPlay Support
# =============================================================================
WORKDIR /build/shairport-sync

RUN git clone --depth 1 --branch ${SHAIRPORT_SYNC_VERSION} \
    https://github.com/mikebrady/shairport-sync.git . && \
    autoreconf -i -f && \
    ./configure \
        --prefix=/usr/local \
        --with-alsa \
        --with-avahi \
        --with-ssl=openssl \
        --with-soxr \
        --with-metadata \
        --with-dbus-interface \
        --with-mpris-interface \
        --with-pipe \
        --with-stdout \
        --sysconfdir=/etc && \
    make -j$(nproc) && \
    make install

# =============================================================================
# 🚀 RUNTIME STAGE - SnapDog2 Production Image
# =============================================================================
FROM alpine:3.21 AS runtime

# Metadata labels for container registry
LABEL maintainer="SnapDog2 Team" \
      description="Award-winning SnapDog2 Snapcast Server with AirPlay support" \
      version="1.0.0" \
      org.opencontainers.image.title="SnapDog2 Snapcast Server" \
      org.opencontainers.image.description="Multi-room audio server with TCP JSON-RPC, AirPlay integration, and SnapDog2 configuration system" \
      org.opencontainers.image.vendor="SnapDog2" \
      org.opencontainers.image.licenses="GPL-3.0"

# Install minimal runtime dependencies
RUN apk add --no-cache \
    # Process management
    dumb-init \
    supervisor \
    # Audio runtime libraries
    alsa-lib \
    avahi \
    avahi-tools \
    flac \
    libvorbis \
    opus \
    soxr \
    # Network and security runtime
    openssl \
    # FFmpeg runtime
    ffmpeg \
    # Additional runtime for shairport-sync
    libconfig \
    libplist \
    libsodium \
    libgcrypt \
    dbus \
    expat \
    libuuid \
    popt \
    libdaemon \
    # System utilities
    bash \
    curl \
    wget \
    unzip \
    tzdata \
    # Logging
    rsyslog

# Create dedicated snapcast user for security (matching SnapDog2 requirements)
RUN addgroup -g 1000 snapcast && \
    adduser -D -u 1000 -G snapcast -h /var/lib/snapcast snapcast

# Copy built binaries from builder stage
COPY --from=builder /usr/local/bin/snapserver /usr/local/bin/
COPY --from=builder /usr/local/bin/shairport-sync /usr/local/bin/
COPY --from=builder /etc/shairport-sync.conf /etc/

# Create SnapDog2-specific directory structure
RUN mkdir -p \
    /var/lib/snapcast \
    /var/lib/snapserver \
    /var/log/supervisor \
    /run/supervisord \
    /snapsinks \
    /etc/snapcast \
    /usr/share/snapserver/snapweb \
    /home/snapcast/.config/snapserver \
    /root/.config/snapserver \
    /run/dbus \
    /run/avahi-daemon && \
    # Set proper ownership for SnapDog2 requirements
    chown -R snapcast:snapcast /var/lib/snapcast && \
    chown -R snapcast:snapcast /var/lib/snapserver && \
    chown -R snapcast:snapcast /snapsinks && \
    chown -R snapcast:snapcast /home/snapcast && \
    chown -R snapcast:snapcast /root/.config/snapserver && \
    chmod 0777 /snapsinks

# =============================================================================
# 📦 Install Latest SnapWeb UI (SnapDog2 Requirement)
# =============================================================================
RUN wget --no-check-certificate -O /tmp/snapweb.zip \
    https://github.com/badaix/snapweb/releases/latest/download/snapweb.zip && \
    unzip -o /tmp/snapweb.zip -d /usr/share/snapserver/snapweb/ && \
    rm /tmp/snapweb.zip

# =============================================================================
# 📝 SnapDog2 Configuration Files
# =============================================================================

# Copy SnapDog2-specific configuration and scripts
COPY --chmod=0775 setup.sh /setup.sh
COPY --chmod=0775 healthcheck.sh /healthcheck.sh
COPY supervisord.conf /etc/supervisord.conf

# Enhanced shairport-sync configuration for SnapDog2
COPY <<EOF /etc/shairport-sync.conf
// SnapDog2 AirPlay Configuration
// Award-winning AirPlay 1 & 2 integration with SnapDog2

general = {
    name = "SnapDog2 AirPlay";
    interpolation = "soxr";
    output_backend = "pipe";
    mdns_backend = "avahi";
    port = 5000;
    udp_port_base = 6001;
    udp_port_range = 10;
    drift_tolerance_in_seconds = 0.002;
    resync_threshold_in_seconds = 0.050;
    log_verbosity = 1;
    ignore_volume_control = "no";
    volume_range_db = 60;
    regtype = "_raop._tcp";
    playbook_mode = "stereo";
};

pipe = {
    name = "/snapsinks/airplay";
    audio_backend_latency_offset_in_seconds = 0.0;
    audio_backend_buffer_desired_length_in_seconds = 0.35;
};

dsp = {
    convolution = "no";
    convolution_gain = -20.0;
    convolution_max_length = 8192;
    loudness = "no";
    loudness_reference_volume_db = -23.0;
};

metadata = {
    enabled = "yes";
    include_cover_art = "yes";
    pipe_name = "/tmp/shairport-sync-metadata";
    pipe_timeout = 5000;
    socket_address = "226.0.0.1";
    socket_port = 5555;
    socket_msglength = 65000;
};

sessioncontrol = {
    allow_session_interruption = "yes";
    session_timeout = 120;
};
EOF

# =============================================================================
# 🚀 Enhanced Startup Script for SnapDog2 + AirPlay
# =============================================================================

# Create enhanced startup script that combines SnapDog2 setup with AirPlay
COPY <<'EOF' /snapdog-startup.sh
#!/bin/bash
set -euo pipefail

echo "🎵 Starting SnapDog2 Award-Worthy Snapcast Server..."

# Start D-Bus (required for shairport-sync)
echo "📡 Starting D-Bus..."
mkdir -p /run/dbus
dbus-daemon --system --fork --nopidfile

# Start Avahi daemon (required for mDNS/AirPlay discovery)
echo "🔍 Starting Avahi daemon..."
mkdir -p /run/avahi-daemon
avahi-daemon --daemonize --no-drop-root

# Wait for services to be ready
sleep 2

# Start shairport-sync for AirPlay support (if enabled)
if [ "${SNAPDOG_AIRPLAY_ENABLED:-true}" = "true" ]; then
    echo "🍎 Starting AirPlay service..."
    # Create AirPlay sink if it doesn't exist
    if [ ! -p "/snapsinks/airplay" ]; then
        mkfifo /snapsinks/airplay
        chown snapcast:snapcast /snapsinks/airplay
    fi
    # Note: shairport-sync will be managed by supervisor, not started here
fi

# Run SnapDog2 setup script
echo "⚙️  Running SnapDog2 configuration setup..."
/setup.sh

# Clean up any existing supervisor socket
rm -f /run/supervisord/supervisor.sock

# Start supervisor with SnapDog2 configuration
echo "📻 Starting SnapDog2 Snapcast services..."
exec /usr/bin/supervisord -c /etc/supervisord.conf
EOF

RUN chmod +x /snapdog-startup.sh

# =============================================================================
# 🏥 Enhanced Health Check for SnapDog2 + AirPlay
# =============================================================================

# Create comprehensive health check that includes AirPlay
COPY <<'EOF' /snapdog-healthcheck.sh
#!/bin/bash
set -euo pipefail

echo "🏥 SnapDog2 Award-Worthy Health Check..."

# Use SnapDog2's original health check first
if ! /healthcheck.sh; then
    echo "❌ SnapDog2 health check failed"
    exit 1
fi

# Additional checks for award-worthy features
echo "🔍 Checking award-worthy features..."

# Check if AirPlay is enabled and running
if [ "${SNAPDOG_AIRPLAY_ENABLED:-true}" = "true" ]; then
    if ! pgrep -f shairport-sync > /dev/null; then
        echo "❌ AirPlay service not running"
        exit 1
    fi

    # Check AirPlay port
    if ! nc -z localhost 5000 2>/dev/null; then
        echo "❌ AirPlay port 5000 not accessible"
        exit 1
    fi

    echo "✅ AirPlay service healthy"
fi

# Check if Avahi is running
if ! pgrep -f avahi-daemon > /dev/null; then
    echo "❌ Avahi daemon not running"
    exit 1
fi

# Check D-Bus
if ! pgrep -f dbus-daemon > /dev/null; then
    echo "❌ D-Bus daemon not running"
    exit 1
fi

echo "✅ All award-worthy features healthy!"
exit 0
EOF

RUN chmod +x /snapdog-healthcheck.sh

# =============================================================================
# 🔧 Final Configuration
# =============================================================================

# Set HOME environment variable for snapserver (SnapDog2 requirement)
ENV HOME=/home/snapcast

# Expose all necessary ports
EXPOSE \
    1704/tcp \
    1705/tcp \
    1780/tcp \
    5000/tcp \
    6001-6011/udp

# Health check configuration using enhanced health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=15s --retries=3 \
    CMD /snapdog-healthcheck.sh

# Use dumb-init for proper signal handling (SnapDog2 requirement)
ENTRYPOINT ["/usr/bin/dumb-init", "--"]

# Start the award-worthy SnapDog2 service
CMD ["/snapdog-startup.sh"]
# =============================================================================
