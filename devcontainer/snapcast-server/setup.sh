#!/bin/bash

set -euo pipefail

# ═══════════════════════════════════════════════════════════════════════════════
# SnapDog2 Snapcast Server Configuration Generator
# ═══════════════════════════════════════════════════════════════════════════════

readonly SCRIPT_NAME="SnapDog2 Snapcast Server Setup"
readonly CONFIG_FILE="/etc/snapserver.conf"
readonly SNAPCAST_USER="snapcast"
readonly SNAPCAST_GROUP="snapcast"

# Default configuration values
declare -A DEFAULTS=(
    ["SAMPLE_RATE"]="48000"
    ["BIT_DEPTH"]="16" 
    ["CHANNELS"]="2"
    ["CODEC"]="flac"
    ["JSONRPC_PORT"]="1705"
    ["WEBSERVER_PORT"]="1780"
    ["WEBSOCKET_PORT"]="1704"
    ["BASE_URL"]=""
)

# ═══════════════════════════════════════════════════════════════════════════════
# Utility Functions
# ═══════════════════════════════════════════════════════════════════════════════

log() {
    echo "🔧 $*"
}

log_section() {
    echo
    echo "═══════════════════════════════════════════════════════════════════════════════"
    echo "🎵 $*"
    echo "═══════════════════════════════════════════════════════════════════════════════"
}

log_zone() {
    echo "  📍 Zone $1: $2"
}

log_config() {
    echo "    ├─ $1: $2"
}

# Get environment variable with fallback to default
get_env_or_default() {
    local var_name="$1"
    local default_key="$2"
    local env_value
    
    env_value=$(printenv "SNAPDOG_${var_name}" 2>/dev/null || echo "")
    echo "${env_value:-${DEFAULTS[$default_key]}}"
}

# Get audio configuration from global settings
get_audio_config() {
    local sample_rate bit_depth channels codec
    
    sample_rate=$(get_env_or_default "AUDIO_SAMPLE_RATE" "SAMPLE_RATE")
    bit_depth=$(get_env_or_default "AUDIO_BIT_DEPTH" "BIT_DEPTH")
    channels=$(get_env_or_default "AUDIO_CHANNELS" "CHANNELS")
    codec=$(get_env_or_default "AUDIO_CODEC" "CODEC")
    
    echo "${sample_rate}:${bit_depth}:${channels}:${codec}"
}

# Write configuration section to file
write_config_section() {
    local section="$1"
    shift
    
    {
        echo "[$section]"
        printf '%s\n' "$@"
        echo
    } >> "$CONFIG_FILE"
}

# ═══════════════════════════════════════════════════════════════════════════════
# Zone Discovery and Configuration
# ═══════════════════════════════════════════════════════════════════════════════

discover_zones() {
    local zones=()
    local zone_pattern="^SNAPDOG_ZONE_([0-9]+)_NAME$"
    
    # Find all zone name variables
    while IFS= read -r var; do
        if [[ $var =~ $zone_pattern ]]; then
            zones+=("${BASH_REMATCH[1]}")
        fi
    done < <(printenv | grep -E "$zone_pattern" | cut -d= -f1)
    
    # Sort zones numerically
    printf '%s\n' "${zones[@]}" | sort -n
}

configure_zone() {
    local zone_id="$1"
    local audio_config="$2"
    
    # Parse audio configuration
    IFS=':' read -r sample_rate bit_depth channels codec <<< "$audio_config"
    local sample_format="${sample_rate}:${bit_depth}:${channels}"
    
    # Get zone-specific configuration
    local zone_name sink_path
    zone_name=$(printenv "SNAPDOG_ZONE_${zone_id}_NAME")
    sink_path=$(printenv "SNAPDOG_ZONE_${zone_id}_SINK" 2>/dev/null || echo "/snapsinks/zone${zone_id}")
    
    log_zone "$zone_id" "$zone_name"
    log_config "Sink" "$sink_path"
    log_config "Sample Format" "$sample_format"
    log_config "Codec" "$codec"
    
    # Generate source configuration
    echo "source = pipe://${sink_path}?name=Zone${zone_id}&sampleformat=${sample_format}&codec=${codec}"
}

# ═══════════════════════════════════════════════════════════════════════════════
# Configuration File Generation
# ═══════════════════════════════════════════════════════════════════════════════

generate_snapserver_config() {
    local audio_config="$1"
    local sources="$2"
    
    # Parse audio configuration for stream section
    IFS=':' read -r sample_rate bit_depth channels codec <<< "$audio_config"
    local sample_format="${sample_rate}:${bit_depth}:${channels}"
    
    # Get port configuration
    local jsonrpc_port webserver_port websocket_port base_url
    jsonrpc_port=$(get_env_or_default "SNAPCAST_JSONRPC_PORT" "JSONRPC_PORT")
    webserver_port=$(get_env_or_default "SNAPCAST_WEBSERVER_PORT" "WEBSERVER_PORT")
    websocket_port=$(get_env_or_default "SNAPCAST_WEBSOCKET_PORT" "WEBSOCKET_PORT")
    base_url=$(get_env_or_default "SERVICES_SNAPCAST_BASE_URL" "BASE_URL")
    
    # Remove existing config and create directories
    rm -f "$CONFIG_FILE"
    mkdir -p /root/.config/snapserver
    chown "$SNAPCAST_USER:$SNAPCAST_GROUP" /root/.config/snapserver
    
    # Generate configuration sections
    write_config_section "server" \
        "user = $SNAPCAST_USER" \
        "group = $SNAPCAST_GROUP" \
        "datadir = /var/lib/snapserver/"
    
    write_config_section "tcp" \
        "enabled = true" \
        "bind_to_address = ::" \
        "port = $jsonrpc_port"
    
    write_config_section "http" \
        "enabled = true" \
        "bind_to_address = ::" \
        "port = $webserver_port" \
        "doc_root = /usr/share/snapserver/snapweb/" \
        "host = snapdog" \
        "base_url = $base_url"
    
    write_config_section "logging" \
        "sink = stdout" \
        "filter = *:info"
    
    write_config_section "stream" \
        "bind_to_address = ::" \
        "port = $websocket_port" \
        "sampleformat = $sample_format" \
        "codec = $codec" \
        "chunk_ms = 26" \
        "buffer = 1000" \
        "send_to_muted = false"
    
    # Add sources
    echo "$sources" >> "$CONFIG_FILE"
}

# ═══════════════════════════════════════════════════════════════════════════════
# Main Execution
# ═══════════════════════════════════════════════════════════════════════════════

main() {
    log_section "$SCRIPT_NAME"
    
    # Get global audio configuration
    local audio_config
    audio_config=$(get_audio_config)
    log "Global audio configuration: $audio_config"
    
    # Discover and configure zones
    log_section "Zone Discovery and Configuration"
    local zones sources=""
    readarray -t zones < <(discover_zones)
    
    if [[ ${#zones[@]} -eq 0 ]]; then
        log "⚠️  No zones found - creating default configuration"
        sources="# No zones configured"
    else
        log "Found ${#zones[@]} zone(s): ${zones[*]}"
        echo
        
        for zone_id in "${zones[@]}"; do
            local zone_source
            zone_source=$(configure_zone "$zone_id" "$audio_config")
            sources+="$zone_source"$'\n'
        done
    fi
    
    # Generate configuration file
    log_section "Generating Snapcast Configuration"
    generate_snapserver_config "$audio_config" "$sources"
    
    # Display results
    log_section "Configuration Summary"
    log "Sources configured:"
    echo "$sources"
    
    log_section "Generated Configuration File"
    cat "$CONFIG_FILE"
    
    log_section "Setup Complete"
    log "✅ Snapcast server configuration generated successfully"
}

# Execute main function
main "$@"
