#!/bin/bash
# SnapDog2 Conservative Cleanup - Remove Only Definitively Obsolete Files
# This script removes only files that we're 100% certain are obsolete

set -e

echo "🧹 SnapDog2 Conservative Cleanup - Definitively Obsolete Files Only"
echo "=================================================================="
echo ""

REMOVED_COUNT=0

# Function to safely remove a file with verification
remove_file() {
    local file="$1"
    local reason="$2"
    
    if [ -f "$file" ]; then
        echo "🗑️  Removing: $file"
        echo "   Reason: $reason"
        rm "$file"
        REMOVED_COUNT=$((REMOVED_COUNT + 1))
        echo "   ✅ Removed"
    else
        echo "⚠️  File not found: $file (already removed?)"
    fi
    echo ""
}

echo "Phase 1: Removing superseded behavior files"
echo "==========================================="

# These are 100% confirmed to be superseded by SharedLoggingBehavior
remove_file "SnapDog2/Server/Behaviors/LoggingCommandBehavior.cs" \
    "Superseded by SharedLoggingCommandBehavior in SharedLoggingBehavior.cs"

remove_file "SnapDog2/Server/Behaviors/LoggingQueryBehavior.cs" \
    "Superseded by SharedLoggingQueryBehavior in SharedLoggingBehavior.cs"

echo "Phase 2: Removing superseded consolidated command files"
echo "======================================================"

# These are 100% confirmed to be superseded by individual command files
remove_file "SnapDog2/Server/Features/Zones/Commands/ZoneCommands.cs" \
    "Superseded by individual command files in Commands/Playback/, Commands/Volume/, etc."

remove_file "SnapDog2/Server/Features/Clients/Commands/ClientVolumeCommands.cs" \
    "Superseded by individual command files in Commands/Volume/"

remove_file "SnapDog2/Server/Features/Clients/Commands/ClientConfigCommands.cs" \
    "Superseded by individual command files in Commands/Config/"

echo "Phase 3: Build verification"
echo "==========================="

echo "🔧 Running build to verify cleanup..."
if dotnet build --verbosity quiet; then
    echo "✅ Build successful - conservative cleanup verified!"
    echo ""
    echo "📊 Conservative Cleanup Summary:"
    echo "   Files removed: $REMOVED_COUNT"
    echo ""
    echo "🎉 Conservative cleanup completed successfully!"
    echo ""
    echo "These files were safely removed because they are definitively superseded:"
    echo "- LoggingCommandBehavior.cs → SharedLoggingCommandBehavior"
    echo "- LoggingQueryBehavior.cs → SharedLoggingQueryBehavior"  
    echo "- ZoneCommands.cs → Individual command files"
    echo "- ClientVolumeCommands.cs → Individual command files"
    echo "- ClientConfigCommands.cs → Individual command files"
    echo ""
    echo "💡 Additional cleanup opportunities exist but require more analysis:"
    echo "   - Old Controllers/ directory files (may be superseded by Api/Controllers/V1/)"
    echo "   - Unused notification files"
    echo "   - Unused model files"
    echo "   - Unused configuration files"
    echo "   - Test files that may not be referenced"
    echo ""
    echo "   Run the full analysis script again to identify more candidates."
else
    echo "❌ Build failed after cleanup!"
    echo "   This should not happen with the conservative cleanup."
    echo "   Please review the build errors."
    exit 1
fi
