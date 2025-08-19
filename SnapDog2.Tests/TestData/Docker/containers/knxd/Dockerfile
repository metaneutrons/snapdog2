# ═══════════════════════════════════════════════════════════════════════════════
# Test KNX Daemon Container - Based on Proven Devcontainer Implementation
# ═══════════════════════════════════════════════════════════════════════════════
# Adapted from devcontainer/knxd/Dockerfile with test optimizations
# Uses the same proven Alpine edge + knxd approach but optimized for testing
# ═══════════════════════════════════════════════════════════════════════════════

FROM alpine:edge

# Enable testing repository for knxd package - same as devcontainer
RUN echo "https://dl-cdn.alpinelinux.org/alpine/edge/testing" >> /etc/apk/repositories

# Install knxd and dependencies - same as devcontainer
RUN apk add --no-cache \
    knxd \
    libusb \
    libev \
    fmt \
    tini \
    net-tools \
    && rm -rf /var/cache/apk/*

# Create knxd user and group - same as devcontainer
RUN addgroup -g 1000 knxd \
    && adduser -D -u 1000 -G knxd -h /home/knxd -s /bin/sh knxd

# Create necessary directories - same as devcontainer
RUN mkdir -p \
    /var/run/knxd \
    /var/log/knxd \
    /home/knxd/.config \
    /home/knxd/data \
    && chown -R knxd:knxd \
        /var/run/knxd \
        /var/log/knxd \
        /home/knxd

# Copy test-specific configuration
COPY knxd.test.conf /etc/knxd.conf

# Expose KNX ports - same as devcontainer
EXPOSE 3671 6720

# Health check for test reliability
HEALTHCHECK --interval=10s --timeout=5s --start-period=15s --retries=3 \
    CMD nc -z localhost 3671 || exit 1

USER knxd

# Start KNX daemon in dummy mode for testing - adapted for test environment
CMD ["knxd", "--eibaddr=1.1.128", "--client-addrs=1.1.129:8", "--daemon=/var/run/knxd/knxd.pid", "--listen-tcp=3671", "--Discovery", "--Server", "dummy:"]
