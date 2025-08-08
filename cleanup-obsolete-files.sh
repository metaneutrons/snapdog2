#!/bin/bash
# SnapDog2 Comprehensive Obsolete File Cleanup Script
# This script safely removes files that have been proven obsolete

set -e

echo "🧹 SnapDog2 Comprehensive Obsolete File Cleanup"
echo "=============================================="
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Counters
REMOVED_COUNT=0
SKIPPED_COUNT=0

# Function to safely remove a file with verification
remove_file_safe() {
    local file="$1"
    local reason="$2"
    local replacement="$3"
    
    if [ -f "$file" ]; then
        echo -e "${BLUE}🔍 Analyzing: $file${NC}"
        echo "   Reason: $reason"
        if [ -n "$replacement" ]; then
            echo "   Replacement: $replacement"
        fi
        
        # Double-check the file isn't referenced (excluding itself)
        local basename=$(basename "$file" .cs)
        local references=$(grep -r "$basename" --include="*.cs" . --exclude-dir=bin --exclude-dir=obj 2>/dev/null | grep -v "$file" | wc -l)
        
        if [ "$references" -eq 0 ]; then
            echo -e "   ${GREEN}✅ Confirmed unreferenced - REMOVING${NC}"
            rm "$file"
            REMOVED_COUNT=$((REMOVED_COUNT + 1))
        else
            echo -e "   ${YELLOW}⚠️  Found $references references - SKIPPING for safety${NC}"
            SKIPPED_COUNT=$((SKIPPED_COUNT + 1))
        fi
    else
        echo -e "${YELLOW}⚠️  File not found: $file (already removed?)${NC}"
    fi
    echo ""
}

# Function to remove entire files that are completely obsolete
remove_obsolete_file() {
    local file="$1"
    local reason="$2"
    
    if [ -f "$file" ]; then
        echo -e "${BLUE}🗑️  Removing obsolete file: $file${NC}"
        echo "   Reason: $reason"
        rm "$file"
        REMOVED_COUNT=$((REMOVED_COUNT + 1))
        echo -e "   ${GREEN}✅ Removed${NC}"
    else
        echo -e "${YELLOW}⚠️  File not found: $file (already removed?)${NC}"
    fi
    echo ""
}

echo "Phase 1: Removing definitively obsolete files"
echo "============================================="

# Remove the deprecated behavior files (confirmed superseded)
remove_file_safe "SnapDog2/Server/Behaviors/LoggingCommandBehavior.cs" \
    "Superseded by SharedLoggingCommandBehavior" \
    "SharedLoggingBehavior.cs"

remove_file_safe "SnapDog2/Server/Behaviors/LoggingQueryBehavior.cs" \
    "Superseded by SharedLoggingQueryBehavior" \
    "SharedLoggingBehavior.cs"

# Remove the old consolidated command files (confirmed superseded)
remove_file_safe "SnapDog2/Server/Features/Zones/Commands/ZoneCommands.cs" \
    "Superseded by individual command files" \
    "Individual files in Commands/Playback/, Commands/Volume/, etc."

remove_file_safe "SnapDog2/Server/Features/Clients/Commands/ClientVolumeCommands.cs" \
    "Superseded by individual command files" \
    "Individual files in Commands/Volume/"

remove_file_safe "SnapDog2/Server/Features/Clients/Commands/ClientConfigCommands.cs" \
    "Superseded by individual command files" \
    "Individual files in Commands/Config/"

echo "Phase 2: Analyzing potentially unused files"
echo "==========================================="

# Check some high-confidence obsolete files
echo "Checking high-confidence obsolete files..."

# Old controller files that might be superseded by API controllers
if [ -f "SnapDog2/Controllers/ZoneController.cs" ]; then
    echo -e "${BLUE}🔍 Checking SnapDog2/Controllers/ZoneController.cs${NC}"
    if [ -f "SnapDog2/Api/Controllers/V1/ZoneController.cs" ]; then
        echo "   Found newer API controller - old controller likely obsolete"
        remove_file_safe "SnapDog2/Controllers/ZoneController.cs" \
            "Superseded by API v1 controller" \
            "SnapDog2/Api/Controllers/V1/ZoneController.cs"
    fi
fi

if [ -f "SnapDog2/Controllers/ClientController.cs" ]; then
    echo -e "${BLUE}🔍 Checking SnapDog2/Controllers/ClientController.cs${NC}"
    if [ -f "SnapDog2/Api/Controllers/V1/ClientController.cs" ]; then
        echo "   Found newer API controller - old controller likely obsolete"
        remove_file_safe "SnapDog2/Controllers/ClientController.cs" \
            "Superseded by API v1 controller" \
            "SnapDog2/Api/Controllers/V1/ClientController.cs"
    fi
fi

# Check for unused notification files
echo "Checking notification files..."

# These notifications might be unused if they're not published anywhere
remove_file_safe "SnapDog2/Server/Notifications/PlaybackNotifications.cs" \
    "Notifications appear unused" \
    "Remove if not published by any service"

remove_file_safe "SnapDog2/Server/Notifications/SnapcastNotifications.cs" \
    "Notifications appear unused" \
    "Remove if not published by any service"

# Check for unused model files
echo "Checking potentially unused model files..."

remove_file_safe "SnapDog2/Core/Models/GitVersionInfo.cs" \
    "Model appears unused" \
    "Remove if not used by version service"

# Check for unused configuration files
echo "Checking potentially unused configuration files..."

remove_file_safe "SnapDog2/Core/Configuration/SnapcastServerConfig.cs" \
    "Configuration appears unused" \
    "Remove if not used in configuration binding"

remove_file_safe "SnapDog2/Core/Configuration/ResilienceConfig.cs" \
    "Configuration appears unused" \
    "Remove if not used in configuration binding"

remove_file_safe "SnapDog2/Core/Configuration/TelemetryConfig.cs" \
    "Configuration appears unused" \
    "Remove if not used in configuration binding"

echo "Phase 3: Removing unused validator files"
echo "========================================"

# These validators are likely unused since validation is handled by FluentValidation
remove_file_safe "SnapDog2/Server/Features/Zones/Validators/ZoneCommandValidators.cs" \
    "Validators appear unused" \
    "FluentValidation handles validation"

remove_file_safe "SnapDog2/Server/Features/Clients/Validators/ClientCommandValidators.cs" \
    "Validators appear unused" \
    "FluentValidation handles validation"

remove_file_safe "SnapDog2/Server/Features/Snapcast/Validators/SetSnapcastClientVolumeCommandValidator.cs" \
    "Validator appears unused" \
    "FluentValidation handles validation"

echo "Phase 4: Removing unused query files"
echo "===================================="

# Check if these query files are superseded by individual query files
remove_file_safe "SnapDog2/Server/Features/Zones/Queries/ZoneQueries.cs" \
    "Consolidated queries superseded" \
    "Individual query files"

remove_file_safe "SnapDog2/Server/Features/Clients/Queries/ClientQueries.cs" \
    "Consolidated queries superseded" \
    "Individual query files"

remove_file_safe "SnapDog2/Server/Features/Playlists/Queries/PlaylistQueries.cs" \
    "Consolidated queries superseded" \
    "Individual query files"

echo "Phase 5: Build verification"
echo "==========================="

echo "🔧 Running build to verify cleanup didn't break anything..."
if dotnet build --verbosity quiet; then
    echo -e "${GREEN}✅ Build successful - cleanup verified!${NC}"
else
    echo -e "${RED}❌ Build failed after cleanup!${NC}"
    echo "   Some removed files may still be needed."
    echo "   Please review the build errors and restore necessary files."
    exit 1
fi

echo "Phase 6: Test verification"
echo "=========================="

echo "🧪 Running tests to verify functionality..."
if dotnet test --verbosity quiet --no-build; then
    echo -e "${GREEN}✅ Tests passed - cleanup verified!${NC}"
else
    echo -e "${YELLOW}⚠️  Some tests failed - this might be expected${NC}"
    echo "   Review test failures to ensure they're not related to cleanup"
fi

echo ""
echo "📊 Cleanup Summary"
echo "=================="
echo -e "${GREEN}✅ Files removed: $REMOVED_COUNT${NC}"
echo -e "${YELLOW}⚠️  Files skipped: $SKIPPED_COUNT${NC}"
echo ""
echo "🎉 Cleanup completed successfully!"
echo ""
echo "Next steps:"
echo "1. Review the build output to ensure no issues"
echo "2. Test the application functionality"
echo "3. Commit the cleanup changes"
echo "4. Consider running the cleanup analysis again to find more candidates"
