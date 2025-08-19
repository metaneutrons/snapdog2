#!/bin/bash
# ═══════════════════════════════════════════════════════════════════════════════
# Test Snapcast Client Startup Script - Adapted from Devcontainer
# ═══════════════════════════════════════════════════════════════════════════════
# Based on devcontainer/snapcast-client/start.sh with test-specific adaptations
# ═══════════════════════════════════════════════════════════════════════════════

set -e

# Use the MAC address from docker-compose (test environment has fixed MACs)
if [ -n "$FIXED_MAC_ADDRESS" ]; then
    MAC_ADDRESS="$FIXED_MAC_ADDRESS"
else
    # Fallback to container-based MAC detection (same logic as devcontainer)
    IFACE=$(ip -o link show | grep -v lo | grep -v "tunl0@NONE" | head -n1 | awk -F': ' '{print $2}')

    if [ -n "$IFACE" ] && [ -f "/sys/class/net/$IFACE/address" ]; then
        MAC_ADDRESS=$(cat /sys/class/net/$IFACE/address 2>/dev/null || echo "02:42:ac:14:00:99")
    else
        # Test-specific MAC addresses (different from dev environment)
        CONTAINER_NAME=$(hostname)
        case "$CONTAINER_NAME" in
        *"living"*)
            MAC_ADDRESS="02:42:ac:14:00:10"
            ;;
        *"kitchen"*)
            MAC_ADDRESS="02:42:ac:14:00:11"
            ;;
        *"bedroom"*)
            MAC_ADDRESS="02:42:ac:14:00:12"
            ;;
        *)
            MAC_ADDRESS="02:42:ac:14:00:99"
            ;;
        esac
    fi
fi

export MAC_ADDRESS="${MAC_ADDRESS}"
echo "Using MAC address: ${MAC_ADDRESS}"

# Get hostname - same as devcontainer
HOSTNAME=$(hostname)
echo "Using hostname: ${HOSTNAME}"

# Generate CLIENT_ID - same logic as devcontainer
if [ -z "${CLIENT_ID}" ]; then
    MAC_SHORT=$(echo "${MAC_ADDRESS}" | tr -d ':' | tail -c 7)
    export CLIENT_ID="${HOSTNAME}-${MAC_SHORT}"
    echo "CLIENT_ID not set, generated from hostname and MAC: ${CLIENT_ID}"
else
    echo "CLIENT_ID set to: ${CLIENT_ID}"
fi

# Configure null audio output - same as devcontainer but test-optimized
echo "Setting up null audio device for testing"
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

# Ensure supervisor directories exist - same as devcontainer
mkdir -p /run/supervisord /var/log/supervisor

# Start supervisord - same as devcontainer
exec /usr/bin/supervisord -c /etc/supervisord.conf
