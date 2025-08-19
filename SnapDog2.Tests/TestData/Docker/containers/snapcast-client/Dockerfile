# ═══════════════════════════════════════════════════════════════════════════════
# Test Snapcast Client Container
# ═══════════════════════════════════════════════════════════════════════════════
# Minimal Snapcast client optimized for testing - simulates audio playback devices
# ═══════════════════════════════════════════════════════════════════════════════

FROM debian:bookworm-slim

# Install Snapcast client and dependencies
RUN apt-get update && apt-get install -y \
    snapclient \
    alsa-utils \
    pulseaudio \
    && rm -rf /var/lib/apt/lists/*

# Create snapcast user
RUN useradd -r -s /bin/false snapclient

# Create dummy audio device for testing (null sink)
RUN mkdir -p /var/log/snapclient && \
    chown -R snapclient:snapclient /var/log/snapclient

# Expose client port for web interface
EXPOSE 1780

# Health check for test reliability
HEALTHCHECK --interval=10s --timeout=5s --start-period=15s --retries=3 \
    CMD pgrep snapclient || exit 1

USER snapclient

# Default command - will be overridden with specific client settings
CMD ["snapclient", "--host", "snapcast-server", "--port", "1704", "--player", "null"]
