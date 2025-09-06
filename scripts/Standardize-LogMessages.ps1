#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Standardizes LoggerMessage formats according to SnapDog2 logging schema.

.DESCRIPTION
    This script processes all LoggerMessage attributes and standardizes their message formats:
    - Removes redundant component prefixes (SignalR:, Snapcast, MQTT:, etc.)
    - Standardizes arrows to Unicode → 
    - Converts boolean formatting to lowercase
    - Applies consistent parameter formatting
    - Removes decorative emojis, keeps semantic ones

.PARAMETER Path
    Optional. The path to the SnapDog2 project directory.

.PARAMETER WhatIf
    Optional. Shows what changes would be made without modifying files.
#>

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

function Standardize-LogMessage {
    param([string]$Message)
    
    $standardized = $Message
    
    # Remove redundant component prefixes
    $standardized = $standardized -replace '^(SignalR: |Snapcast |MQTT: |KNX: )', ''
    
    # Standardize arrows
    $standardized = $standardized -replace ' -> ', ' → '
    $standardized = $standardized -replace ' to ', ' → '
    
    # Standardize boolean formatting
    $standardized = $standardized -replace '\(Muted: ', '(muted: '
    $standardized = $standardized -replace '\(Enabled: ', '(enabled: '
    $standardized = $standardized -replace ' Enabled\}', ' enabled}'
    $standardized = $standardized -replace ' Muted\}', ' muted}'
    
    # Remove decorative emojis but keep semantic ones
    $decorativeEmojis = @('🔊', '📡', '📤', '🔔')
    foreach ($emoji in $decorativeEmojis) {
        $standardized = $standardized -replace "$emoji ", ''
    }
    
    # Keep semantic emojis: ⚡ ⚠️ ✅ ❌ 🔄 💾 🎵 etc.
    
    return $standardized
}

try {
    Write-ColoredOutput "🔄 LoggerMessage Format Standardizer" "Blue"
    Write-ColoredOutput "====================================" "Blue"
    
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
                $standardizedMessage = Standardize-LogMessage -Message $originalMessage
                
                if ($originalMessage -ne $standardizedMessage) {
                    $updatedContent[$i] = $line -replace [regex]::Escape($originalMessage), $standardizedMessage
                    $fileChanged = $true
                    $totalChanges++
                    
                    if ($WhatIf) {
                        $relativePath = $file.FullName.Replace($snapDogPath, "").TrimStart('\/')
                        Write-ColoredOutput "📄 $relativePath" "Green"
                        Write-ColoredOutput "  - $originalMessage" "Red"
                        Write-ColoredOutput "  + $standardizedMessage" "Green"
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
        Write-ColoredOutput "`n🔍 WhatIf: Would standardize $totalChanges messages across $($changedFiles.Count) files" "Yellow"
    } else {
        Write-ColoredOutput "`n✅ Standardized $totalChanges messages across $($changedFiles.Count) files" "Green"
    }
    
    Write-ColoredOutput "`n🎉 Message format standardization completed!" "Green"
}
catch {
    Write-ColoredOutput "❌ Error: $($_.Exception.Message)" "Red"
    exit 1
}
