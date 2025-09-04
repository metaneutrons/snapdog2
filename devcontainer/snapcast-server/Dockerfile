# ═══════════════════════════════════════════════════════════════════════════════
# SnapDog2 Snapcast Server - Development Container
# ═══════════════════════════════════════════════════════════════════════════════
# Simplified development build without D-Bus/Avahi for reliable container startup
# ═══════════════════════════════════════════════════════════════════════════════

# ═══════════════════════════════════════════════════════════════════════════════
# 🏗️ BUILDER STAGE - Compilation Environment
# ═══════════════════════════════════════════════════════════════════════════════
FROM alpine:3.21 AS builder

# Build configuration
ARG SNAPCAST_VERSION=0.32.3
ARG MAKEFLAGS="-j$(nproc)"

# Install build dependencies (minimal set for development)
RUN apk add --no-cache \
    build-base cmake git ccache pkgconfig \
    boost-dev alsa-lib-dev flac-dev libvorbis-dev opus-dev soxr-dev \
    openssl-dev ffmpeg-dev

# Configure build optimization
ENV CCACHE_DIR=/tmp/ccache PATH="/usr/lib/ccache/bin:$PATH"
RUN mkdir -p $CCACHE_DIR

# Build Snapcast Server (without Avahi for development)
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
        -DBUILD_WITH_AVAHI=OFF \
        -DBUILD_WITH_EXPAT=OFF \
        -DBUILD_TESTS=OFF \
        -DBUILD_CLIENT=OFF && \
    cmake --build build && \
    cmake --install build

# ═══════════════════════════════════════════════════════════════════════════════
# 🚀 RUNTIME STAGE - Development Image
# ═══════════════════════════════════════════════════════════════════════════════
FROM alpine:3.21 AS runtime

# Container metadata
LABEL maintainer="SnapDog2 Team" \
      description="SnapDog2 Snapcast Server - Development Build" \
      version="1.0.0-dev" \
      org.opencontainers.image.title="SnapDog2 Snapcast Server (Development)" \
      org.opencontainers.image.description="Simplified multi-room audio server for development"

# Install minimal runtime dependencies (no D-Bus/Avahi)
RUN apk add --no-cache \
    dumb-init supervisor bash curl \
    alsa-lib flac libvorbis opus soxr openssl ffmpeg

# Create snapcast user and directory structure
RUN addgroup -g 1000 snapcast && \
    adduser -D -u 1000 -G snapcast -h /var/lib/snapcast snapcast && \
    mkdir -p \
        /var/lib/snapcast /var/lib/snapserver /var/log/supervisor \
        /run/supervisord /snapsinks /etc/snapcast /usr/share/snapserver/snapweb \
        /home/snapcast/.config/snapserver /root/.config/snapserver && \
    chown -R snapcast:snapcast \
        /var/lib/snapcast /var/lib/snapserver /snapsinks \
        /home/snapcast /root/.config/snapserver && \
    chmod 0777 /snapsinks

# Copy built binaries
COPY --from=builder /usr/local/bin/snapserver /usr/local/bin/

# Install SnapWeb UI
RUN wget -qO /tmp/snapweb.zip \
        https://github.com/badaix/snapweb/releases/latest/download/snapweb.zip && \
    unzip -q /tmp/snapweb.zip -d /usr/share/snapserver/snapweb/ && \
    rm /tmp/snapweb.zip

# Copy SnapDog2 configuration files
COPY --chmod=755 setup.sh /usr/local/bin/snapdog-setup
COPY supervisord.conf /etc/supervisord.conf

# Create simplified development entrypoint (no D-Bus/Avahi)
RUN cat > /usr/local/bin/snapdog-entrypoint << 'EOF' && \
    chmod +x /usr/local/bin/snapdog-entrypoint
#!/bin/bash
set -euo pipefail

readonly SCRIPT_NAME="SnapDog2 Snapcast Server (Development)"

log() { echo "🎵 $SCRIPT_NAME: $*"; }

log "Starting development initialization..."

# Create audio sinks directory
log "Preparing audio sinks..."
mkdir -p /snapsinks
chown snapcast:snapcast /snapsinks
chmod 0777 /snapsinks

# Run SnapDog2 configuration
log "Generating configuration..."
snapdog-setup

# Clean up supervisor state
rm -f /run/supervisord/supervisor.sock

# Start services
log "Starting Snapcast server..."
exec supervisord -c /etc/supervisord.conf
EOF

# Environment configuration
ENV HOME=/home/snapcast

# Network ports
EXPOSE 1704/tcp 1705/tcp 1780/tcp

# Simplified health check for development
HEALTHCHECK --interval=30s --timeout=10s --start-period=15s --retries=3 \
    CMD set -euo pipefail; \
        echo "🏥 Development Health Check..."; \
        \
        supervisorctl -c /etc/supervisord.conf status >/dev/null || { \
            echo "❌ Supervisor services unhealthy"; exit 1; \
        }; \
        \
        curl -sf --max-time 5 -X POST \
            -d '{"id":1,"jsonrpc":"2.0","method":"Server.GetStatus"}' \
            "http://localhost:1780/jsonrpc" >/dev/null || { \
            echo "❌ Snapcast API unreachable"; exit 2; \
        }; \
        \
        echo "✅ Development server healthy"; exit 0

# Container execution
ENTRYPOINT ["/usr/bin/dumb-init", "--"]
CMD ["/usr/local/bin/snapdog-entrypoint"]
