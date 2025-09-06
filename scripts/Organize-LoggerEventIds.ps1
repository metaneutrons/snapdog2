#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Organizes LoggerMessage EventIds across SnapDog2 project files into systematic category-based ranges.

.DESCRIPTION
    This script processes all C# files containing LoggerMessage attributes and reorganizes their EventIds
    into systematic category-based ranges to eliminate duplicates and ensure maintainable organization.
    
    The script uses a four-phase approach:
    - Phase 1: Analyze all files and their EventId usage patterns
    - Phase 2: Calculate optimal ranges for each functional category
    - Phase 3: Generate new EventId mappings with validation
    - Phase 4: Apply changes with duplicate prevention
    
    Categories and ranges:
    - Domain: 10000-10999 (Domain services like ClientManager, ZoneManager)
    - Application: 11000-11999 (Application services like StartupService)
    - Server: 12000-12999 (Server handlers for Snapcast, Zones, Clients)
    - Api: 13000-13999 (API controllers, hubs, middleware)
    - Infrastructure: 14000-14999 (Infrastructure services, storage, hosting)
    - Integration: 15000-15999 (Integration services for MQTT, KNX, Subsonic)
    - Audio: 16000-16999 (Audio processing, media players, metadata)
    - Notifications: 17000-17999 (Notification handlers and publishers)
    - Metrics: 18000-18999 (Performance metrics and monitoring)

.PARAMETER Path
    Optional. The path to the SnapDog2 project directory.
    Defaults to "../SnapDog2" relative to the script location.

.PARAMETER WhatIf
    Optional. If specified, shows what changes would be made without actually modifying files.
    Useful for previewing the reorganization before applying changes.

.PARAMETER DebugMode
    Optional. If specified, enables detailed debug output for troubleshooting.

.EXAMPLE
    .\Organize-LoggerEventIds.ps1
    Organizes EventIds in the default SnapDog2 directory

.EXAMPLE
    .\Organize-LoggerEventIds.ps1 -Path "C:\Source\SnapDog2" -WhatIf
    Shows what changes would be made without modifying files

.EXAMPLE
    .\Organize-LoggerEventIds.ps1 -DebugMode
    Runs with detailed debug output for troubleshooting

.NOTES
    This script ensures no duplicate EventIds are created and validates all changes before applying them.
    Each category has a dedicated 1000-number range with automatic gap management between files.
#>

param(
    [Parameter(Mandatory = $false)]
    [string]$Path = "",

    [Parameter(Mandatory = $false)]
    [switch]$WhatIf = $false,

    [Parameter(Mandatory = $false)]
    [switch]$DebugMode = $false
)

# Function to write colored output to console
function Write-ColoredOutput {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Message,
        
        [Parameter(Mandatory = $false)]
        [ValidateSet("Blue", "Green", "Yellow", "Red", "White")]
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

# Function to categorize files by functional area based on path patterns
function Get-FileCategory {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RelativePath
    )
    
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

# Function to get category base EventId ranges
function Get-CategoryBase {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Category
    )
    
    $categoryBases = @{
        "Domain" = 10000        # 10000-10999: Domain Services
        "Application" = 11000   # 11000-11999: Application Services
        "Server" = 12000        # 12000-12999: Server Handlers
        "Api" = 13000          # 13000-13999: API Layer
        "Infrastructure" = 14000 # 14000-14999: Infrastructure Services
        "Integration" = 15000   # 15000-15999: Integration Services
        "Audio" = 16000        # 16000-16999: Audio Services
        "Notifications" = 17000 # 17000-17999: Notifications & Messaging
        "Metrics" = 18000      # 18000-18999: Metrics & Monitoring
    }
    
    return $categoryBases[$Category]
}

# Function to extract EventIds from file content
function Get-EventIdsFromContent {
    param(
        [string[]]$Content
    )
    
    $eventIds = @()
    
    if (-not $Content) {
        return $eventIds
    }
    
    for ($i = 0; $i -lt $Content.Length; $i++) {
        $line = $Content[$i]
        
        # Find EventId = number patterns
        if ($line -match 'EventId\s*=\s*(\d+)') {
            $eventIds += [PSCustomObject]@{
                LineNumber = $i
                EventId = [int]$matches[1]
                OriginalLine = $line
            }
        }
    }
    
    return $eventIds
}

# Main script execution
try {
    Write-ColoredOutput "🔧 LoggerMessage EventId Organizer" "Blue"
    Write-ColoredOutput "===================================" "Blue"
    
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
    Write-ColoredOutput "`n📁 Scanning for LoggerMessage files..." "Blue"
    
    $csFiles = Get-ChildItem -Path $snapDogPath -Filter "*.cs" -Recurse | 
        Where-Object { 
            $_.FullName -notmatch "(obj|Tests)" -and
            (Get-Content $_.FullName -Raw -ErrorAction SilentlyContinue) -match "LoggerMessage"
        }
    
    Write-ColoredOutput "📊 Found $($csFiles.Count) files with LoggerMessage" "Yellow"
    
    if ($csFiles.Count -eq 0) {
        Write-ColoredOutput "❌ No LoggerMessage files found" "Red"
        exit 1
    }
    
    # Phase 1: Analyze all files and their EventId usage patterns
    Write-ColoredOutput "`n📋 Phase 1: Analyzing EventId usage..." "Blue"
    
    $fileAnalysis = @()
    $categoryFiles = @{}
    
    foreach ($file in $csFiles) {
        $relativePath = $file.FullName.Replace($snapDogPath, "").TrimStart('\/')
        $category = Get-FileCategory -RelativePath $relativePath
        $content = Get-Content -Path $file.FullName -Encoding UTF8 -ErrorAction SilentlyContinue
        $eventIds = Get-EventIdsFromContent -Content $content
        
        if ($eventIds.Count -gt 0) {
            $fileInfo = [PSCustomObject]@{
                File = $file
                RelativePath = $relativePath
                Category = $category
                EventIdCount = $eventIds.Count
                EventIds = $eventIds
                Content = $content
            }
            
            $fileAnalysis += $fileInfo
            
            if (-not $categoryFiles.ContainsKey($category)) {
                $categoryFiles[$category] = @()
            }
            $categoryFiles[$category] += $fileInfo
        }
    }
    
    # Phase 2: Calculate optimal ranges for each functional category
    Write-ColoredOutput "`n📋 Phase 2: Calculating optimal ranges..." "Blue"
    
    $allAllocations = @{}
    $nextAvailableId = @{}
    
    foreach ($category in ($categoryFiles.Keys | Sort-Object)) {
        $categoryBase = Get-CategoryBase -Category $category
        $nextAvailableId[$category] = $categoryBase
        
        Write-ColoredOutput "  $category (base: $categoryBase):" "White"
        
        foreach ($fileInfo in $categoryFiles[$category]) {
            $startId = $nextAvailableId[$category]
            $endId = $startId + $fileInfo.EventIdCount - 1
            
            # Add 10-number buffer between files to prevent future conflicts
            $nextAvailableId[$category] = $endId + 11
            
            $allAllocations[$fileInfo.File.FullName] = @{
                StartId = $startId
                EndId = $endId
                FileInfo = $fileInfo
            }
            
            Write-ColoredOutput "    $($fileInfo.RelativePath): $startId-$endId ($($fileInfo.EventIdCount) EventIds)" "White"
        }
        
        $totalUsed = $nextAvailableId[$category] - $categoryBase
        $remaining = 1000 - $totalUsed
        Write-ColoredOutput "    Total used: $totalUsed/1000 ($remaining remaining)" "Yellow"
        
        if ($totalUsed -gt 1000) {
            Write-ColoredOutput "    ❌ OVERFLOW: Category exceeds 1000-number range!" "Red"
            exit 1
        }
    }
    
    # Phase 3: Generate new EventId mappings with validation
    Write-ColoredOutput "`n📋 Phase 3: Generating EventId mappings..." "Blue"
    
    $totalChanges = 0
    $allNewEventIds = @()
    
    foreach ($allocation in $allAllocations.Values) {
        $fileInfo = $allocation.FileInfo
        $startId = $allocation.StartId
        
        for ($i = 0; $i -lt $fileInfo.EventIds.Count; $i++) {
            $oldId = $fileInfo.EventIds[$i].EventId
            $newId = $startId + $i
            
            if ($oldId -ne $newId) {
                $totalChanges++
            }
            
            $allNewEventIds += $newId
        }
    }
    
    # Phase 4: Verify no duplicates in new allocation
    $duplicateCheck = $allNewEventIds | Group-Object | Where-Object { $_.Count -gt 1 }
    if ($duplicateCheck) {
        Write-ColoredOutput "❌ CRITICAL: New allocation would create duplicates!" "Red"
        foreach ($dup in $duplicateCheck) {
            Write-ColoredOutput "  EventId $($dup.Name) appears $($dup.Count) times" "Red"
        }
        exit 1
    }
    
    Write-ColoredOutput "✅ Verification passed: $($allNewEventIds.Count) unique EventIds, $totalChanges changes needed" "Green"
    
    if ($WhatIf) {
        Write-ColoredOutput "`n🔍 WhatIf: Would reorganize $totalChanges EventIds across $($fileAnalysis.Count) files" "Yellow"
        Write-ColoredOutput "No duplicates would be created." "Green"
    } else {
        Write-ColoredOutput "`n✏️ Applying changes..." "Blue"
        
        foreach ($allocation in $allAllocations.Values) {
            $fileInfo = $allocation.FileInfo
            $startId = $allocation.StartId
            $updatedContent = $fileInfo.Content.Clone()
            
            for ($i = 0; $i -lt $fileInfo.EventIds.Count; $i++) {
                $eventIdInfo = $fileInfo.EventIds[$i]
                $oldId = $eventIdInfo.EventId
                $newId = $startId + $i
                
                if ($oldId -ne $newId) {
                    $oldLine = $updatedContent[$eventIdInfo.LineNumber]
                    $newLine = $oldLine -replace "(EventId\s*=\s*)$oldId\b", ('$1' + $newId)
                    $updatedContent[$eventIdInfo.LineNumber] = $newLine
                }
            }
            
            $updatedContent | Set-Content -Path $fileInfo.File.FullName -Encoding UTF8
        }
        
        Write-ColoredOutput "✅ Successfully reorganized $totalChanges EventIds with no duplicates!" "Green"
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
