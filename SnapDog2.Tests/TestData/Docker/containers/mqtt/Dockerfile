# ═══════════════════════════════════════════════════════════════════════════════
# Test MQTT Broker Container
# ═══════════════════════════════════════════════════════════════════════════════
# Minimal Eclipse Mosquitto broker optimized for testing
# ═══════════════════════════════════════════════════════════════════════════════

FROM eclipse-mosquitto:2.0-openssl

# Copy test-specific configuration
COPY mosquitto.test.conf /mosquitto/config/mosquitto.conf

# Create test user credentials
RUN echo "snapdog:snapdog" > /mosquitto/config/passwd && \
    mosquitto_passwd -U /mosquitto/config/passwd

# Expose MQTT port
EXPOSE 1883

# Health check for test reliability
HEALTHCHECK --interval=5s --timeout=3s --start-period=10s --retries=3 \
    CMD mosquitto_pub -h localhost -p 1883 -u snapdog -P snapdog -t health -m "check" || exit 1

CMD ["/usr/sbin/mosquitto", "-c", "/mosquitto/config/mosquitto.conf"]
