# ═══════════════════════════════════════════════════════════════════════════════
# Test Snapcast Server Container - Based on Proven Devcontainer Implementation
# ═══════════════════════════════════════════════════════════════════════════════
# Adapted from devcontainer/snapcast-server/Dockerfile with test optimizations
# Uses the same proven Alpine + compiled approach but optimized for testing
# ═══════════════════════════════════════════════════════════════════════════════

# ═══════════════════════════════════════════════════════════════════════════════
# 🏗️ BUILDER STAGE - Same as devcontainer but test-optimized
# ═══════════════════════════════════════════════════════════════════════════════
FROM alpine:3.21 AS builder

# Build configuration - same as devcontainer
ARG SNAPCAST_VERSION=0.31.0
ARG MAKEFLAGS="-j$(nproc)"

# Install build dependencies - same as devcontainer
RUN apk add --no-cache \
    build-base cmake git ccache pkgconfig \
    boost-dev alsa-lib-dev flac-dev libvorbis-dev opus-dev soxr-dev \
    openssl-dev ffmpeg-dev

# Configure build optimization - same as devcontainer
ENV CCACHE_DIR=/tmp/ccache PATH="/usr/lib/ccache/bin:$PATH"
RUN mkdir -p $CCACHE_DIR

# Build Snapcast Server - same approach as devcontainer
WORKDIR /build/snapcast
RUN git clone --depth 1 --branch v${SNAPCAST_VERSION} \
        https://github.com/badaix/snapcast.git . && \
    cmake -B build -S . \
        -DCMAKE_BUILD_TYPE=Release \
        -DCMAKE_CXX_COMPILER_LAUNCHER=ccache \
        -DBUILD_CLIENT=OFF \
        -DBUILD_SERVER=ON \
        -DBUILD_WITH_AVAHI=OFF \
        -DBUILD_WITH_PULSE=OFF && \
    cmake --build build --target snapserver

# ═══════════════════════════════════════════════════════════════════════════════
# 🚀 RUNTIME STAGE - Optimized for testing
# ═══════════════════════════════════════════════════════════════════════════════
FROM alpine:3.21

# Install runtime dependencies - same as devcontainer
RUN apk add --no-cache \
    boost-system boost-program_options \
    alsa-lib flac libvorbis opus soxr \
    openssl ffmpeg-libs \
    curl \
    && rm -rf /var/cache/apk/*

# Create snapcast user - same approach as devcontainer
RUN addgroup -g 1000 snapserver \
    && adduser -D -u 1000 -G snapserver -h /home/snapserver -s /bin/sh snapserver

# Create directories - enhanced for testing
RUN mkdir -p \
    /tmp/snapfifo \
    /var/log/snapserver \
    /home/snapserver/.config \
    && chown -R snapserver:snapserver \
        /tmp/snapfifo \
        /var/log/snapserver \
        /home/snapserver

# Copy binary from builder - same as devcontainer
COPY --from=builder /build/snapcast/build/server/snapserver /usr/local/bin/

# Copy test-specific configuration
COPY snapserver.test.conf /etc/snapserver.conf

# Create test audio streams (named pipes) - test-specific
RUN mkfifo /tmp/snapfifo/zone1 && \
    mkfifo /tmp/snapfifo/zone2 && \
    chown snapserver:snapserver /tmp/snapfifo/zone*

# Expose Snapcast ports - same as devcontainer
EXPOSE 1704 1705 1780

# Health check for test reliability
HEALTHCHECK --interval=10s --timeout=5s --start-period=15s --retries=3 \
    CMD curl -f http://localhost:1780 || exit 1

USER snapserver
CMD ["snapserver", "-c", "/etc/snapserver.conf"]
