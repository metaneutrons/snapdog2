# ═══════════════════════════════════════════════════════════════════════════════
# Test Snapcast Client Container - Based on Proven Devcontainer Implementation
# ═══════════════════════════════════════════════════════════════════════════════
# Adapted from devcontainer/snapcast-client/Dockerfile with test optimizations
# Uses the same proven Alpine + supervisor approach but optimized for testing
# ═══════════════════════════════════════════════════════════════════════════════

FROM alpine:latest

# Install dependencies - same as devcontainer
RUN apk update && \
    apk upgrade && \
    apk add --no-cache \
    snapcast-client \
    dumb-init \
    bash \
    supervisor \
    alsa-utils

# Create configuration and environment - same as devcontainer approach
RUN mkdir -p /run/supervisord && \
    mkdir -p /etc/snapclient && \
    mkdir -p /var/log/supervisor

# Add supervisor configuration - adapted for testing
COPY supervisord.test.conf /etc/supervisord.conf

# Add startup script - adapted for testing
COPY start.test.sh /start.sh
RUN chmod +x /start.sh

# Expose ports - same as devcontainer
EXPOSE 1704/tcp
EXPOSE 1705/tcp

# Health check - same approach as devcontainer but test-optimized
HEALTHCHECK --interval=10s --timeout=5s --retries=3 --start-period=15s \
    CMD pgrep snapclient || exit 1

ENTRYPOINT ["/usr/bin/dumb-init", "--"]
CMD ["/start.sh"]
