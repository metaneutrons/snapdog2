# ═══════════════════════════════════════════════════════════════════════════════
# Test Snapcast Server Container
# ═══════════════════════════════════════════════════════════════════════════════
# Minimal Snapcast server optimized for testing multi-room audio functionality
# ═══════════════════════════════════════════════════════════════════════════════

FROM debian:bookworm-slim

# Install Snapcast server and dependencies
RUN apt-get update && apt-get install -y \
    snapserver \
    curl \
    alsa-utils \
    pulseaudio \
    && rm -rf /var/lib/apt/lists/*

# Create snapcast user
RUN useradd -r -s /bin/false snapserver

# Create directories for audio streams
RUN mkdir -p /tmp/snapfifo /var/log/snapserver && \
    chown -R snapserver:snapserver /tmp/snapfifo /var/log/snapserver

# Copy test-specific configuration
COPY snapserver.test.conf /etc/snapserver.conf

# Create test audio streams (named pipes)
RUN mkfifo /tmp/snapfifo/zone1 && \
    mkfifo /tmp/snapfifo/zone2 && \
    chown snapserver:snapserver /tmp/snapfifo/zone*

# Expose Snapcast ports
EXPOSE 1704 1705 1780

# Health check for test reliability
HEALTHCHECK --interval=10s --timeout=5s --start-period=15s --retries=3 \
    CMD curl -f http://localhost:1780 || exit 1

USER snapserver
CMD ["snapserver", "-c", "/etc/snapserver.conf"]
