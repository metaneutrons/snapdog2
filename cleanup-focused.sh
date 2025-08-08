#!/bin/bash
# SnapDog2 Focused Cleanup - Remove Obviously Obsolete Files
# This script removes files that are definitively obsolete

set -e

echo "🧹 SnapDog2 Focused Cleanup - Obviously Obsolete Files"
echo "====================================================="
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

echo "Removing definitively obsolete files..."
echo "======================================"

# 1. Remove the deprecated behavior files (confirmed superseded)
remove_file "SnapDog2/Server/Behaviors/LoggingCommandBehavior.cs" \
    "Superseded by SharedLoggingCommandBehavior"

remove_file "SnapDog2/Server/Behaviors/LoggingQueryBehavior.cs" \
    "Superseded by SharedLoggingQueryBehavior"

# 2. Remove the old consolidated command files (confirmed superseded)
remove_file "SnapDog2/Server/Features/Zones/Commands/ZoneCommands.cs" \
    "Superseded by individual command files"

remove_file "SnapDog2/Server/Features/Clients/Commands/ClientVolumeCommands.cs" \
    "Superseded by individual command files"

remove_file "SnapDog2/Server/Features/Clients/Commands/ClientConfigCommands.cs" \
    "Superseded by individual command files"

# 3. Remove old Controllers that are superseded by API controllers
remove_file "SnapDog2/Controllers/ZoneController.cs" \
    "Superseded by Api/Controllers/V1/ZonesController.cs"

remove_file "SnapDog2/Controllers/ClientController.cs" \
    "Superseded by Api/Controllers/V1/ClientsController.cs"

remove_file "SnapDog2/Controllers/PlaylistController.cs" \
    "Superseded by Api/Controllers/V1/PlaylistsController.cs"

remove_file "SnapDog2/Controllers/GlobalStatusController.cs" \
    "Superseded by Api/Controllers/V1/SystemController.cs"

remove_file "SnapDog2/Controllers/TestController.cs" \
    "Test controller - not needed in production"

# 4. Remove unused notification files
remove_file "SnapDog2/Server/Notifications/PlaybackNotifications.cs" \
    "Notifications not published anywhere"

remove_file "SnapDog2/Server/Notifications/SnapcastNotifications.cs" \
    "Notifications not published anywhere"

# 5. Remove consolidated query files (superseded by individual files)
remove_file "SnapDog2/Server/Features/Zones/Queries/ZoneQueries.cs" \
    "Superseded by individual query files"

remove_file "SnapDog2/Server/Features/Clients/Queries/ClientQueries.cs" \
    "Superseded by individual query files"

remove_file "SnapDog2/Server/Features/Playlists/Queries/PlaylistQueries.cs" \
    "Superseded by individual query files"

# 6. Remove unused validator files (FluentValidation handles this)
remove_file "SnapDog2/Server/Features/Zones/Validators/ZoneCommandValidators.cs" \
    "Validators unused - FluentValidation handles validation"

remove_file "SnapDog2/Server/Features/Clients/Validators/ClientCommandValidators.cs" \
    "Validators unused - FluentValidation handles validation"

remove_file "SnapDog2/Server/Features/Snapcast/Validators/SetSnapcastClientVolumeCommandValidator.cs" \
    "Validator unused - FluentValidation handles validation"

# 7. Remove unused model files
remove_file "SnapDog2/Core/Models/GitVersionInfo.cs" \
    "Model not used anywhere"

remove_file "SnapDog2/Core/Models/MqttModels.cs" \
    "Models not used anywhere"

remove_file "SnapDog2/Core/Models/SnapcastServerStatus.cs" \
    "Models not used anywhere"

# 8. Remove unused configuration files
remove_file "SnapDog2/Core/Configuration/SnapcastServerConfig.cs" \
    "Configuration not used"

remove_file "SnapDog2/Core/Configuration/ResilienceConfig.cs" \
    "Configuration not used"

remove_file "SnapDog2/Core/Configuration/TelemetryConfig.cs" \
    "Configuration not used"

# 9. Remove unused shared notifications
remove_file "SnapDog2/Server/Features/Shared/Notifications/VersionInfoChangedNotification.cs" \
    "Notification not published anywhere"

remove_file "SnapDog2/Server/Features/Shared/Notifications/SystemStatusChangedNotification.cs" \
    "Notification not published anywhere"

remove_file "SnapDog2/Server/Features/Shared/Notifications/ServerStatsChangedNotification.cs" \
    "Notification not published anywhere"

remove_file "SnapDog2/Server/Features/Shared/Notifications/SystemErrorNotification.cs" \
    "Notification not published anywhere"

# 10. Remove unused middleware components
remove_file "SnapDog2/Logging/StackTraceSuppressionEnricher.cs" \
    "Enricher not used in logging configuration"

echo "🔧 Running build to verify cleanup..."
if dotnet build --verbosity quiet; then
    echo "✅ Build successful - cleanup verified!"
    echo ""
    echo "📊 Cleanup Summary:"
    echo "   Files removed: $REMOVED_COUNT"
    echo ""
    echo "🎉 Focused cleanup completed successfully!"
else
    echo "❌ Build failed after cleanup!"
    echo "   This indicates some removed files are still needed."
    echo "   Please review the build errors."
    exit 1
fi
