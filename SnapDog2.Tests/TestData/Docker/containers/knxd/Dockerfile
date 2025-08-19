# ═══════════════════════════════════════════════════════════════════════════════
# Test KNX Daemon Container
# ═══════════════════════════════════════════════════════════════════════════════
# Minimal KNX daemon optimized for testing building automation functionality
# ═══════════════════════════════════════════════════════════════════════════════

FROM debian:bookworm-slim

# Install KNX daemon and dependencies
RUN apt-get update && apt-get install -y \
    knxd \
    knxd-tools \
    netcat-openbsd \
    && rm -rf /var/lib/apt/lists/*

# Create knxd user
RUN useradd -r -s /bin/false knxd

# Create directories
RUN mkdir -p /var/log/knxd /var/run/knxd && \
    chown -R knxd:knxd /var/log/knxd /var/run/knxd

# Copy test-specific configuration
COPY knxd.test.conf /etc/knxd.conf

# Expose KNX ports
EXPOSE 3671 6720

# Health check for test reliability
HEALTHCHECK --interval=10s --timeout=5s --start-period=15s --retries=3 \
    CMD nc -z localhost 3671 || exit 1

USER knxd

# Start KNX daemon in dummy mode for testing
CMD ["knxd", "--eibaddr=1.1.128", "--client-addrs=1.1.129:8", "--daemon=/var/run/knxd/knxd.pid", "--listen-tcp=3671", "--Discovery", "--Server", "dummy:"]
