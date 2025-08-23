#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Manages LoggerMessage methods for consistent formatting and systematic EventId organization.

.DESCRIPTION
    This script helps maintain consistency across LoggerMessage methods in the SnapDog2 codebase by:
    - Standardizing formatting to preferred multi-line style
    - Managing EventId ranges systematically
    - Detecting conflicts and gaps
    - Validating parameter naming consistency
    - Generating reports on logging patterns

.PARAMETER Action
    The action to perform: Format, Analyze, Validate, or Report

.PARAMETER Path
    Path to analyze (defaults to SnapDog2 directory)

.PARAMETER DryRun
    Show what would be changed without making modifications

.EXAMPLE
    ./Manage-LoggerMessages.ps1 -Action Format -DryRun
    Shows formatting changes that would be made

.EXAMPLE
    ./Manage-LoggerMessages.ps1 -Action Analyze
    Analyzes current EventId usage and suggests ranges
#>

param(
    [Parameter(Mandatory)]
    [ValidateSet("Format", "Analyze", "Validate", "Report", "FixEventIds")]
    [string]$Action,
    
    [string]$Path = "SnapDog2",
    
    [switch]$DryRun
)

# Load configuration from JSON
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$configPath = Join-Path $scriptDir "logger-config.json"

if (-not (Test-Path $configPath)) {
    Write-Error "Configuration file not found: $configPath"
    exit 1
}

$config = Get-Content $configPath | ConvertFrom-Json
$EventIdRanges = @{}

# Convert JSON config to PowerShell hashtable
foreach ($category in $config.eventIdRanges.PSObject.Properties) {
    $EventIdRanges[$category.Name] = @{
        Start = $category.Value.start
        End = $category.Value.end
        Description = $category.Value.description
    }
}

class LoggerMessageInfo {
    [string]$FilePath
    [string]$ClassName
    [int]$EventId
    [string]$Level
    [string]$Message
    [string]$MethodName
    [string[]]$Parameters
    [int]$LineNumber
    [string]$OriginalText
    [string]$Category
}

function Get-LoggerMessages {
    param([string]$SearchPath)
    
    $loggerMessages = @()
    $csFiles = Get-ChildItem -Path $SearchPath -Filter "*.cs" -Recurse
    
    foreach ($file in $csFiles) {
        $content = Get-Content $file.FullName -Raw
        $lines = Get-Content $file.FullName
        
        # Find LoggerMessage attributes with their methods
        $pattern = '(?s)\[LoggerMessage\s*\((.*?)\)\]\s*private\s+partial\s+void\s+(\w+)\s*\((.*?)\)\s*;'
        $matches = [regex]::Matches($content, $pattern)
        
        foreach ($match in $matches) {
            $attributeContent = $match.Groups[1].Value
            $methodName = $match.Groups[2].Value
            $parameters = $match.Groups[3].Value
            
            # Find line number
            $lineNumber = ($content.Substring(0, $match.Index) -split "`n").Count
            
            # Parse attribute parameters
            $eventId = if ($attributeContent -match 'EventId\s*=\s*(\d+)') { [int]$matches[0].Groups[1].Value } else { 0 }
            $level = if ($attributeContent -match 'Level\s*=\s*([^,\)]+)') { $matches[0].Groups[1].Value.Trim() } else { "" }
            $message = if ($attributeContent -match 'Message\s*=\s*"([^"]*)"') { $matches[0].Groups[1].Value } else { "" }
            
            # Parse parameters
            $paramList = @()
            if ($parameters.Trim()) {
                $paramList = ($parameters -split ',') | ForEach-Object { $_.Trim() -replace '^\w+\s+', '' -replace '\s+\w+$', '' }
            }
            
            # Determine category based on file path or class name
            $category = Get-LoggingCategory -FilePath $file.FullName -ClassName (Get-ClassName $file.FullName)
            
            $loggerMessages += [LoggerMessageInfo]@{
                FilePath = $file.FullName
                ClassName = Get-ClassName $file.FullName
                EventId = $eventId
                Level = $level
                Message = $message
                MethodName = $methodName
                Parameters = $paramList
                LineNumber = $lineNumber
                OriginalText = $match.Value
                Category = $category
            }
        }
    }
    
    return $loggerMessages
}

function Get-ClassName {
    param([string]$FilePath)
    
    $content = Get-Content $FilePath -Raw
    if ($content -match 'class\s+(\w+)') {
        return $matches[1]
    }
    return [System.IO.Path]::GetFileNameWithoutExtension($FilePath)
}

function Get-LoggingCategory {
    param([string]$FilePath, [string]$ClassName)
    
    $relativePath = $FilePath -replace [regex]::Escape((Get-Location).Path), ""
    
    switch -Regex ($relativePath) {
        "Audio|Snapcast" { return "Audio" }
        "KNX|Knx" { return "KNX" }
        "MQTT|Mqtt" { return "MQTT" }
        "Web|Http|Api|Controller" { return "Web" }
        "Infrastructure|Host|Extension" { return "Infrastructure" }
        "Performance|Metrics" { return "Performance" }
        "Test" { return "Testing" }
        default { return "Core" }
    }
}

function Format-LoggerMessages {
    param([string]$SearchPath, [switch]$DryRun)
    
    Write-Host "🎨 Formatting LoggerMessage methods to preferred style..." -ForegroundColor Cyan
    
    $csFiles = Get-ChildItem -Path $SearchPath -Filter "*.cs" -Recurse
    $changedFiles = 0
    
    foreach ($file in $csFiles) {
        $content = Get-Content $file.FullName -Raw
        $originalContent = $content
        
        # Pattern to match LoggerMessage attributes (single or multi-line)
        $pattern = '\[LoggerMessage\([^\]]+\)\]\s*\r?\n\s*private\s+partial\s+void\s+\w+\s*\([^)]*\)\s*;'
        
        $content = [regex]::Replace($content, $pattern, {
            param($match)
            
            $text = $match.Value
            
            # Extract components - handle both positional and named parameters
            if ($text -match '\[LoggerMessage\(([^\]]+)\)\]\s*\r?\n\s*private\s+partial\s+void\s+(\w+)\s*\(([^)]*)\)\s*;') {
                $attributeParams = $matches[1]
                $methodName = $matches[2]
                $methodParams = $matches[3]
                
                # Parse attribute parameters (handle both formats)
                $eventId = ""
                $level = ""
                $message = ""
                
                if ($attributeParams -match 'EventId\s*=\s*(\d+)') {
                    # Named parameter format
                    $eventId = $matches[1]
                    if ($attributeParams -match 'Level\s*=\s*([^,\)]+)') { 
                        $level = $matches[1].Trim()
                    }
                    if ($attributeParams -match 'Message\s*=\s*"([^"]*)"') { 
                        $message = $matches[1] 
                    }
                } else {
                    # Positional parameter format: EventId, Level, Message
                    # Need to carefully parse the message which may contain escaped quotes
                    $parts = @()
                    $current = ""
                    $inQuotes = $false
                    $escaped = $false
                    
                    for ($i = 0; $i -lt $attributeParams.Length; $i++) {
                        $char = $attributeParams[$i]
                        
                        if ($escaped) {
                            $current += $char
                            $escaped = $false
                        } elseif ($char -eq '\') {
                            $current += $char
                            $escaped = $true
                        } elseif ($char -eq '"') {
                            $current += $char
                            $inQuotes = -not $inQuotes
                        } elseif ($char -eq ',' -and -not $inQuotes) {
                            $parts += $current.Trim()
                            $current = ""
                        } else {
                            $current += $char
                        }
                    }
                    if ($current.Trim()) {
                        $parts += $current.Trim()
                    }
                    
                    if ($parts.Count -ge 3) {
                        $eventId = $parts[0]
                        $level = $parts[1]
                        $message = $parts[2] -replace '^"', '' -replace '"$', ''
                    }
                }
                
                # Ensure full namespace for LogLevel
                if ($level -and $level -notmatch "Microsoft\.Extensions\.Logging\.LogLevel") {
                    $level = $level -replace "LogLevel\.", "Microsoft.Extensions.Logging.LogLevel."
                }
                
                # Use defaults if parsing failed
                if (-not $eventId) { $eventId = "1000" }
                if (-not $level) { $level = "Microsoft.Extensions.Logging.LogLevel.Information" }
                if (-not $message) { $message = "Message not found" }
                
                # Format in preferred style
                return @"
    [LoggerMessage(
        EventId = $eventId,
        Level = $level,
        Message = "$message"
    )]
    private partial void $methodName($methodParams);
"@
            }
            
            return $match.Value
        })
        
        if ($content -ne $originalContent) {
            if ($DryRun) {
                Write-Host "  📝 Would format: $($file.Name)" -ForegroundColor Yellow
            } else {
                Set-Content -Path $file.FullName -Value $content -NoNewline
                Write-Host "  ✅ Formatted: $($file.Name)" -ForegroundColor Green
            }
            $changedFiles++
        }
    }
    
    Write-Host "📊 Summary: $changedFiles files would be changed" -ForegroundColor Magenta
}

function Analyze-EventIds {
    param([string]$SearchPath)
    
    Write-Host "🔍 Analyzing EventId usage and ranges..." -ForegroundColor Cyan
    
    $loggerMessages = Get-LoggerMessages -SearchPath $SearchPath
    
    # Group by category
    $byCategory = $loggerMessages | Group-Object Category
    
    Write-Host "`n📋 EventId Analysis by Category:" -ForegroundColor White
    Write-Host "=" * 50
    
    foreach ($categoryGroup in $byCategory) {
        $category = $categoryGroup.Name
        $messages = $categoryGroup.Group
        $range = $EventIdRanges[$category]
        
        Write-Host "`n🏷️  Category: $category" -ForegroundColor Yellow
        Write-Host "   Range: $($range.Start)-$($range.End) ($($range.Description))" -ForegroundColor Gray
        
        $eventIds = $messages | ForEach-Object { $_.EventId } | Sort-Object
        $usedIds = $eventIds | Where-Object { $_ -gt 0 }
        
        if ($usedIds.Count -gt 0) {
            Write-Host "   Used IDs: $($usedIds -join ', ')" -ForegroundColor Green
            
            # Check for conflicts (IDs outside range)
            $conflicts = $usedIds | Where-Object { $_ -lt $range.Start -or $_ -gt $range.End }
            if ($conflicts.Count -gt 0) {
                Write-Host "   ⚠️  CONFLICTS: $($conflicts -join ', ') (outside range)" -ForegroundColor Red
            }
            
            # Suggest next available ID
            $nextId = $range.Start
            while ($usedIds -contains $nextId -and $nextId -le $range.End) {
                $nextId++
            }
            if ($nextId -le $range.End) {
                Write-Host "   💡 Next available: $nextId" -ForegroundColor Cyan
            } else {
                Write-Host "   ⚠️  Range full! Consider expanding or cleanup." -ForegroundColor Red
            }
        } else {
            Write-Host "   📝 No EventIds used yet. Start with: $($range.Start)" -ForegroundColor Blue
        }
    }
    
    # Find duplicates across all categories
    $duplicates = $loggerMessages | Group-Object EventId | Where-Object { $_.Count -gt 1 -and $_.Name -ne "0" }
    if ($duplicates.Count -gt 0) {
        Write-Host "`n⚠️  DUPLICATE EventIds Found:" -ForegroundColor Red
        foreach ($dup in $duplicates) {
            Write-Host "   EventId $($dup.Name) used in:" -ForegroundColor Red
            foreach ($msg in $dup.Group) {
                Write-Host "     - $($msg.ClassName).$($msg.MethodName) ($($msg.Category))" -ForegroundColor Yellow
            }
        }
    }
}

function Validate-LoggerMessages {
    param([string]$SearchPath)
    
    Write-Host "✅ Validating LoggerMessage consistency..." -ForegroundColor Cyan
    
    $loggerMessages = Get-LoggerMessages -SearchPath $SearchPath
    $issues = @()
    
    foreach ($msg in $loggerMessages) {
        # Check parameter naming consistency
        $messageParams = [regex]::Matches($msg.Message, '\{(\w+)\}') | ForEach-Object { $_.Groups[1].Value }
        $methodParams = $msg.Parameters | ForEach-Object { ($_ -split '\s+')[-1] }
        
        foreach ($msgParam in $messageParams) {
            if ($msgParam -notin $methodParams) {
                $issues += "❌ $($msg.ClassName).$($msg.MethodName): Message parameter '{$msgParam}' not found in method parameters"
            }
        }
        
        # Check EventId is positive
        if ($msg.EventId -le 0) {
            $issues += "❌ $($msg.ClassName).$($msg.MethodName): EventId must be positive (current: $($msg.EventId))"
        }
        
        # Check Level format
        if ($msg.Level -and $msg.Level -notmatch "Microsoft\.Extensions\.Logging\.LogLevel") {
            $issues += "⚠️  $($msg.ClassName).$($msg.MethodName): Consider using full LogLevel namespace"
        }
    }
    
    if ($issues.Count -eq 0) {
        Write-Host "✅ All LoggerMessage methods are valid!" -ForegroundColor Green
    } else {
        Write-Host "`n🚨 Validation Issues Found:" -ForegroundColor Red
        foreach ($issue in $issues) {
            Write-Host "  $issue" -ForegroundColor Yellow
        }
    }
    
    return $issues.Count -eq 0
}

function Fix-EventIds {
    param([string]$SearchPath, [switch]$DryRun)
    
    Write-Host "🔧 Fixing EventId conflicts and organizing ranges..." -ForegroundColor Cyan
    
    $loggerMessages = Get-LoggerMessages -SearchPath $SearchPath
    $changes = @()
    
    # Group by category and assign sequential IDs within ranges
    $byCategory = $loggerMessages | Group-Object Category
    
    foreach ($categoryGroup in $byCategory) {
        $category = $categoryGroup.Name
        $messages = $categoryGroup.Group | Sort-Object ClassName, MethodName
        $range = $EventIdRanges[$category]
        
        $nextId = $range.Start
        
        foreach ($msg in $messages) {
            if ($msg.EventId -ne $nextId) {
                $changes += @{
                    File = $msg.FilePath
                    OldEventId = $msg.EventId
                    NewEventId = $nextId
                    Method = "$($msg.ClassName).$($msg.MethodName)"
                    Category = $category
                }
            }
            $nextId++
        }
    }
    
    if ($changes.Count -eq 0) {
        Write-Host "✅ No EventId changes needed!" -ForegroundColor Green
        return
    }
    
    Write-Host "`n📋 Proposed EventId Changes:" -ForegroundColor White
    foreach ($change in $changes) {
        Write-Host "  $($change.Method): $($change.OldEventId) → $($change.NewEventId) [$($change.Category)]" -ForegroundColor Yellow
    }
    
    if (-not $DryRun) {
        $confirm = Read-Host "`nApply these changes? (y/N)"
        if ($confirm -eq 'y' -or $confirm -eq 'Y') {
            # Apply changes
            foreach ($change in $changes) {
                $content = Get-Content $change.File -Raw
                $content = $content -replace "EventId = $($change.OldEventId)", "EventId = $($change.NewEventId)"
                Set-Content -Path $change.File -Value $content -NoNewline
            }
            Write-Host "✅ EventId changes applied!" -ForegroundColor Green
        }
    }
}

function Show-Report {
    param([string]$SearchPath)
    
    Write-Host "📊 LoggerMessage Report" -ForegroundColor Cyan
    Write-Host "=" * 50
    
    $loggerMessages = Get-LoggerMessages -SearchPath $SearchPath
    
    Write-Host "`n📈 Statistics:" -ForegroundColor White
    Write-Host "  Total LoggerMessage methods: $($loggerMessages.Count)"
    Write-Host "  Classes with logging: $(($loggerMessages | Group-Object ClassName).Count)"
    Write-Host "  Categories in use: $(($loggerMessages | Group-Object Category).Count)"
    
    Write-Host "`n🏷️  By Category:" -ForegroundColor White
    $loggerMessages | Group-Object Category | Sort-Object Name | ForEach-Object {
        Write-Host "  $($_.Name): $($_.Count) methods"
    }
    
    Write-Host "`n📝 By Log Level:" -ForegroundColor White
    $loggerMessages | Group-Object { $_.Level -replace '.*\.', '' } | Sort-Object Name | ForEach-Object {
        Write-Host "  $($_.Name): $($_.Count) methods"
    }
}

# Main execution
switch ($Action) {
    "Format" { Format-LoggerMessages -SearchPath $Path -DryRun:$DryRun }
    "Analyze" { Analyze-EventIds -SearchPath $Path }
    "Validate" { Validate-LoggerMessages -SearchPath $Path }
    "Report" { Show-Report -SearchPath $Path }
    "FixEventIds" { Fix-EventIds -SearchPath $Path -DryRun:$DryRun }
}

Write-Host "`n🎯 Script completed!" -ForegroundColor Green
