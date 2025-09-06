#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Systematically organizes EventIds in SnapDog2 LoggerMessage attributes by functional categories.

.DESCRIPTION
    This script processes all C# files containing LoggerMessage attributes and reassigns EventIds
    systematically by functional category. Each category gets a dedicated range, and each file
    within a category gets a unique 100-number block. This eliminates duplicates by design
    and ensures consistent, maintainable EventId organization.

    Categories and ranges:
    - Core: 1000-1999 (foundational components)
    - Audio: 2000-2999 (audio processing, media players)
    - KNX: 3000-3999 (KNX building automation)
    - MQTT: 4000-4999 (MQTT messaging)
    - Web: 5000-5999 (API controllers, middleware)
    - Infrastructure: 6000-7999 (services, integrations)
    - Performance: 8000-8999 (metrics, monitoring)
    - Notifications: 10000-15999 (handlers, publishers)

.PARAMETER Path
    Optional. The path to the SnapDog2 project directory.
    Defaults to "../SnapDog2" relative to the script location.

.PARAMETER WhatIf
    Optional. If specified, shows what changes would be made without actually modifying files.

.PARAMETER DebugMode
    Optional. If specified, enables detailed debug output for troubleshooting.

.EXAMPLE
    .\Organize-EventIds.ps1
    Organizes EventIds in the default SnapDog2 directory

.EXAMPLE
    .\Organize-EventIds.ps1 -Path "C:\Source\SnapDog2" -WhatIf
    Shows what changes would be made without modifying files

.EXAMPLE
    .\Organize-EventIds.ps1 -DebugMode
    Runs with detailed debug output
#>

param(
    [Parameter(Mandatory = $false)]
    [string]$Path = "",

    [Parameter(Mandatory = $false)]
    [switch]$WhatIf = $false,

    [Parameter(Mandatory = $false)]
    [switch]$DebugMode = $false
)

# Function to write colored output
function Write-ColoredOutput {
    param(
        [string]$Message,
        [string]$Color = "White"
    )
    
    $colorMap = @{
        "Blue" = "Cyan"
        "Green" = "Green"
        "Yellow" = "Yellow"
        "Red" = "Red"
        "White" = "White"
    }
    
    Write-Host $Message -ForegroundColor $colorMap[$Color]
}

# Function to categorize files by functional area
function Get-FileCategory {
    param([string]$RelativePath)
    
    # More specific patterns first - match actual project structure
    if ($RelativePath -match "Domain[/\\]Services") { 
        return "Domain" 
    }
    elseif ($RelativePath -match "Application[/\\]Services") { 
        return "Application" 
    }
    elseif ($RelativePath -match "Server[/\\]") { 
        return "Server" 
    }
    elseif ($RelativePath -match "Api[/\\]") { 
        return "Api" 
    }
    elseif ($RelativePath -match "Infrastructure[/\\]Services") { 
        return "Infrastructure" 
    }
    elseif ($RelativePath -match "Infrastructure[/\\]Integrations") { 
        return "Integration" 
    }
    elseif ($RelativePath -match "(Audio|Media|LibVLC|Player)") { 
        return "Audio" 
    }
    elseif ($RelativePath -match "(Metrics|Performance)") { 
        return "Metrics" 
    }
    elseif ($RelativePath -match "(Notification|Publisher)") { 
        return "Notifications" 
    }
    else { 
        return "Infrastructure" # Default fallback
    }
}

# Function to get category base EventId
function Get-CategoryBase {
    param([string]$Category)
    
    $categoryBases = @{
        "Domain" = 10000        # 10000-10999: Domain Services (ClientManager, ZoneManager, etc.)
        "Application" = 11000   # 11000-11999: Application Services (StartupService, BusinessMetrics, etc.)
        "Server" = 12000        # 12000-12999: Server Handlers (Snapcast, Zones, Clients, etc.)
        "Api" = 13000          # 13000-13999: API Layer (Controllers, Hubs, Middleware)
        "Infrastructure" = 14000 # 14000-14999: Infrastructure Services (Background services, Storage)
        "Integration" = 15000   # 15000-15999: Integration Services (MQTT, KNX, Subsonic)
        "Audio" = 16000        # 16000-16999: Audio Services (MediaPlayer, MetadataManager)
        "Notifications" = 17000 # 17000-17999: Notifications & Messaging
        "Metrics" = 18000      # 18000-18999: Metrics & Monitoring
    }
    
    return $categoryBases[$Category]
}

# Function to extract EventIds from file content
function Get-EventIdsFromContent {
    param([string[]]$Content)
    
    $eventIds = @()
    $inCodeBlock = $false
    
    for ($i = 0; $i -lt $Content.Length; $i++) {
        $line = $Content[$i]
        
        # Track code block boundaries
        if ($line -match '^```') {
            $inCodeBlock = -not $inCodeBlock
            continue
        }
        
        # Skip lines inside code blocks
        if ($inCodeBlock) {
            continue
        }
        
        # Find EventId = number patterns or corrupted $number patterns
        if ($line -match 'EventId\s*=\s*(\d+)') {
            $eventIds += [PSCustomObject]@{
                LineNumber = $i
                EventId = [int]$matches[1]
                OriginalLine = $line
            }
        }
        elseif ($line -match '\$(\d+),') {
            $eventIds += [PSCustomObject]@{
                LineNumber = $i
                EventId = [int]$matches[1]
                OriginalLine = $line
            }
        }
    }
    
    return $eventIds
}

# Function to update EventIds in content
function Update-EventIdsInContent {
    param(
        [string[]]$Content,
        [array]$EventIds,
        [hashtable]$EventIdMapping
    )
    
    $updatedContent = $Content.Clone()
    
    foreach ($eventIdInfo in $EventIds) {
        $oldId = $eventIdInfo.EventId
        $newId = $EventIdMapping[$oldId]
        
        if ($newId -and $newId -ne $oldId) {
            $oldLine = $updatedContent[$eventIdInfo.LineNumber]
            
            # Handle both EventId = format and corrupted $ format
            if ($oldLine -match 'EventId\s*=\s*\d+') {
                $newLine = $oldLine -replace "(EventId\s*=\s*)$oldId\b", ('$1' + $newId)
            }
            elseif ($oldLine -match '\$\d+') {
                $newLine = $oldLine -replace "\`$$oldId\b", "EventId = $newId"
            }
            else {
                $newLine = $oldLine
            }
            
            # Only update if replacement actually changed the line
            if ($newLine -ne $oldLine) {
                $updatedContent[$eventIdInfo.LineNumber] = $newLine
            }
        }
    }
    
    return $updatedContent
}

# Function to verify no duplicates exist
function Test-EventIdUniqueness {
    param([array]$Files, [string]$SnapDogPath)
    
    $allEventIds = @{}
    $duplicates = @()
    
    foreach ($file in $Files) {
        $content = Get-Content -Path $file.FullName -Encoding UTF8 -ErrorAction SilentlyContinue
        if (-not $content) { continue }
        
        $eventIds = Get-EventIdsFromContent -Content $content
        $relativePath = $file.FullName.Replace($SnapDogPath, "").TrimStart('\/')
        
        foreach ($eventIdInfo in $eventIds) {
            $eventId = $eventIdInfo.EventId
            $key = "$eventId-$($eventIdInfo.LineNumber)"
            
            if ($allEventIds.ContainsKey($eventId)) {
                $existing = $allEventIds[$eventId]
                if ($existing -ne $relativePath) {
                    $duplicates += [PSCustomObject]@{
                        EventId = $eventId
                        Files = @($existing, $relativePath)
                    }
                }
            }
            else {
                $allEventIds[$eventId] = $relativePath
            }
        }
    }
    
    return @{
        TotalEventIds = $allEventIds.Count
        AllEventIds = $allEventIds
        Duplicates = $duplicates
    }
}

# Main script execution
try {
    Write-ColoredOutput "🔄 Systematic EventId Organizer" "Blue"
    Write-ColoredOutput "================================" "Blue"
    
    # Determine SnapDog2 path
    if ([string]::IsNullOrEmpty($Path)) {
        $scriptDir = Split-Path $MyInvocation.MyCommand.Path -Parent
        $Path = Join-Path $scriptDir ".." "SnapDog2"
    }
    
    $snapDogPath = Resolve-Path $Path -ErrorAction Stop
    Write-ColoredOutput "Processing: $snapDogPath" "Yellow"
    
    if ($WhatIf) {
        Write-ColoredOutput "🔍 WhatIf mode - no files will be modified" "Yellow"
    }
    
    # Find all C# files with LoggerMessage
    Write-ColoredOutput "📁 Scanning for LoggerMessage files..." "Yellow"
    
    $csFiles = Get-ChildItem -Path $snapDogPath -Filter "*.cs" -Recurse | 
        Where-Object { 
            $_.FullName -notmatch "(obj|Tests)" -and
            (Get-Content $_.FullName -Raw -ErrorAction SilentlyContinue) -match "LoggerMessage"
        } |
        Sort-Object FullName
    
    Write-ColoredOutput "📊 Found $($csFiles.Count) files with LoggerMessage" "Yellow"
    
    if ($csFiles.Count -eq 0) {
        Write-ColoredOutput "❌ No LoggerMessage files found" "Red"
        exit 1
    }
    
    # Categorize files and assign file indices
    Write-ColoredOutput "`n📋 Analyzing file categories..." "Blue"
    
    $fileAssignments = @{}
    $categoryCounters = @{}
    
    foreach ($file in $csFiles) {
        $relativePath = $file.FullName.Replace($snapDogPath, "").TrimStart('\/')
        $category = Get-FileCategory -RelativePath $relativePath
        
        if (-not $categoryCounters.ContainsKey($category)) {
            $categoryCounters[$category] = 0
        }
        
        $fileIndex = $categoryCounters[$category]
        $categoryCounters[$category]++
        
        $categoryBase = Get-CategoryBase -Category $category
        $fileBase = $categoryBase + ($fileIndex * 100)  # 100 EventIds per file to avoid overlaps
        
        $fileAssignments[$file.FullName] = @{
            Category = $category
            FileIndex = $fileIndex
            FileBase = $fileBase
            RelativePath = $relativePath
        }
    }
    
    # Display category summary
    Write-ColoredOutput "`n📋 File assignments:" "Blue"
    foreach ($category in ($categoryCounters.Keys | Sort-Object)) {
        $count = $categoryCounters[$category]
        $base = Get-CategoryBase -Category $category
        $maxRange = $base + 999  # Full 1000-number range per category
        Write-ColoredOutput "  $category`: $count files ($base-$maxRange)" "White"
    }
    
    # Process each file and calculate changes
    Write-ColoredOutput "`n🔄 Calculating EventId changes..." "Blue"
    
    $totalChanges = 0
    $fileChanges = @{}
    
    foreach ($file in $csFiles) {
        $assignment = $fileAssignments[$file.FullName]
        $content = Get-Content -Path $file.FullName -Encoding UTF8
        $eventIds = Get-EventIdsFromContent -Content $content
        
        if ($eventIds.Count -eq 0) { continue }
        
        # Calculate new EventIds for this file
        $eventIdMapping = @{}
        $fileBase = $assignment.FileBase
        
        for ($i = 0; $i -lt $eventIds.Count; $i++) {
            $oldId = $eventIds[$i].EventId
            $newId = $fileBase + $i  # Sequential within file
            $eventIdMapping[$oldId] = $newId
        }
        
        # Check if any changes are needed
        $changesNeeded = $false
        foreach ($mapping in $eventIdMapping.GetEnumerator()) {
            if ($mapping.Key -ne $mapping.Value) {
                $changesNeeded = $true
                break
            }
        }
        
        if ($changesNeeded) {
            $fileChanges[$file.FullName] = @{
                Assignment = $assignment
                EventIds = $eventIds
                Mapping = $eventIdMapping
                Content = $content
            }
            
            $changeCount = ($eventIdMapping.GetEnumerator() | Where-Object { $_.Key -ne $_.Value }).Count
            $totalChanges += $changeCount
        }
    }
    
    # Display changes or apply them
    if ($fileChanges.Count -eq 0) {
        Write-ColoredOutput "`n✅ All EventIds already properly organized!" "Green"
    }
    else {
        Write-ColoredOutput "`n📄 Files requiring changes:" "Blue"
        
        foreach ($fileChange in $fileChanges.GetEnumerator()) {
            $filePath = $fileChange.Key
            $change = $fileChange.Value
            $assignment = $change.Assignment
            
            Write-ColoredOutput "📄 $($assignment.RelativePath) ($($assignment.Category)): $($change.EventIds.Count) EventIds" "Green"
            
            if ($DebugMode) {
                foreach ($mapping in $change.Mapping.GetEnumerator()) {
                    if ($mapping.Key -ne $mapping.Value) {
                        Write-ColoredOutput "   $($mapping.Key) → $($mapping.Value)" "White"
                    }
                }
            }
        }
        
        if ($WhatIf) {
            Write-ColoredOutput "`n🔍 WhatIf: Would reorganize $totalChanges EventIds across $($fileChanges.Count) files" "Yellow"
        }
        else {
            Write-ColoredOutput "`n✏️ Applying changes..." "Blue"
            
            foreach ($fileChange in $fileChanges.GetEnumerator()) {
                $filePath = $fileChange.Key
                $change = $fileChange.Value
                
                $updatedContent = Update-EventIdsInContent -Content $change.Content -EventIds $change.EventIds -EventIdMapping $change.Mapping
                $updatedContent | Set-Content -Path $filePath -Encoding UTF8
            }
            
            Write-ColoredOutput "`n✅ Reorganized $totalChanges EventIds across $($fileChanges.Count) files" "Green"
        }
    }
    
    # Verify uniqueness
    if (-not $WhatIf) {
        Write-ColoredOutput "`n🔍 Verifying uniqueness..." "Blue"
        
        $verification = Test-EventIdUniqueness -Files $csFiles -SnapDogPath $snapDogPath
        
        if ($verification.Duplicates.Count -gt 0) {
            Write-ColoredOutput "❌ Found $($verification.Duplicates.Count) duplicates!" "Red"
            foreach ($duplicate in $verification.Duplicates) {
                Write-ColoredOutput "  EventId $($duplicate.EventId): $($duplicate.Files -join ', ')" "Red"
            }
            exit 1
        }
        else {
            Write-ColoredOutput "✅ All $($verification.TotalEventIds) EventIds are unique!" "Green"
        }
    }
    
    Write-ColoredOutput "`n🎉 EventId organization completed successfully!" "Green"
}
catch {
    Write-ColoredOutput "❌ Error: $($_.Exception.Message)" "Red"
    if ($DebugMode) {
        Write-ColoredOutput "Stack trace: $($_.ScriptStackTrace)" "Red"
    }
    exit 1
}
