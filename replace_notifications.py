#!/usr/bin/env python3

import re

# Read the file
with open('SnapDog2/Domain/Services/ZoneManager.cs', 'r') as f:
    content = f.read()

# Replace notification patterns
patterns = [
    # Pattern 1: ZoneTrackPlayingStatusChangedNotification
    (r'var mediator = new StubMediator\(\);\s*var playingStatusNotification = new ZoneTrackPlayingStatusChangedNotification\s*\{\s*ZoneIndex = this\._zoneIndex,\s*IsPlaying = ([^}]+)\s*\};\s*await mediator\.PublishAsync\(playingStatusNotification\)\.ConfigureAwait\(false\);', 
     r'var playingStatusNotification = new { ZoneIndex = this._zoneIndex, IsPlaying = \1 };\n                                await this._hubContext.Clients.All.SendAsync("ZonePlaybackChanged", playingStatusNotification).ConfigureAwait(false);'),
    
    # Pattern 2: ZoneTrackProgressChangedNotification
    (r'var mediator = new StubMediator\(\);\s*var progressNotification = new ZoneTrackProgressChangedNotification\s*\{([^}]+)\};\s*await mediator\.PublishAsync\(progressNotification\)\.ConfigureAwait\(false\);', 
     r'var progressNotification = new {\1};\n                                await this._hubContext.Clients.All.SendAsync("ZoneProgressChanged", progressNotification).ConfigureAwait(false);'),
     
    # Pattern 3: Other notifications
    (r'var mediator = new StubMediator\(\);\s*await mediator\.PublishAsync\(([^)]+)\)\.ConfigureAwait\(false\);', 
     r'await this._hubContext.Clients.All.SendAsync("ZoneNotification", \1).ConfigureAwait(false);'),
]

for pattern, replacement in patterns:
    content = re.sub(pattern, replacement, content, flags=re.MULTILINE | re.DOTALL)

# Write back
with open('SnapDog2/Domain/Services/ZoneManager.cs', 'w') as f:
    f.write(content)

print("Replaced all notification usages")
