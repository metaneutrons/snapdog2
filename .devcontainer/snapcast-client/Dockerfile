FROM alpine:latest

RUN apk update && \
    apk upgrade && \
    apk add --no-cache \
    snapcast-client \
    dumb-init \
    bash \
    supervisor \
    alsa-utils

# Create configuration and environment for supervisord
RUN mkdir -p /run/supervisord && \
    mkdir -p /etc/snapclient && \
    mkdir -p /var/log/supervisor

# Add supervisor configuration
COPY supervisord.conf /etc/supervisord.conf

# Add startup script
COPY start.sh /start.sh
RUN chmod +x /start.sh

EXPOSE 1704/tcp
EXPOSE 1705/tcp

HEALTHCHECK --interval=30s --timeout=10s --retries=3 CMD pgrep -x snapclient || exit 1

ENTRYPOINT ["/usr/bin/dumb-init", "--"]
CMD ["/start.sh"]