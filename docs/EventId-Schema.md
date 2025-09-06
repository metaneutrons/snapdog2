# EventId Schema Documentation

## Overview
This document defines the systematic EventId allocation schema for SnapDog2 LoggerMessage patterns. EventIds are organized by functional categories to ensure uniqueness and maintainability.

## Category Ranges

| Category | Range | Description | Examples |
|----------|-------|-------------|----------|
| **Domain** | 10000-10999 | Domain Services | ClientManager, ZoneManager, PlaylistManager |
| **Application** | 11000-11999 | Application Services | StartupService, BusinessMetricsCollectionService |
| **Server** | 12000-12999 | Server Handlers | Snapcast handlers, Zone handlers, Client handlers |
| **Api** | 13000-13999 | API Layer | Controllers, Hubs, Middleware |
| **Infrastructure** | 14000-14999 | Infrastructure Services | Background services, Storage, Hosting |
| **Integration** | 15000-15999 | Integration Services | MQTT, KNX, Subsonic, Snapcast integrations |
| **Audio** | 16000-16999 | Audio Services | MediaPlayer, MetadataManager, AudioProcessing |
| **Notifications** | 17000-17999 | Notifications & Messaging | NotificationHandlers, Publishers |
| **Metrics** | 18000-18999 | Metrics & Monitoring | Performance behaviors, ApplicationMetrics |

## File Allocation
- Each file gets a 50-number block within its category
- Files are assigned sequentially: first file gets base+0, second gets base+50, etc.
- Example: First Domain service file uses 10000-10049, second uses 10050-10099

## Message Format Standards

### Template
```
[Action/Event]: [Details with parameters]
```

### Examples
```csharp
// ✅ Good - Clear, consistent, informative
"Client connected: {ClientIndex} ({ClientName})"
"Zone volume changed: {ZoneIndex} → {Volume}% (muted: {IsMuted})"
"Service starting: interval {IntervalMs}ms"

// ❌ Avoid - Redundant prefixes, inconsistent formatting
"SignalR: Zone {ZoneIndex} volume changed to {Volume}"
"🔊 Volume changed to {Volume}%" // Emoji adds no information
```

### Parameter Formatting
- **Units:** Always include units: `{DurationMs}ms`, `{Volume}%`, `{SizeBytes} bytes`
- **Booleans:** Use lowercase: `muted: {IsMuted}`, `enabled: {IsEnabled}`
- **Arrows:** Use Unicode arrow `→` for state changes
- **Percentages:** Use `{Progress:P1}` for percentage formatting

### UTF Icons (Selective Use)
Only use when adding semantic value:
```csharp
// ✅ Status indicators (add semantic meaning)
"Service starting ⚡ interval {IntervalMs}ms"
"Connection lost ⚠️ reason: {Reason}"
"Operation completed ✅ duration: {DurationMs}ms"

// ❌ Decorative only (avoid)
"🔊 Volume changed" // Adds no information
"📡 Client joined"  // Redundant with message
```

## LoggerMessage Attribute Format
```csharp
[LoggerMessage(
    EventId = 12001,
    Level = LogLevel.Information,
    Message = "Client connected: {ClientIndex} ({ClientName})"
)]
private partial void LogClientConnected(int clientIndex, string clientName);
```

## Log Level Guidelines
- **Trace:** Detailed execution flow (rarely used)
- **Debug:** Development/troubleshooting info
- **Information:** Normal operation events (connections, state changes)
- **Warning:** Recoverable issues, degraded performance
- **Error:** Failures requiring attention
- **Critical:** System-threatening failures

## Maintenance
- Use `scripts/Organize-EventIds.ps1` to reorganize EventIds automatically
- Run with `-WhatIf` to preview changes before applying
- EventIds are automatically assigned based on file location and category
