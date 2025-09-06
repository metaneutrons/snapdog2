#!/usr/bin/env pwsh

param(
    [Parameter(Mandatory = $false)]
    [string]$Path = "",
    [Parameter(Mandatory = $false)]
    [switch]$WhatIf = $false
)

function Write-ColoredOutput {
    param([string]$Message, [string]$Color = "White")
    $colorMap = @{ "Blue" = "Cyan"; "Green" = "Green"; "Yellow" = "Yellow"; "Red" = "Red"; "White" = "White" }
    Write-Host $Message -ForegroundColor $colorMap[$Color]
}

function Get-FileCategory {
    param([string]$RelativePath)
    
    if ($RelativePath -match "Domain[/\\]Services") { return "Domain" }
    elseif ($RelativePath -match "Application[/\\]Services") { return "Application" }
    elseif ($RelativePath -match "Server[/\\]") { return "Server" }
    elseif ($RelativePath -match "Api[/\\]") { return "Api" }
    elseif ($RelativePath -match "Infrastructure[/\\]Services") { return "Infrastructure" }
    elseif ($RelativePath -match "Infrastructure[/\\]Integrations") { return "Integration" }
    elseif ($RelativePath -match "(Audio|Media|LibVLC|Player)") { return "Audio" }
    elseif ($RelativePath -match "(Metrics|Performance)") { return "Metrics" }
    elseif ($RelativePath -match "(Notification|Publisher)") { return "Notifications" }
    else { return "Infrastructure" }
}

function Get-CategoryBase {
    param([string]$Category)
    
    $categoryBases = @{
        "Domain" = 10000
        "Application" = 11000
        "Server" = 12000
        "Api" = 13000
        "Infrastructure" = 14000
        "Integration" = 15000
        "Audio" = 16000
        "Notifications" = 17000
        "Metrics" = 18000
    }
    
    return $categoryBases[$Category]
}

function Get-EventIdsFromContent {
    param([string[]]$Content)
    
    $eventIds = @()
    
    for ($i = 0; $i -lt $Content.Length; $i++) {
        $line = $Content[$i]
        
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

try {
    Write-ColoredOutput "🔧 Proper EventId Organizer" "Blue"
    Write-ColoredOutput "===========================" "Blue"
    
    if ([string]::IsNullOrEmpty($Path)) {
        $scriptDir = Split-Path $MyInvocation.MyCommand.Path -Parent
        $Path = Join-Path $scriptDir ".." "SnapDog2"
    }
    
    $snapDogPath = Resolve-Path $Path -ErrorAction Stop
    Write-ColoredOutput "Processing: $snapDogPath" "Yellow"
    
    # Find all C# files with LoggerMessage
    $csFiles = Get-ChildItem -Path $snapDogPath -Filter "*.cs" -Recurse | 
        Where-Object { 
            $_.FullName -notmatch "(obj|Tests)" -and
            (Get-Content $_.FullName -Raw -ErrorAction SilentlyContinue) -match "LoggerMessage"
        }
    
    Write-ColoredOutput "📊 Found $($csFiles.Count) files with LoggerMessage" "Yellow"
    
    # Phase 1: Analyze all files and their EventId counts
    Write-ColoredOutput "`n📋 Phase 1: Analyzing EventId usage..." "Blue"
    
    $fileAnalysis = @()
    $categoryFiles = @{}
    
    foreach ($file in $csFiles) {
        $relativePath = $file.FullName.Replace($snapDogPath, "").TrimStart('\/')
        $category = Get-FileCategory -RelativePath $relativePath
        $content = Get-Content -Path $file.FullName -Encoding UTF8
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
    
    # Phase 2: Calculate proper ranges for each category
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
        
        if ($totalUsed > 1000) {
            Write-ColoredOutput "    ❌ OVERFLOW: Category exceeds 1000-number range!" "Red"
            exit 1
        }
    }
    
    # Phase 3: Generate new EventId mappings
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
    exit 1
}
