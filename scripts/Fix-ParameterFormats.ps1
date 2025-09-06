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

function Fix-SafeParameterFormats {
    param([string]$Message)
    
    $fixed = $Message
    
    # Only fix percentage formatting (safe changes)
    $fixed = $fixed -replace '\{Progress\}%', '{Progress:P1}'
    $fixed = $fixed -replace '\{CpuUsage:F2\}%', '{CpuUsage:P1}'
    
    return $fixed
}

try {
    Write-ColoredOutput "🔧 Safe Parameter Format Fixes" "Blue"
    Write-ColoredOutput "==============================" "Blue"
    
    if ([string]::IsNullOrEmpty($Path)) {
        $scriptDir = Split-Path $MyInvocation.MyCommand.Path -Parent
        $Path = Join-Path $scriptDir ".." "SnapDog2"
    }
    
    $snapDogPath = Resolve-Path $Path -ErrorAction Stop
    Write-ColoredOutput "Processing: $snapDogPath" "Yellow"
    
    if ($WhatIf) {
        Write-ColoredOutput "🔍 WhatIf mode - no files will be modified" "Yellow"
    }
    
    $csFiles = Get-ChildItem -Path $snapDogPath -Filter "*.cs" -Recurse | 
        Where-Object { 
            $_.FullName -notmatch "(obj|Tests)" -and
            (Get-Content $_.FullName -Raw -ErrorAction SilentlyContinue) -match "LoggerMessage"
        }
    
    Write-ColoredOutput "📊 Found $($csFiles.Count) files with LoggerMessage" "Yellow"
    
    $totalChanges = 0
    $changedFiles = @()
    
    foreach ($file in $csFiles) {
        $content = Get-Content -Path $file.FullName -Encoding UTF8
        $updatedContent = $content.Clone()
        $fileChanged = $false
        
        for ($i = 0; $i -lt $content.Length; $i++) {
            $line = $content[$i]
            
            if ($line -match 'Message = "([^"]*)"') {
                $originalMessage = $matches[1]
                $fixedMessage = Fix-SafeParameterFormats -Message $originalMessage
                
                if ($originalMessage -ne $fixedMessage) {
                    $updatedContent[$i] = $line -replace [regex]::Escape($originalMessage), $fixedMessage
                    $fileChanged = $true
                    $totalChanges++
                    
                    if ($WhatIf) {
                        $relativePath = $file.FullName.Replace($snapDogPath, "").TrimStart('\/')
                        Write-ColoredOutput "📄 $relativePath" "Green"
                        Write-ColoredOutput "  - $originalMessage" "Red"
                        Write-ColoredOutput "  + $fixedMessage" "Green"
                    }
                }
            }
        }
        
        if ($fileChanged) {
            $changedFiles += $file
            if (-not $WhatIf) {
                $updatedContent | Set-Content -Path $file.FullName -Encoding UTF8
            }
        }
    }
    
    if ($WhatIf) {
        Write-ColoredOutput "`n🔍 WhatIf: Would fix $totalChanges parameter formats across $($changedFiles.Count) files" "Yellow"
    } else {
        Write-ColoredOutput "`n✅ Fixed $totalChanges parameter formats across $($changedFiles.Count) files" "Green"
    }
    
    Write-ColoredOutput "`n🎉 Safe parameter format fixes completed!" "Green"
}
catch {
    Write-ColoredOutput "❌ Error: $($_.Exception.Message)" "Red"
    exit 1
}
