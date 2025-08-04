#!/bin/sh
set -e

echo "Starting KNX daemon setup..."

# Create necessary directories
mkdir -p /tmp /var/run/knxd /home/knxd/.config

# Create a minimal working knxd configuration file
cat > /home/knxd/.config/knxd.ini << EOF
[main]
addr = ${ADDRESS:-1.1.128}
client-addrs = ${CLIENT_ADDRESS:-1.1.129:8}
connections = A.tcp,dummy

[A.tcp]
server = knxd_tcp
port = 6720

[dummy]
driver = dummy
EOF

echo "Configuration file created:"
cat /home/knxd/.config/knxd.ini
echo "Starting knxd daemon..."

# Start knxd with configuration file
exec knxd /home/knxd/.config/knxd.ini ${KNXD_OPTS}
