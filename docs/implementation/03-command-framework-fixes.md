# Command Framework Documentation Fixes

## Overview

This document summarizes the critical fixes applied to the Command Framework documentation (Section 15) to resolve ambiguities and broken references.

## Issues Fixed

### 1. **Cortex.Mediator Interface References**
- **Issue**: Referenced incorrect `IRequest<r>` interface
- **Fix**: Updated to correct `ICommand<Result<T>>` and `IQuery<Result<T>>` interfaces
- **Section Reference**: Fixed from "Section 6" to "Section 16" for Cortex.Mediator details

### 2. **Missing Core Model References**
- **Issue**: Referenced "Section 10.2.1" for Core.Models definitions that didn't exist
- **Fix**: Updated to reference actual `SnapDog2.Core.Models` namespace
- **Status**: ✅ Models already exist: `ErrorDetails`, `VersionDetails`, `ServerStats`, `SystemStatus`

### 3. **Missing Configuration References**
- **Issue**: Referenced "Section 10.2.2" for Radio Configuration that didn't exist
- **Fix**: Updated to reference general "Section 10" for configuration details

### 4. **Broken JSON State Object References**
- **Issue**: Referenced non-existent "Section 9.5.1" and "9.5.2" for JSON examples
- **Fix**: Updated to reference correct "Section 13.5.1" and "13.5.2" within same document

### 5. **Missing Payload List Reference**
- **Issue**: Referenced non-existent "Section 9.3.2.3" for payload examples
- **Fix**: Updated to reference correct "Section 13.3.2.3" within same document

### 6. **Missing DPT Mapping Reference**
- **Issue**: Referenced non-existent "Appendix 20.3" for KNX DPT mapping
- **Fix**: Updated to reference "Section 20" for dependencies with note about standard KNX conventions

## New Models Created

To support the command framework, the following models were created in `SnapDog2.Core.Models`:

### TrackInfo.cs
- Represents detailed track information
- Includes index, title, artist, album, duration, position, cover art, source
- Supports both Subsonic tracks and radio streams

### PlaylistInfo.cs
- Represents detailed playlist information
- Includes ID, name, index, track count, duration, description, cover art, source
- Supports both Subsonic playlists and radio station collections

### ZoneState.cs
- Represents complete zone state for JSON serialization
- Includes playback state, volume, mute, repeat/shuffle modes
- Contains current playlist and track information
- Lists assigned client IDs

### ClientState.cs
- Represents complete client state for JSON serialization
- Includes connection status, volume, mute, latency
- Contains Snapcast client details and host information
- Tracks zone assignment

## Alignment Status

### ✅ **Resolved Issues**
- Core model definitions exist and are properly referenced
- Cortex.Mediator interfaces match actual implementation
- All internal section references are now correct
- JSON state objects have proper model backing

### ✅ **Well-Aligned Aspects**
- Command/Status separation is clear and consistent
- MQTT topic structure is well-defined
- KNX Group Address mapping is comprehensive
- 1-based indexing convention is consistently applied
- Environment variable naming follows clear patterns

### 📋 **Implementation Ready**
The command framework documentation is now ready for implementation with:
- Clear mapping between conceptual commands and C# interfaces
- Proper model definitions for all referenced objects
- Consistent cross-references within the document
- Well-defined protocol adapter specifications

## Next Steps

1. **Implement Global Status Commands** - Start with the four global status types
2. **Create Zone Command Handlers** - Implement playback, track, and playlist management
3. **Build Client Command Handlers** - Implement volume, mute, and configuration commands
4. **Develop Protocol Adapters** - Create MQTT and KNX translation layers

The command framework is now structurally sound and ready for systematic implementation.
