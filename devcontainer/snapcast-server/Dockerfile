# This Dockerfile is used to create a container for the Snapcast server.
# It is based on Alpine Linux and installs the necessary packages to run the Snapcast server.
# DEVELOPMENT VERSION for SnapDog2 Development
# NO AIRPLAY SUPPORT
FROM alpine:latest

# Install necessary packages
RUN apk update && \
    apk upgrade && \
    apk add --no-cache \
    snapcast-server \
    bash \
    dumb-init \
    curl \
    supervisor \
    tzdata

# Create directories
RUN mkdir -p /run/supervisord && \
    mkdir -p /var/log/supervisor && \
    mkdir -p /var/lib/snapserver && \
    mkdir -p /usr/share/snapserver/snapweb && \
    mkdir -p /snapsinks && \
    mkdir -p /home/snapcast && \
    mkdir -p /root/.config/snapserver && \
    chown -R snapcast:snapcast /home/snapcast && \
    chown -R snapcast:snapcast /snapsinks && \
    chown -R snapcast:snapcast /var/lib/snapserver && \
    chown -R snapcast:snapcast /root/.config/snapserver && \
    chmod 0777 /snapsinks

# Install latest snapweb
RUN wget --no-check-certificate -O /tmp/snapweb.zip https://github.com/badaix/snapweb/releases/latest/download/snapweb.zip && \
    unzip -o /tmp/snapweb.zip -d /usr/share/snapserver/snapweb/ && \
    rm /tmp/snapweb.zip

# Copy configuration files
COPY --chmod=0775 setup.sh /setup.sh
COPY --chmod=0775 healthcheck.sh /healthcheck.sh
COPY supervisord.conf /etc/supervisord.conf

EXPOSE 1704/tcp
EXPOSE 1705/tcp
EXPOSE 1780/tcp

HEALTHCHECK --interval=30s --timeout=10s --retries=3 CMD /healthcheck.sh

# Create a config directory that the snapcast user can use instead of /root/.config
RUN mkdir -p /home/snapcast/.config/snapserver && \
    chown -R snapcast:snapcast /home/snapcast/.config

# Set the HOME environment variable for snapserver
ENV HOME=/home/snapcast

ENTRYPOINT ["/usr/bin/dumb-init", "--"]
CMD ["/bin/sh", "-c", "rm -f /run/supervisord/supervisor.sock && /setup.sh && exec /usr/bin/supervisord -c /etc/supervisord.conf"]
