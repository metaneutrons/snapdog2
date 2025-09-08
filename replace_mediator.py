#!/usr/bin/env python3

import re

# Read the file
with open('SnapDog2/Domain/Services/ZoneManager.cs', 'r') as f:
    content = f.read()

# Replace patterns
patterns = [
    # Pattern 1: using var scope = ... var mediator = ...
    (r'using var scope = this\._serviceScopeFactory\.CreateScope\(\);\s*var mediator = scope\.ServiceProvider\.GetRequiredService<IMediator>\(\);', 
     'var mediator = new StubMediator();'),
    
    # Pattern 2: var scope = ... var mediator = ...
    (r'var scope = this\._serviceScopeFactory\.CreateScope\(\);\s*var mediator = scope\.ServiceProvider\.GetRequiredService<IMediator>\(\);', 
     'var mediator = new StubMediator();'),
     
    # Pattern 3: standalone mediator resolution
    (r'var mediator = scope\.ServiceProvider\.GetRequiredService<IMediator>\(\);', 
     'var mediator = new StubMediator();'),
]

for pattern, replacement in patterns:
    content = re.sub(pattern, replacement, content, flags=re.MULTILINE)

# Write back
with open('SnapDog2/Domain/Services/ZoneManager.cs', 'w') as f:
    f.write(content)

print("Replaced all mediator usages")
