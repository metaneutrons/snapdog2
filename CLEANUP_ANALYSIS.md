# SnapDog2 Code Cleanup Analysis Report
==================================================

## 🗑️ Deprecated Files (Safe to Remove)

**File**: `SnapDog2/Server/Behaviors/LoggingQueryBehavior.cs`
**Reason**: Superseded by new implementation
**Replacement**: SharedLoggingQueryBehavior.cs

**File**: `SnapDog2/Server/Behaviors/LoggingCommandBehavior.cs`
**Reason**: Superseded by new implementation
**Replacement**: SharedLoggingCommandBehavior.cs

**File**: `SnapDog2/Server/Features/Zones/Commands/ZoneCommands.cs`
**Reason**: Superseded by new implementation
**Replacement**: Individual files in Commands/Playback/, Commands/Volume/, etc.

**File**: `SnapDog2/Server/Features/Clients/Commands/ClientVolumeCommands.cs`
**Reason**: Superseded by new implementation
**Replacement**: Individual files in Commands/Volume/

**File**: `SnapDog2/Server/Features/Clients/Commands/ClientConfigCommands.cs`
**Reason**: Superseded by new implementation
**Replacement**: Individual files in Commands/Config/

## 🔍 Potentially Unused Files (Review Required)

**File**: `SnapDog2.Tests/Api/HealthControllerTests.cs`
**Class**: HealthControllerTests
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2.Tests/Api/ApiConfigurationTests.cs`
**Class**: ApiConfigurationTests
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2.Tests/Api/ApiConfigurationTests.cs`
**Class**: ApiEnabledConfigurationTests
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2.Tests/Api/TestCollections.cs`
**Class**: ApiEnabledCollection
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2.Tests/Api/TestCollections.cs`
**Class**: ApiDisabledCollection
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2.Tests/Unit/Services/StartupInformationServiceTests.cs`
**Class**: StartupInformationServiceTests
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2.Tests/Unit/Infrastructure/Services/KnxServiceTests.cs`
**Class**: KnxServiceTests
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2.Tests/Unit/Worker/DI/KnxServiceConfigurationTests.cs`
**Class**: KnxServiceConfigurationTests
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2.Tests/Unit/Core/Configuration/ZoneConfigTests.cs`
**Class**: ZoneConfigTests
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2.Tests/Unit/Core/Configuration/RadioStationConfigTests.cs`
**Class**: RadioStationConfigTests
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2.Tests/Unit/Core/Configuration/ClientConfigTests.cs`
**Class**: ClientConfigTests
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2.Tests/Unit/Core/Configuration/SnapDogConfigurationTests.cs`
**Class**: SnapDogConfigurationTests
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2.Tests/Unit/Core/Models/ResultTests.cs`
**Class**: ResultTests
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2.Tests/Unit/Core/Models/ResultTests.cs`
**Class**: ResultTTests
**Reason**: Class appears to be unreferenced

**File**: `KnxMonitor/Models/TuiModels.cs`
**Class**: ConnectionStatusModel
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Middleware/GlobalExceptionHandlingMiddleware.cs`
**Class**: ErrorResponse
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Middleware/GlobalExceptionHandlingMiddleware.cs`
**Class**: SupportInfo
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Extensions/WebHostExtensions.cs`
**Class**: WebHostConfigurationException
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Controllers/ZoneController.cs`
**Class**: VolumeRequest
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Controllers/ZoneController.cs`
**Class**: RepeatRequest
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Controllers/ZoneController.cs`
**Class**: ShuffleRequest
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Controllers/ClientController.cs`
**Class**: ClientVolumeRequest
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Controllers/ClientController.cs`
**Class**: ClientMuteRequest
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Controllers/ClientController.cs`
**Class**: ClientLatencyRequest
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Controllers/ClientController.cs`
**Class**: ZoneAssignmentRequest
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Logging/StackTraceSuppressionEnricher.cs`
**Class**: StackTraceSuppressionEnricher
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Api/Models/RequestDtos.cs`
**Class**: PlayRequest
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Api/Models/RequestDtos.cs`
**Class**: SetTrackRequest
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Api/Models/RequestDtos.cs`
**Class**: SetPlaylistRequest
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Api/Models/RequestDtos.cs`
**Class**: VolumeSetRequest
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Api/Models/RequestDtos.cs`
**Class**: MuteSetRequest
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Api/Models/RequestDtos.cs`
**Class**: ModeSetRequest
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Api/Models/RequestDtos.cs`
**Class**: StepRequest
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Api/Models/RequestDtos.cs`
**Class**: LatencySetRequest
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Api/Models/RequestDtos.cs`
**Class**: AssignZoneRequest
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Api/Models/RequestDtos.cs`
**Class**: RenameRequest
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Api/Models/ApiResponse.cs`
**Class**: ApiError
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Api/Models/ResponseDtos.cs`
**Class**: NameResponse
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Api/Models/ResponseDtos.cs`
**Class**: PaginatedResponse
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Api/Models/ResponseDtos.cs`
**Class**: PaginationMetadata
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Api/Controllers/V1/SnapcastController.cs`
**Class**: SetVolumeRequest
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Api/Controllers/V1/SnapcastController.cs`
**Class**: SetMuteRequest
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Server/Behaviors/LoggingQueryBehavior.cs`
**Class**: LoggingQueryBehavior
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Server/Behaviors/LoggingCommandBehavior.cs`
**Class**: LoggingCommandBehavior
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Server/Notifications/PlaybackNotifications.cs`
**Class**: TrackEndedNotification
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Server/Notifications/PlaybackNotifications.cs`
**Class**: PlaybackErrorNotification
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Server/Notifications/PlaybackNotifications.cs`
**Class**: AudioFormatChangedNotification
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Server/Notifications/PlaybackNotifications.cs`
**Class**: StreamingBufferUnderrunNotification
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Server/Notifications/PlaybackNotifications.cs`
**Class**: StreamingConnectionLostNotification
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Server/Notifications/SnapcastNotifications.cs`
**Class**: SnapcastOperationFailedNotification
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Server/Features/Snapcast/Validators/SetSnapcastClientVolumeCommandValidator.cs`
**Class**: SetSnapcastClientVolumeCommandValidator
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Server/Features/Shared/Notifications/VersionInfoChangedNotification.cs`
**Class**: VersionInfoChangedNotification
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Server/Features/Shared/Notifications/SystemStatusChangedNotification.cs`
**Class**: SystemStatusChangedNotification
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Server/Features/Shared/Notifications/ServerStatsChangedNotification.cs`
**Class**: ServerStatsChangedNotification
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Server/Features/Shared/Notifications/SystemErrorNotification.cs`
**Class**: SystemErrorNotification
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Server/Features/Zones/Queries/ZoneQueries.cs`
**Class**: GetZoneStateQuery
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Server/Features/Zones/Queries/ZoneQueries.cs`
**Class**: GetZonePlaybackStateQuery
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Server/Features/Zones/Queries/ZoneQueries.cs`
**Class**: GetZoneVolumeQuery
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Server/Features/Zones/Queries/ZoneQueries.cs`
**Class**: GetZoneTrackInfoQuery
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Server/Features/Zones/Queries/ZoneQueries.cs`
**Class**: GetZonePlaylistInfoQuery
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Server/Features/Zones/Validators/ZoneCommandValidators.cs`
**Class**: SetZoneVolumeCommandValidator
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Server/Features/Zones/Validators/ZoneCommandValidators.cs`
**Class**: VolumeUpCommandValidator
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Server/Features/Zones/Validators/ZoneCommandValidators.cs`
**Class**: VolumeDownCommandValidator
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Server/Features/Zones/Validators/ZoneCommandValidators.cs`
**Class**: SetTrackCommandValidator
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Server/Features/Zones/Validators/ZoneCommandValidators.cs`
**Class**: SetPlaylistCommandValidator
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Server/Features/Zones/Validators/ZoneCommandValidators.cs`
**Class**: PlayCommandValidator
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Server/Features/Zones/Validators/ZoneCommandValidators.cs`
**Class**: PauseCommandValidator
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Server/Features/Zones/Validators/ZoneCommandValidators.cs`
**Class**: StopCommandValidator
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Server/Features/Zones/Validators/ZoneCommandValidators.cs`
**Class**: SetZoneMuteCommandValidator
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Server/Features/Zones/Validators/ZoneCommandValidators.cs`
**Class**: ToggleZoneMuteCommandValidator
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Server/Features/Zones/Validators/ZoneCommandValidators.cs`
**Class**: NextTrackCommandValidator
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Server/Features/Zones/Validators/ZoneCommandValidators.cs`
**Class**: PreviousTrackCommandValidator
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Server/Features/Zones/Validators/ZoneCommandValidators.cs`
**Class**: NextPlaylistCommandValidator
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Server/Features/Zones/Validators/ZoneCommandValidators.cs`
**Class**: PreviousPlaylistCommandValidator
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Server/Features/Zones/Validators/ZoneCommandValidators.cs`
**Class**: SetTrackRepeatCommandValidator
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Server/Features/Zones/Validators/ZoneCommandValidators.cs`
**Class**: ToggleTrackRepeatCommandValidator
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Server/Features/Zones/Validators/ZoneCommandValidators.cs`
**Class**: SetPlaylistShuffleCommandValidator
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Server/Features/Zones/Validators/ZoneCommandValidators.cs`
**Class**: TogglePlaylistShuffleCommandValidator
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Server/Features/Zones/Validators/ZoneCommandValidators.cs`
**Class**: SetPlaylistRepeatCommandValidator
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Server/Features/Zones/Validators/ZoneCommandValidators.cs`
**Class**: TogglePlaylistRepeatCommandValidator
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Server/Features/Playlists/Queries/PlaylistQueries.cs`
**Class**: GetPlaylistQuery
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Server/Features/Playlists/Queries/PlaylistQueries.cs`
**Class**: GetStreamUrlQuery
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Server/Features/Clients/Queries/ClientQueries.cs`
**Class**: GetClientQuery
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Server/Features/Clients/Queries/ClientQueries.cs`
**Class**: GetClientsByZoneQuery
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Server/Features/Clients/Validators/ClientCommandValidators.cs`
**Class**: SetClientVolumeCommandValidator
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Server/Features/Clients/Validators/ClientCommandValidators.cs`
**Class**: SetClientMuteCommandValidator
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Server/Features/Clients/Validators/ClientCommandValidators.cs`
**Class**: ToggleClientMuteCommandValidator
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Server/Features/Clients/Validators/ClientCommandValidators.cs`
**Class**: SetClientLatencyCommandValidator
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Server/Features/Clients/Validators/ClientCommandValidators.cs`
**Class**: AssignClientToZoneCommandValidator
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Core/Configuration/ResilienceConfig.cs`
**Class**: ResilienceConfig
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Core/Configuration/ResilienceConfig.cs`
**Class**: PolicyConfig
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Core/Configuration/SnapcastServerConfig.cs`
**Class**: SnapcastServerConfig
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Core/Configuration/TelemetryConfig.cs`
**Class**: OtlpConfig
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Core/Configuration/TelemetryConfig.cs`
**Class**: PrometheusConfig
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Core/Configuration/TelemetryConfig.cs`
**Class**: SeqConfig
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Core/Models/GitVersionInfo.cs`
**Class**: GitVersionInfo
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Core/Models/MqttModels.cs`
**Class**: ZoneControlTopics
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Core/Models/MqttModels.cs`
**Class**: ZoneStatusTopics
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Core/Models/MqttModels.cs`
**Class**: ClientControlTopics
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Core/Models/MqttModels.cs`
**Class**: ClientStatusTopics
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Core/Models/SnapcastServerStatus.cs`
**Class**: SnapcastServerInfo
**Reason**: Class appears to be unreferenced

**File**: `SnapDog2/Core/Models/SnapcastServerStatus.cs`
**Class**: SnapcastClientHost
**Reason**: Class appears to be unreferenced

## 📊 Summary

- **Deprecated files**: 5
- **Potentially unused files**: 102
- **Temporary files**: 0
- **Total files analyzed**: 189
