# This Dockerfile is used to create a container for the Snapcast server.
# It is based on Alpine Linux and installs the necessary packages to run the Snapcast server.
# PRODUCTION VERSOON for SnapDog2
FROM alpine AS builder-base
RUN apk update && \
    apk upgrade && \
    apk add --no-cache \
    git \
    build-base \
    autoconf \
    automake \
    libtool \
    dbus \
    su-exec \
    alsa-lib-dev \
    libdaemon-dev \
    popt-dev \
    mbedtls-dev \
    openssl-dev \
    expat-dev \
    soxr-dev \
    avahi-dev \
    libconfig-dev \
    libsndfile-dev \
    mosquitto-dev \
    xmltoman

# ALAC Build System:
FROM builder-base AS builder-alac

RUN git clone https://github.com/mikebrady/alac
WORKDIR /alac
RUN autoreconf -fi
RUN ./configure
RUN make
RUN make install

# Shairport Sync Build System:
FROM builder-base AS builder-sps

# this may be modified by the Github Action Workflow.
ARG SHAIRPORT_SYNC_BRANCH=master

COPY --from=builder-alac /usr/local/lib/libalac.* /usr/local/lib/
COPY --from=builder-alac /usr/local/lib/pkgconfig/alac.pc /usr/local/lib/pkgconfig/alac.pc
COPY --from=builder-alac /usr/local/include /usr/local/include

RUN git clone https://github.com/mikebrady/shairport-sync
WORKDIR /shairport-sync
RUN git checkout "$SHAIRPORT_SYNC_BRANCH"
RUN autoreconf -fi
RUN ./configure \
    --with-alsa \
    --with-dummy \
    --with-pipe \
    --with-stdout \
    --with-avahi \
    --with-ssl=openssl \
    --with-soxr \
    --sysconfdir=/etc \
    --with-dbus-interface \
    --with-mpris-interface \
    --with-mqtt-client \
    --with-apple-alac \
    --with-convolution \
    --with-metadata
RUN make -j $(nproc)
RUN make install

# Snapcast with Shairport Sync and ALAC runtime
LABEL maintainer="Fabian Schmieder (github@schmieder.eu)"
LABEL version="1.0"
LABEL org.label-schema.name="Snapcast Server Docker" \
    org.label-schema.description="Snapcast server on alpine image with Avahi and D-Bus support (for SnapDog)" \
    org.label-schema.schema-version="1.0"

FROM alpine
RUN apk update && \
    apk upgrade

RUN apk add --no-cache \
    alsa-lib \
    popt \
    glib \
    soxr \
    libconfig \
    libsndfile \
    mosquitto-libs \
    su-exec \
    libgcc \
    libgc++ \
    dumb-init \
    coreutils \
    curl \
    alpine-sdk \
    avahi \
    avahi-tools \
    dbus \
    snapcast-server \
    vlc \
    bash

# copy shairport-sync and alac libraries from builder
COPY --from=builder-alac /usr/local/lib/libalac.* /usr/local/lib/
COPY --from=builder-sps /etc/shairport-sync* /etc/
COPY --from=builder-sps /etc/dbus-1/system.d/shairport-sync-dbus.conf /etc/dbus-1/system.d/
COPY --from=builder-sps /etc/dbus-1/system.d/shairport-sync-mpris.conf /etc/dbus-1/system.d/
COPY --from=builder-sps /usr/local/bin/shairport-sync /usr/local/bin/shairport-sync

# create non-root user for running the container -- running as the user 'shairport-sync' also allows
# Shairport Sync to provide the D-Bus and MPRIS interfaces within the container
RUN addgroup -g 29 docker_audio
RUN deluser snapcast
RUN adduser -h /home/snapcast -D snapcast
RUN chown -R snapcast:snapcast /home/snapcast

# create configuration and environment for supervisord
RUN apk add --no-cache --upgrade supervisor tzdata curl \
    && mkdir /run/supervisord \
    && sed -i 's/^files =.*/files = \/home\/snapcast\/*.conf/g' /etc/supervisord.conf \
    && sed -i 's/\/run\/supervisord.sock/\/run\/supervisord\/supervisord.sock/g' /etc/supervisord.conf

# add mopidy
RUN apk add --no-cache --upgrade gstreamer mopidy

# add setup script
ADD --chown=snapcast:snapcast --chmod=0775 setup.sh /home/snapcast/setup.sh

# add healtcheck script
ADD --chown=snapcast:snapcast --chmod=0775 healthcheck.sh /home/snapcast/healthcheck.sh

# install latest release of snapweb
# (it's not shipped with snapcast-server, and not shipped with snapcast since 0.28.0 anymore
RUN wget --no-check-certificate -O /tmp/snapweb.zip https://github.com/badaix/snapweb/releases/latest/download/snapweb.zip \
    && unzip -o /tmp/snapweb.zip -d /usr/share/snapserver/snapweb/ \
    && rm /tmp/snapweb.zip

# create directory for snapcast fifo
RUN mkdir -p /snapsinks/ ; chown -R snapcast:audio /snapsinks ; chmod 0777 /snapsinks

# configure dbus-daemon and avahi-daemon for rootless execution
# https://gnaneshkunal.github.io/avahi-docker-non-root.html

ADD --chown=snapcast:snapcast ./supervisord.conf /home/snapcast/supervisord.conf
RUN echo "<busconfig><listen>unix:path=/var/run/dbus/system_bus_socket</listen></busconfig>" > /usr/share/dbus-1/session.d/custom.conf \
    && mkdir -p /var/run/dbus \
    && chmod 777 /var/run/dbus/ \
    && dbus-uuidgen > /var/lib/dbus/machine-id \
    && chmod 777 /etc/avahi/avahi-daemon.conf \
    && mkdir -p /var/run/avahi-daemon \
    && chown avahi:avahi /var/run/avahi-daemon \
    && chmod 777 /var/run/avahi-daemon

# set chown for files in /home/snapcast
RUN chown -R snapcast:snapcast /home/snapcast

# cleanup
RUN rm -rf /etc/ssl /var/cache/apk/* /lib/apk/db/*
RUN rm -rf  /lib/apk/db/*

EXPOSE 1704/tcp
EXPOSE 1705/tcp
EXPOSE 1780/tcp
# expose AirPlay ports for each Zone - this is configured in the setup.sh script and starting with port 5555
EXPOSE 5555-5655/tcp
EXPOSE 5353/udp
EXPOSE 10001/tcp

# ENTRYPOINT /home/snapcast/entrypoint.sh
# Run start script
HEALTHCHECK --interval=30s --timeout=10s --retries=3 CMD /home/snapcast/healthcheck.sh
ENTRYPOINT [ "/bin/sh", "-c" ]
CMD [ "/home/snapcast/setup.sh && /usr/bin/supervisord -c /etc/supervisord.conf" ]
