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
    ./ManageLoggerMessages.ps1 -Action Format -DryRun
    Shows formatting changes that would be made

.EXAMPLE
    ./ManageLoggerMessages.ps1 -Action Analyze
    Analyzes current EventId usage and suggests ranges
#>

param(
    [Parameter(Mandatory)]
    [ValidateSet("Format", "Analyze", "Validate", "Report", "FixEventIds", "Repair")]
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
$CategoryMappings = @{}

# Convert JSON config to PowerShell hashtables
foreach ($category in $config.eventIdRanges.PSObject.Properties) {
    $EventIdRanges[$category.Name] = @{
        Start = $category.Value.start
        End = $category.Value.end
        Description = $category.Value.description
    }
}

# Load category mappings if they exist
if ($config.categoryMappings) {
    foreach ($category in $config.categoryMappings.PSObject.Properties) {
        $CategoryMappings[$category.Name] = $category.Value
    }
}

function Repair-MalformedEventIds {
    param([string]$SearchPath)
    
    Write-Host "🔧 Repairing malformed EventIds..." -ForegroundColor Cyan
    
    $files = Get-ChildItem -Path $SearchPath -Recurse -Filter "*.cs" | Where-Object { 
        $_.FullName -notmatch "\\bin\\|\\obj\\|\\packages\\" 
    }
    
    $repairedCount = 0
    
    foreach ($file in $files) {
        $content = Get-Content $file.FullName -Raw
        $originalContent = $content
        
        # Fix EventIds that are too large (likely concatenated)
        # Pattern: EventId = [very large number]
        $content = $content -replace 'EventId\s*=\s*(\d{7,})', {
            param($match)
            $largeNumber = $match.Groups[1].Value
            Write-Host "  🔧 Fixing malformed EventId $largeNumber in $($file.Name)" -ForegroundColor Yellow
            
            # Extract a reasonable 4-digit number from the large number
            # Take the last 4 digits, but ensure it's in a valid range
            $lastFour = $largeNumber.Substring([Math]::Max(0, $largeNumber.Length - 4))
            $eventId = [int]$lastFour
            
            # Ensure it's in a reasonable range (1000-9999)
            if ($eventId -lt 1000) { $eventId += 1000 }
            if ($eventId -gt 9999) { $eventId = 1000 + ($eventId % 1000) }
            
            return "EventId = $eventId"
        }
        
        if ($content -ne $originalContent) {
            Set-Content -Path $file.FullName -Value $content -NoNewline
            $repairedCount++
        }
    }
    
    if ($repairedCount -gt 0) {
        Write-Host "✅ Repaired $repairedCount files with malformed EventIds" -ForegroundColor Green
    } else {
        Write-Host "✅ No malformed EventIds found" -ForegroundColor Green
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
            $eventIdMatch = if ($attributeContent -match 'EventId\s*=\s*(\d+)') { $matches[1] } else { "0" }
            $eventId = try { 
                $num = [long]$eventIdMatch
                if ($num -gt [int]::MaxValue -or $num -lt 0) { 
                    Write-Warning "Skipping invalid EventId $num in $file"
                    0 
                } else { 
                    [int]$num 
                }
            } catch { 
                Write-Warning "Could not parse EventId '$eventIdMatch' in $file"
                0 
            }
            $level = if ($attributeContent -match 'Level\s*=\s*([^,\)]+)') { $matches[1].Trim() } else { "" }
            $message = if ($attributeContent -match 'Message\s*=\s*"([^"]*)"') { $matches[1] } else { "" }
            
            # Parse parameters
            $paramList = @()
            if ($parameters.Trim()) {
                $paramList = ($parameters -split ',') | ForEach-Object { $_.Trim() -replace '^\w+\s+', '' -replace '\s+\w+$', '' }
            }
            
            # Determine category based on file path or class name
            $category = Get-LoggingCategory -FilePath $file.FullName -ClassName (Get-ClassName $file.FullName) -CategoryMappings $CategoryMappings
            
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

function Get-LogLevelFormat {
    param([string]$FilePath)
    
    $content = Get-Content $FilePath -Raw
    
    # Check for potential LogLevel conflicts by looking for multiple LogLevel imports/usings
    $hasLibVLCSharp = $content -match 'using\s+LibVLCSharp' -or $content -match 'LibVLCSharp\.Shared\.LogLevel'
    $hasMicrosoftLogging = $content -match 'using\s+Microsoft\.Extensions\.Logging' -or $content -match 'Microsoft\.Extensions\.Logging\.LogLevel'
    
    # If both are present, use full namespace to avoid ambiguity
    if ($hasLibVLCSharp -and $hasMicrosoftLogging) {
        return "Microsoft.Extensions.Logging.LogLevel"
    }
    
    # Otherwise use short form
    return "LogLevel"
}

function Get-LoggingCategory {
    param([string]$FilePath, [string]$ClassName, [hashtable]$CategoryMappings)
    
    $relativePath = $FilePath -replace [regex]::Escape((Get-Location).Path), ""
    $fullContext = "$relativePath $ClassName"
    
    # Check each category mapping
    foreach ($category in $CategoryMappings.Keys) {
        $patterns = $CategoryMappings[$category]
        foreach ($pattern in $patterns) {
            if ($fullContext -match $pattern) {
                return $category
            }
        }
    }
    
    # Default to Core for everything else
    return "Core"
}

function Format-LoggerMessages {
    param([string]$SearchPath, [switch]$DryRun)
    
    Write-Host "🎨 Formatting LoggerMessage methods to preferred style..." -ForegroundColor Cyan
    
    $csFiles = Get-ChildItem -Path $SearchPath -Filter "*.cs" -Recurse
    $changedFiles = 0
    
    foreach ($file in $csFiles) {
        $content = Get-Content $file.FullName -Raw
        $originalContent = $content
        
        # Pattern to match LoggerMessage attributes (single or multi-line, both positional and named)
        $pattern = '(?m)^\s*\[LoggerMessage\s*\([^\]]+\)\]\s*\r?\n\s*private\s+partial\s+void\s+\w+\s*\([^)]*\)\s*;'
        
        $content = [regex]::Replace($content, $pattern, {
            param($match)
            
            $text = $match.Value
            
            # Extract components - handle both positional and named parameters  
            if ($text -match '(?s)\[LoggerMessage\s*\(([^\]]+)\)\]\s*\r?\n\s*private\s+partial\s+void\s+(\w+)\s*\(([^)]*)\)\s*;') {
                $attributeParams = $matches[1]
                $methodName = $matches[2]
                $methodParams = $matches[3]
                
                # Clean up whitespace and newlines from attribute parameters
                $attributeParams = $attributeParams -replace '\s+', ' ' -replace '^\s+|\s+$', ''
                
                # Parse attribute parameters (handle both formats)
                $eventId = ""
                $level = ""
                $message = ""
                
                # Handle named parameter format
                if ($attributeParams -match 'EventId\s*=\s*(\d+)') {
                    $eventId = $matches[1]
                    if ($attributeParams -match 'Level\s*=\s*([^,\)]+)') { 
                        $level = $matches[1].Trim()
                    }
                    if ($attributeParams -match 'Message\s*=\s*"((?:[^"\\]|\\.)*)"') { 
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
                        # Don't strip quotes from message - preserve the entire quoted string including escaped quotes
                        $message = $parts[2]
                        if ($message -match '^"(.*)"$') {
                            $message = $matches[1]  # Remove outer quotes but preserve inner escaped quotes
                        }
                    }
                }
                
                # Normalize LogLevel format based on conflict detection
                if ($level) {
                    # Remove any existing namespace
                    $level = $level -replace "Microsoft\.Extensions\.Logging\.LogLevel\.", ""
                    $level = $level -replace "LibVLCSharp\.Shared\.LogLevel\.", ""
                    $level = $level -replace "^LogLevel\.", ""
                    
                    # Detect LogLevel conflicts in this file
                    $logLevelFormat = Get-LogLevelFormat -FilePath $file.FullName
                    
                    # Apply the appropriate format for this file
                    if ($logLevelFormat -eq "Microsoft.Extensions.Logging.LogLevel") {
                        $level = "Microsoft.Extensions.Logging.LogLevel.$level"
                    } else {
                        $level = "LogLevel.$level"
                    }
                }
                
                # Use defaults if parsing failed
                if (-not $eventId) { $eventId = "1000" }
                if (-not $level) { 
                    $logLevelFormat = Get-LogLevelFormat -FilePath $file.FullName
                    $level = if ($logLevelFormat -eq "Microsoft.Extensions.Logging.LogLevel") { 
                        "Microsoft.Extensions.Logging.LogLevel.Information" 
                    } else { 
                        "LogLevel.Information" 
                    }
                }
                if (-not $message) { $message = "Message not found" }
                
                # Format in preferred single-line style with consistent 4-space indentation
                return @"

    [LoggerMessage(EventId = $eventId, Level = $level, Message = "$message")]
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
        # Check parameter naming consistency (SYSLIB1015)
        $messageParams = [regex]::Matches($msg.Message, '\{(\w+)\}') | ForEach-Object { $_.Groups[1].Value }
        $methodParams = $msg.Parameters | ForEach-Object { 
            # Extract parameter name from "Type paramName" format
            if ($_ -match '\s+(\w+)$') { $matches[1] } else { $_ }
        }
        
        # Check for parameters in method but not in message (SYSLIB1015)
        foreach ($methodParam in $methodParams) {
            if ($methodParam -notin $messageParams) {
                $issues += "⚠️  SYSLIB1015: $($msg.ClassName).$($msg.MethodName): Parameter '$methodParam' not referenced in message template"
                $issues += "   File: $($msg.FilePath -replace [regex]::Escape((Get-Location).Path), '')"
                $issues += "   Message: `"$($msg.Message)`""
                $issues += ""
            }
        }
        
        # Check for parameters in message but not in method
        foreach ($msgParam in $messageParams) {
            if ($msgParam -notin $methodParams) {
                $issues += "❌ $($msg.ClassName).$($msg.MethodName): Message parameter '{$msgParam}' not found in method parameters"
                $issues += "   File: $($msg.FilePath -replace [regex]::Escape((Get-Location).Path), '')"
                $issues += ""
            }
        }
        
        # Check EventId is positive
        if ($msg.EventId -le 0) {
            $issues += "❌ $($msg.ClassName).$($msg.MethodName): EventId must be positive (current: $($msg.EventId))"
            $issues += ""
        }
        
        # Check Level format
        if ($msg.Level -and $msg.Level -notmatch "LogLevel") {
            $issues += "⚠️  $($msg.ClassName).$($msg.MethodName): Consider using LogLevel namespace"
            $issues += ""
        }
    }
    
    if ($issues.Count -eq 0) {
        Write-Host "✅ All LoggerMessage methods are valid!" -ForegroundColor Green
    } else {
        Write-Host "`n🚨 Validation Issues Found:" -ForegroundColor Red
        foreach ($issue in $issues) {
            if ($issue) {
                Write-Host "  $issue" -ForegroundColor Yellow
            } else {
                Write-Host ""
            }
        }
        
        # Provide suggestions for SYSLIB1015 fixes
        $syslib1015Issues = $issues | Where-Object { $_ -match "SYSLIB1015" }
        if ($syslib1015Issues.Count -gt 0) {
            Write-Host "`n💡 SYSLIB1015 Fix Suggestions:" -ForegroundColor Cyan
            Write-Host "  1. Add missing parameters to message template: `"Message with {ParameterName}`"" -ForegroundColor White
            Write-Host "  2. Remove unused parameters from method signature" -ForegroundColor White
            Write-Host "  3. Use underscore for intentionally unused parameters: `"_ => LogUnused()`"" -ForegroundColor White
        }
    }
    
    return $issues.Count -eq 0
}

function Fix-EventIds {
    param([string]$SearchPath, [switch]$DryRun)
    
    Write-Host "🔧 Fixing EventId conflicts and organizing with 100-based class ranges..." -ForegroundColor Cyan
    
    $loggerMessages = Get-LoggerMessages -SearchPath $SearchPath
    $changes = @()
    
    # Group by category first, then by class within each category
    $byCategory = $loggerMessages | Group-Object Category
    
    foreach ($categoryGroup in $byCategory) {
        $category = $categoryGroup.Name
        $range = $EventIdRanges[$category]
        $rangeSize = $Config.classRanges.rangeSize
        
        # Group by class within this category
        $byClass = $categoryGroup.Group | Group-Object ClassName | Sort-Object Name
        
        $classIndex = 0
        foreach ($classGroup in $byClass) {
            $className = $classGroup.Name
            $messages = $classGroup.Group | Sort-Object MethodName
            
            # Calculate starting EventId for this class (100-based ranges)
            $classStartId = $range.Start + ($classIndex * $rangeSize)
            
            # Ensure we don't exceed the category range
            if ($classStartId + $messages.Count - 1 > $range.End) {
                Write-Warning "⚠️  Class $className in category $category would exceed range limit. Consider increasing range or reducing classes."
                continue
            }
            
            $nextId = $classStartId
            
            foreach ($msg in $messages) {
                if ($msg.EventId -ne $nextId) {
                    $changes += @{
                        File = $msg.FilePath
                        OldEventId = $msg.EventId
                        NewEventId = $nextId
                        Method = "$($msg.ClassName).$($msg.MethodName)"
                        Category = $category
                        ClassName = $className
                        ClassRange = "$classStartId-$($classStartId + $rangeSize - 1)"
                    }
                }
                $nextId++
            }
            
            $classIndex++
        }
    }
    
    if ($changes.Count -eq 0) {
        Write-Host "✅ No EventId changes needed!" -ForegroundColor Green
        return
    }
    
    Write-Host "`n📋 Proposed EventId Changes (100-based class ranges):" -ForegroundColor White
    $changesByClass = $changes | Group-Object ClassName | Sort-Object Name
    
    foreach ($classGroup in $changesByClass) {
        $className = $classGroup.Name
        $classChanges = $classGroup.Group
        $range = $classChanges[0].ClassRange
        $category = $classChanges[0].Category
        
        Write-Host "`n  📁 $className [$category] → Range: $range" -ForegroundColor Cyan
        foreach ($change in $classChanges) {
            Write-Host "    $($change.Method): $($change.OldEventId) → $($change.NewEventId)" -ForegroundColor Yellow
        }
    }
    
    Write-Host "`n📊 Summary:" -ForegroundColor White
    Write-Host "  Total changes: $($changes.Count)"
    Write-Host "  Classes affected: $(($changes | Group-Object ClassName).Count)"
    Write-Host "  Categories: $(($changes | Group-Object Category).Count)"
    
    if (-not $DryRun) {
        $confirm = Read-Host "`nApply these changes? (y/N)"
        if ($confirm -eq 'y' -or $confirm -eq 'Y') {
            # Apply changes
            foreach ($change in $changes) {
                $content = Get-Content $change.File -Raw
                # Use word boundaries to ensure exact EventId match and prevent partial replacements
                $pattern = "EventId\s*=\s*$($change.OldEventId)\b"
                $replacement = "EventId = $($change.NewEventId)"
                $content = $content -replace $pattern, $replacement
                Set-Content -Path $change.File -Value $content -NoNewline
            }
            Write-Host "✅ EventId changes applied with 100-based class ranges!" -ForegroundColor Green
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
    "Format" { 
        Repair-MalformedEventIds -SearchPath $Path
        Format-LoggerMessages -SearchPath $Path -DryRun:$DryRun 
    }
    "Analyze" { 
        Repair-MalformedEventIds -SearchPath $Path
        Analyze-EventIds -SearchPath $Path 
    }
    "Validate" { 
        Repair-MalformedEventIds -SearchPath $Path
        Validate-LoggerMessages -SearchPath $Path 
    }
    "Report" { 
        Repair-MalformedEventIds -SearchPath $Path
        Show-Report -SearchPath $Path 
    }
    "FixEventIds" { 
        Repair-MalformedEventIds -SearchPath $Path
        Fix-EventIds -SearchPath $Path -DryRun:$DryRun 
    }
    "Repair" { 
        Repair-MalformedEventIds -SearchPath $Path 
    }
}

Write-Host "`n🎯 Script completed!" -ForegroundColor Green
