# ═══════════════════════════════════════════════════════════════════════════════
# SnapDog2 Snapcast Server - Production Container
# ═══════════════════════════════════════════════════════════════════════════════
# Multi-stage build for optimized production image with streamlined configuration
# Aligned with SnapDog2's unified audio configuration system
# ═══════════════════════════════════════════════════════════════════════════════

# ═══════════════════════════════════════════════════════════════════════════════
# 🏗️ BUILDER STAGE - Compilation Environment
# ═══════════════════════════════════════════════════════════════════════════════
FROM alpine:3.21 AS builder

# Build configuration
ARG SNAPCAST_VERSION=0.31.0
ARG SHAIRPORT_SYNC_VERSION=4.3.7
ARG MAKEFLAGS="-j$(nproc)"

# Install build dependencies
RUN apk add --no-cache \
    build-base cmake git ccache pkgconfig \
    boost-dev alsa-lib-dev avahi-dev flac-dev libvorbis-dev opus-dev soxr-dev \
    openssl-dev ffmpeg-dev autoconf automake libtool libconfig-dev \
    libplist-dev libsodium-dev libgcrypt-dev dbus-dev expat-dev \
    libuuid util-linux-dev popt-dev

# Configure build optimization
ENV CCACHE_DIR=/tmp/ccache PATH="/usr/lib/ccache/bin:$PATH"
RUN mkdir -p $CCACHE_DIR

# Build Snapcast Server
WORKDIR /build/snapcast
RUN git clone --depth 1 --branch v${SNAPCAST_VERSION} \
        https://github.com/badaix/snapcast.git . && \
    cmake -B build -S . \
        -DCMAKE_BUILD_TYPE=Release \
        -DCMAKE_INSTALL_PREFIX=/usr/local \
        -DCMAKE_CXX_FLAGS="-O3 -DNDEBUG" \
        -DBUILD_WITH_FLAC=ON \
        -DBUILD_WITH_VORBIS=ON \
        -DBUILD_WITH_OPUS=ON \
        -DBUILD_WITH_AVAHI=ON \
        -DBUILD_WITH_EXPAT=ON \
        -DBUILD_TESTS=OFF \
        -DBUILD_CLIENT=OFF && \
    cmake --build build && \
    cmake --install build

# Build Shairport-Sync for AirPlay
WORKDIR /build/shairport-sync
RUN git clone --depth 1 --branch ${SHAIRPORT_SYNC_VERSION} \
        https://github.com/mikebrady/shairport-sync.git . && \
    autoreconf -i -f && \
    ./configure \
        --prefix=/usr/local \
        --with-alsa --with-avahi --with-ssl=openssl --with-soxr \
        --with-metadata --with-dbus-interface --with-mpris-interface \
        --with-pipe --with-stdout --sysconfdir=/etc && \
    make && make install

# ═══════════════════════════════════════════════════════════════════════════════
# 🚀 RUNTIME STAGE - Production Image
# ═══════════════════════════════════════════════════════════════════════════════
FROM alpine:3.21 AS runtime

# Container metadata
LABEL maintainer="SnapDog2 Team" \
      description="SnapDog2 Snapcast Server with streamlined configuration" \
      version="1.0.0" \
      org.opencontainers.image.title="SnapDog2 Snapcast Server" \
      org.opencontainers.image.description="Multi-room audio server with unified configuration system"

# Install runtime dependencies
RUN apk add --no-cache \
    dumb-init supervisor bash curl \
    alsa-lib avahi avahi-tools flac libvorbis opus soxr openssl ffmpeg \
    libconfig libplist libsodium libgcrypt dbus expat libuuid popt libdaemon

# Create snapcast user and directory structure
RUN addgroup -g 1000 snapcast && \
    adduser -D -u 1000 -G snapcast -h /var/lib/snapcast snapcast && \
    mkdir -p \
        /var/lib/snapcast /var/lib/snapserver /var/log/supervisor \
        /run/supervisord /snapsinks /etc/snapcast /usr/share/snapserver/snapweb \
        /home/snapcast/.config/snapserver /root/.config/snapserver \
        /run/dbus /run/avahi-daemon && \
    chown -R snapcast:snapcast \
        /var/lib/snapcast /var/lib/snapserver /snapsinks \
        /home/snapcast /root/.config/snapserver && \
    chmod 0777 /snapsinks

# Copy built binaries
COPY --from=builder /usr/local/bin/snapserver /usr/local/bin/
COPY --from=builder /usr/local/bin/shairport-sync /usr/local/bin/
COPY --from=builder /etc/shairport-sync.conf /etc/

# Install SnapWeb UI
RUN wget -qO /tmp/snapweb.zip \
        https://github.com/badaix/snapweb/releases/latest/download/snapweb.zip && \
    unzip -q /tmp/snapweb.zip -d /usr/share/snapserver/snapweb/ && \
    rm /tmp/snapweb.zip

# Copy SnapDog2 configuration files
COPY --chmod=755 setup.sh /usr/local/bin/snapdog-setup
COPY supervisord.conf /etc/supervisord.conf

# Create streamlined shairport-sync configuration
RUN cat > /etc/shairport-sync.conf << 'EOF'
// SnapDog2 AirPlay Configuration - Aligned with streamlined audio system
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
};

pipe = {
    name = "/snapsinks/airplay";
    audio_backend_latency_offset_in_seconds = 0.0;
    audio_backend_buffer_desired_length_in_seconds = 0.35;
};

metadata = {
    enabled = "yes";
    include_cover_art = "yes";
    pipe_name = "/tmp/shairport-sync-metadata";
    pipe_timeout = 5000;
};
EOF

# Create elegant startup script
RUN cat > /usr/local/bin/snapdog-entrypoint << 'EOF' && \
    chmod +x /usr/local/bin/snapdog-entrypoint
#!/bin/bash
set -euo pipefail

readonly SCRIPT_NAME="SnapDog2 Snapcast Server"

log() { echo "🎵 $SCRIPT_NAME: $*"; }

log "Starting initialization..."

# Initialize system services
log "Starting D-Bus and Avahi..."
mkdir -p /run/dbus /run/avahi-daemon
dbus-daemon --system --fork --nopidfile
avahi-daemon --daemonize --no-drop-root

# Wait for services
sleep 2

# Create AirPlay sink if enabled
if [[ "${SNAPDOG_AIRPLAY_ENABLED:-true}" == "true" ]]; then
    log "Preparing AirPlay integration..."
    [[ ! -p "/snapsinks/airplay" ]] && mkfifo /snapsinks/airplay
    chown snapcast:snapcast /snapsinks/airplay
fi

# Run SnapDog2 configuration
log "Generating configuration..."
snapdog-setup

# Clean up supervisor state
rm -f /run/supervisord/supervisor.sock

# Start services
log "Starting services..."
exec supervisord -c /etc/supervisord.conf
EOF

# Environment configuration
ENV HOME=/home/snapcast

# Network ports
EXPOSE 1704/tcp 1705/tcp 1780/tcp 5000/tcp 6001-6011/udp

# Health check - integrated into Dockerfile for elegance
HEALTHCHECK --interval=30s --timeout=10s --start-period=15s --retries=3 \
    CMD set -euo pipefail; \
        echo "🏥 SnapDog2 Health Check..."; \
        \
        supervisorctl -c /etc/supervisord.conf status >/dev/null || { \
            echo "❌ Supervisor services unhealthy"; exit 1; \
        }; \
        \
        JSONRPC_PORT=$(grep -E '^port = ' /etc/snapserver.conf | head -1 | cut -d' ' -f3 || echo 1705); \
        API_RESPONSE=$(curl -sf --max-time 5 -X POST \
            -d '{"id":1,"jsonrpc":"2.0","method":"Server.GetStatus"}' \
            "http://localhost:${JSONRPC_PORT}/jsonrpc" 2>/dev/null) || { \
            echo "❌ Snapcast API unreachable on port ${JSONRPC_PORT}"; exit 2; \
        }; \
        \
        echo "$API_RESPONSE" | grep -q '"snapserver"' || { \
            echo "❌ Invalid API response"; exit 3; \
        }; \
        \
        if [[ "${SNAPDOG_AIRPLAY_ENABLED:-true}" == "true" ]] && ! pgrep -f shairport-sync >/dev/null; then \
            echo "❌ AirPlay service not running"; exit 4; \
        fi; \
        \
        echo "✅ All services healthy"; exit 0

# Container execution
ENTRYPOINT ["/usr/bin/dumb-init", "--"]
CMD ["/usr/local/bin/snapdog-entrypoint"]
