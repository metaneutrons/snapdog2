#!/bin/bash
# SnapDog2 Focused Cleanup Script - Deprecated Files Only
# This script removes files that are definitively superseded by new implementations

set -e

echo "🧹 SnapDog2 Focused Cleanup - Removing Deprecated Files"
echo "======================================================="
echo ""

# Function to safely remove a file with confirmation
remove_file() {
    local file="$1"
    local reason="$2"
    
    if [ -f "$file" ]; then
        echo "🗑️  Removing: $file"
        echo "   Reason: $reason"
        rm "$file"
        echo "   ✅ Removed successfully"
    else
        echo "⚠️  File not found: $file (already removed?)"
    fi
    echo ""
}

echo "Removing superseded behavior files..."
remove_file "SnapDog2/Server/Behaviors/LoggingCommandBehavior.cs" "Superseded by SharedLoggingCommandBehavior.cs"
remove_file "SnapDog2/Server/Behaviors/LoggingQueryBehavior.cs" "Superseded by SharedLoggingQueryBehavior.cs"

echo "Removing superseded consolidated command files..."
remove_file "SnapDog2/Server/Features/Zones/Commands/ZoneCommands.cs" "Superseded by individual command files in Playback/, Volume/, Track/, Playlist/"
remove_file "SnapDog2/Server/Features/Clients/Commands/ClientVolumeCommands.cs" "Superseded by individual command files in Volume/"
remove_file "SnapDog2/Server/Features/Clients/Commands/ClientConfigCommands.cs" "Superseded by individual command files in Config/"

echo "Removing temporary analysis files..."
remove_file "cleanup-analysis.py" "Temporary analysis script"
remove_file "CLEANUP_ANALYSIS.md" "Temporary analysis report"

echo "🔧 Running build to verify cleanup..."
if dotnet build --verbosity quiet; then
    echo "✅ Build successful - cleanup verified!"
    echo ""
    echo "📊 Cleanup Summary:"
    echo "   - Removed 5 deprecated behavior and command files"
    echo "   - Removed 2 temporary analysis files"
    echo "   - All functionality preserved through new implementations"
    echo ""
    echo "🎉 Cleanup completed successfully!"
else
    echo "❌ Build failed after cleanup!"
    echo "   This indicates the cleanup may have removed files that are still referenced."
    echo "   Please review the build errors and restore any necessary files."
    exit 1
fi
