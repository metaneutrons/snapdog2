#!/bin/bash
set -e

# In devcontainer, we use the fixed MAC address from docker-compose.yml
# Since reading from /sys/class/net can be unreliable in containers
# Use the MAC address from the environment variable or a fallback
if [ -n "$FIXED_MAC_ADDRESS" ]; then
    MAC_ADDRESS="$FIXED_MAC_ADDRESS"
else
    # Try to get MAC from network interface as fallback
    IFACE=$(ip -o link show | grep -v lo | grep -v "tunl0@NONE" | head -n1 | awk -F': ' '{print $2}')

    if [ -n "$IFACE" ] && [ -f "/sys/class/net/$IFACE/address" ]; then
        MAC_ADDRESS=$(cat /sys/class/net/$IFACE/address 2>/dev/null || echo "00:11:22:33:44:55")
    else
        # Use the container name as a deterministic way to generate a MAC
        CONTAINER_NAME=$(hostname)
        case "$CONTAINER_NAME" in
        "snapcast-client-1")
            MAC_ADDRESS="02:42:ac:11:00:10"
            ;;
        "snapcast-client-2")
            MAC_ADDRESS="02:42:ac:11:00:11"
            ;;
        "snapcast-client-3")
            MAC_ADDRESS="02:42:ac:11:00:12"
            ;;
        *)
            MAC_ADDRESS="00:11:22:33:44:55"
            ;;
        esac
    fi
fi

export MAC_ADDRESS="${MAC_ADDRESS}"
echo "Using MAC address: ${MAC_ADDRESS}"

# Get hostname
HOSTNAME=$(hostname)
echo "Using hostname: ${HOSTNAME}"

# Check if CLIENT_ID is set, if not generate one from hostname and MAC address
if [ -z "${CLIENT_ID}" ]; then
    # Extract last part of MAC address (last 6 characters without colons)
    MAC_SHORT=$(echo "${MAC_ADDRESS}" | tr -d ':' | tail -c 7)
    # Combine hostname and MAC short form
    export CLIENT_ID="${HOSTNAME}-${MAC_SHORT}"
    echo "CLIENT_ID not set, generated from hostname and MAC: ${CLIENT_ID}"
else
    echo "CLIENT_ID set to: ${CLIENT_ID}"
fi

# Configure null audio output
echo "Setting up null audio device"
modprobe -q snd-dummy || echo "Note: snd-dummy module not available in container, using ALSA null device"
mkdir -p /etc/asound.conf.d/
cat >/etc/asound.conf <<EOF
pcm.null {
    type null
}

ctl.null {
    type null
}

pcm.!default {
    type null
}

ctl.!default {
    type null
}
EOF

# Ensure supervisor directories exist
mkdir -p /run/supervisord /var/log/supervisor

# Start supervisord
exec /usr/bin/supervisord -c /etc/supervisord.conf
