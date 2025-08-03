#!/bin/bash

set -e
echo "Starting SnapDog2 Development entrypoint.sh"
echo "------------------------------------------------"
echo "Creating zones for snapcast server configuration"

# AirPlay port for first instance
PORT=5555

# create snapsinks from environment variables
for var in "${!SNAPDOG_ZONE_@}"; do
    if [[ $var =~ "NAME" ]]; then
        echo
        ZONE=$(echo $var | cut -d'_' -f 3)

        ## Set name to environment variable
        eval NAME="\$SNAPDOG_ZONE_${ZONE}_NAME"
        echo "Creating Zone $ZONE: $NAME"

        ## Set sink to environment variable or default
        SINK="/snapsinks/zone$ZONE"
        eval SINK_VAR="\$SNAPDOG_ZONE_${ZONE}_SINK"
        if [[ -n "${SINK_VAR}" ]]; then
            SINK=$SINK_VAR
        fi
        echo -e "with sink: $SINK"

        ## Set codec to environment variable or default
        CODEC='flac'
        eval CODEC_VAR="\$SNAPDOG_SNAPCAST_CODEC"
        if [[ -n "${CODEC_VAR}" ]]; then
            CODEC=$CODEC_VAR
        fi
        echo -e "with codec: $CODEC"

        ## Set sampleformat to environment variable or default
        SAMPLEFORMAT="48000:16:2"
        eval SAMPLEFORMAT_VAR="\$SNAPDOG_SNAPCAST_SAMPLEFORMAT"
        if [[ -n "${SAMPLEFORMAT_VAR}" ]]; then
            SAMPLEFORMAT=$SAMPLEFORMAT_VAR
        fi
        echo -e "with sample format: $SAMPLEFORMAT"

        ## Add zone to snapserver configuration
        SNAPSERVER="${SNAPSERVER}source = pipe://$SINK?name=Zone$ZONE&sampleformat=$SAMPLEFORMAT&codec=$CODEC\n"

        echo -e "" >>/etc/supervisord.conf

        ((PORT++))
    fi
done

# Create the snapserver config directory and remove old config
rm -f /etc/snapserver.conf

# Create the snapserver settings directory
mkdir -p /root/.config/snapserver
chown snapcast:snapcast /root/.config/snapserver

echo -e '[server]' >>/etc/snapserver.conf
echo -e 'user = snapcast' >>/etc/snapserver.conf
echo -e 'group = snapcast' >>/etc/snapserver.conf
echo -e 'datadir = /var/lib/snapserver/' >>/etc/snapserver.conf
echo -e '[tcp]' >>/etc/snapserver.conf
echo -e 'enabled = true' >>/etc/snapserver.conf
echo -e 'bind_to_address = ::' >>/etc/snapserver.conf

## set port to environment variable or default
PORT="1705"
eval PORT_VAR="\$SNAPDOG_SNAPCAST_JSONRPC_PORT"
if [[ -n "${PORT_VAR}" ]]; then
    PORT=$PORT_VAR
fi

echo -e "port = $PORT" >>/etc/snapserver.conf

echo -e '[http]' >>/etc/snapserver.conf
echo -e 'enabled = true' >>/etc/snapserver.conf
echo -e 'bind_to_address = ::' >>/etc/snapserver.conf

## set port to environment variable or default
PORT="1780"
eval PORT_VAR="\$SNAPDOG_SNAPCAST_WEBSERVER_PORT"
if [[ -n "${PORT_VAR}" ]]; then
    PORT=$PORT_VAR
fi

echo -e "port = $PORT" >>/etc/snapserver.conf
echo -e 'doc_root = /usr/share/snapserver/snapweb/' >>/etc/snapserver.conf
echo -e 'host = snapdog' >>/etc/snapserver.conf

## set base_url for reverse proxy support (SnapWeb PR #20)
BASE_URL=""
eval BASE_URL_VAR="\$SNAPDOG_SERVICES_SNAPCAST_BASE_URL"
if [[ -n "${BASE_URL_VAR}" ]]; then
    BASE_URL=$BASE_URL_VAR
fi
echo -e "base_url = $BASE_URL" >>/etc/snapserver.conf

echo -e '[logging]' >>/etc/snapserver.conf
echo -e 'sink = stdout' >>/etc/snapserver.conf
echo -e 'filter = *:info' >>/etc/snapserver.conf
echo -e '[stream]' >>/etc/snapserver.conf
echo -e 'bind_to_address = ::' >>/etc/snapserver.conf

## set port to environment variable or default
PORT="1704"
eval PORT_VAR="\$SNAPDOG_SNAPCAST_WEBSOCKET_PORT"
if [[ -n "${PORT_VAR}" ]]; then
    PORT=$PORT_VAR
fi

echo -e "port = $PORT" >>/etc/snapserver.conf
echo -e "sampleformat = $SAMPLEFORMAT" >>/etc/snapserver.conf
echo -e 'codec = flac' >>/etc/snapserver.conf
echo -e 'chunk_ms = 26' >>/etc/snapserver.conf
echo -e 'buffer = 1000' >>/etc/snapserver.conf
echo -e 'send_to_muted = false' >>/etc/snapserver.conf

ESC_SNAPSERVER=$(printf '%s\n' "$SNAPSERVER" | sed -e 's/[\/&]/\\&/g')

echo -e $SNAPSERVER >>/etc/snapserver.conf
echo "------------------------------------"
echo "Sources:"
echo
echo -e $SNAPSERVER
echo "------------------------------------"
echo "/etc/snapserver.conf:"
echo
cat /etc/snapserver.conf
echo "------------------------------------"
