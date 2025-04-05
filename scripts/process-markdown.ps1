#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Numbers all Markdown headings in a series of *.md files and creates a table of contents.
.DESCRIPTION
    This script processes all *.md files in a specified directory, numbers the headings
    (# to #####) and creates a table of contents in a separate file.
.PARAMETER InputDirectory
    The directory containing the *.md files to be processed.
.PARAMETER TocFile
    The file where the table of contents should be saved.
.EXAMPLE
    ./process-markdown.ps1 -InputDirectory "./docs" -TocFile "./docs/TOC.md"
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$InputDirectory,

    [Parameter(Mandatory = $true)]
    [string]$TocFile
)

# Color definitions for console output
$colors = @{
    Info      = [ConsoleColor]::Cyan
    Success   = [ConsoleColor]::Green
    Warning   = [ConsoleColor]::Yellow
    Error     = [ConsoleColor]::Red
    Highlight = [ConsoleColor]::Magenta
}

# UTF-8 Icons for console output
$icons = @{
    Info       = "ℹ️ "
    Success    = "✅ "
    Warning    = "⚠️ "
    Error      = "❌ "
    Processing = "🔄 "
    File       = "📄 "
    Folder     = "📁 "
    List       = "📋 "
    Number     = "🔢 "
    Write      = "✍️ "
}

function Write-ColorOutput {
    param(
        [string]$Message,
        [ConsoleColor]$Color,
        [string]$Icon = ""
    )

    Write-Host "$Icon$Message" -ForegroundColor $Color
}

function Initialize-Script {
    Clear-Host
    Write-ColorOutput "==================================================" $colors.Info
    Write-ColorOutput "  MARKDOWN PROCESSOR AND TABLE OF CONTENTS GENERATOR" $colors.Highlight
    Write-ColorOutput "==================================================" $colors.Info
    Write-ColorOutput "$($icons.Info)Starting Markdown file processing..." $colors.Info
    Write-ColorOutput "$($icons.Folder)Input directory: $InputDirectory" $colors.Info
    Write-ColorOutput "$($icons.File)Table of contents file: $TocFile" $colors.Info
    Write-ColorOutput "==================================================" $colors.Info
    Write-Host ""
}

function Get-MarkdownFiles {
    param(
        [string]$Directory,
        [string]$ExcludeFile
    )

    Write-ColorOutput "$($icons.Processing)Searching for Markdown files..." $colors.Info

    # Find all *.md files in the directory except the TOC file
    $mdFiles = Get-ChildItem -Path $Directory -Filter "*.md" |
    Where-Object { $_.FullName -ne (Resolve-Path $ExcludeFile).Path } |
    Sort-Object Name

    Write-ColorOutput "$($icons.Success)Found $($mdFiles.Count) Markdown files." $colors.Success

    return $mdFiles
}

function Update-MarkdownFiles {
    param(
        [array]$Files
    )

    Write-ColorOutput "$($icons.Number)Beginning to number headings..." $colors.Info

    $tocEntries = @()
    $counter = @{
        "level1" = 0
        "level2" = 0
        "level3" = 0
        "level4" = 0
        "level5" = 0
    }

    # Collect all headings and their new numbered versions for link updating
    $script:headingMap = @{}

    # First pass: Collect all headings and their new numbering
    # Reset level1 counter to ensure chapter numbering starts at 1
    $counter["level1"] = 0
    foreach ($file in $Files) {
        Write-ColorOutput "$($icons.Processing)Analyzing headings in $($file.Name)..." $colors.Info

        $content = Get-Content -Path $file.FullName -Raw
        $lines = $content -split "`r?`n"

        # Flag to track if we're inside a code block
        $inCodeBlock = $false
        $inlineCodeSegments = @()

        foreach ($line in $lines) {
            # Check if the line starts or ends a code block
            if ($line -match '^```') {
                $inCodeBlock = !$inCodeBlock
                continue
            }

            # Skip heading processing if inside a code block
            if ($inCodeBlock) {
                continue
            }

            # Handle inline code segments - temporarily remove them before processing headings
            if ($line -match '`[^`]+`') {
                $inlineCodeSegments = @()
                $tempLine = $line
                $regexMatches = [regex]::Matches($line, '`[^`]+`')

                # Store all inline code segments
                foreach ($match in $regexMatches) {
                    $inlineCodeSegments += $match.Value
                    # Replace inline code with a placeholder
                    $tempLine = $tempLine.Replace($match.Value, "INLINE_CODE_PLACEHOLDER_" + $inlineCodeSegments.Count)
                }

                # Process the line without inline code
                $processLine = $tempLine
            }
            else {
                $processLine = $line
            }

            # Check if the line is a heading (but not in a code block)
            if ($processLine -match '^(#{1,5})\s+(.+)$') {
                $hashmarks = $matches[1]
                $originalHeading = $matches[2]

                # Remove any existing numbering patterns including multiple formats:
                # - Standard numbering like "19.4.2 " or "5. "
                # - Repetitive patterns like "19 19 19 4."
                # - Chapter references like "54 7." (old number + manual number)
                # - Plain numbers like "54 API Specification"

                # Special case for chapter headers that look like "54 API Specification" or similar patterns
                # This handles the case where there's just a number at the beginning without dots
                $heading = $originalHeading -replace '^\d+\s+', ''
                # Then remove any numbers at beginning with optional dots
                $heading = $heading -replace '^(\d+\s+)*', ''
                # Then remove any X.Y.Z style numbering that might be at the beginning
                $heading = $heading -replace '^(\d+(\.\d+)*)\s+', ''
                # Finally, handle any other pattern with a number and period (e.g., "7. ")
                $heading = $heading -replace '^(\d+\.)\s+', ''
                $level = $hashmarks.Length

                # Determine numbering (same logic as in the original)
                if ($level -eq 1) {
                    $counter["level1"]++
                    $counter["level2"] = 0
                    $counter["level3"] = 0
                    $counter["level4"] = 0
                    $counter["level5"] = 0
                    $number = "$($counter["level1"])"
                }
                elseif ($level -eq 2) {
                    $counter["level2"]++
                    $counter["level3"] = 0
                    $counter["level4"] = 0
                    $counter["level5"] = 0
                    $number = "$($counter["level1"]).$($counter["level2"])"
                }
                elseif ($level -eq 3) {
                    $counter["level3"]++
                    $counter["level4"] = 0
                    $counter["level5"] = 0
                    $number = "$($counter["level1"]).$($counter["level2"]).$($counter["level3"])"
                }
                elseif ($level -eq 4) {
                    $counter["level4"]++
                    $counter["level5"] = 0
                    $number = "$($counter["level1"]).$($counter["level2"]).$($counter["level3"]).$($counter["level4"])"
                }
                elseif ($level -eq 5) {
                    $counter["level5"]++
                    $number = "$($counter["level1"]).$($counter["level2"]).$($counter["level3"]).$($counter["level4"]).$($counter["level5"])"
                }

                # Save old and new anchor
                $oldAnchor = $originalHeading.ToLower() -replace '\s+', '-' -replace '[^\w\-]', ''
                $newHeading = "$number $heading"
                $newAnchor = $newHeading.ToLower() -replace '\s+', '-' -replace '[^\w\-]', ''

                $script:headingMap[$oldAnchor] = $newAnchor
            }
        }
    }

    # Reset counters for the second pass
    $counter = @{
        "level1" = 0
        "level2" = 0
        "level3" = 0
        "level4" = 0
        "level5" = 0
    }

    # Second pass: Number headings and update links
    # Reset level1 counter to ensure chapter numbering starts at 1
    $counter["level1"] = 0
    foreach ($file in $Files) {
        Write-ColorOutput "$($icons.Processing)Processing $($file.Name)..." $colors.Info

        $content = Get-Content -Path $file.FullName -Raw
        $lines = $content -split "`r?`n"
        $modifiedLines = @()

        # Flag to track if we're inside a code block
        $inCodeBlock = $false
        $inlineCodeSegments = @()

        foreach ($line in $lines) {
            # Check if the line starts or ends a code block
            if ($line -match '^```') {
                $inCodeBlock = !$inCodeBlock
                $modifiedLines += $line
                continue
            }

            # Skip heading processing if inside a code block
            if ($inCodeBlock) {
                $modifiedLines += $line
                continue
            }

            # Handle inline code segments - temporarily remove them before processing headings
            if ($line -match '`[^`]+`') {
                $inlineCodeSegments = @()
                $tempLine = $line
                $regexMatches = [regex]::Matches($line, '`[^`]+`')

                # Store all inline code segments
                foreach ($match in $regexMatches) {
                    $inlineCodeSegments += $match.Value
                    # Replace inline code with a placeholder
                    $tempLine = $tempLine.Replace($match.Value, "INLINE_CODE_PLACEHOLDER_" + $inlineCodeSegments.Count)
                }

                # Process the line without inline code
                $processLine = $tempLine
            }
            else {
                $processLine = $line
            }

            # Check if the line is a heading (but not in a code block)
            if ($processLine -match '^(#{1,5})\s+(.+)$') {
                $hashmarks = $matches[1]
                $originalHeading = $matches[2]

                # Remove any existing numbering patterns including multiple formats:
                # - Standard numbering like "19.4.2 " or "5. "
                # - Repetitive patterns like "19 19 19 4."
                # - Chapter references like "54 7." (old number + manual number)
                # - Plain numbers like "54 API Specification"

                # Special case for chapter headers that look like "54 API Specification" or similar patterns
                # This handles the case where there's just a number at the beginning without dots
                $heading = $originalHeading -replace '^\d+\s+', ''
                # Then remove any numbers at beginning with optional dots
                $heading = $heading -replace '^(\d+\s+)*', ''
                # Then remove any X.Y.Z style numbering that might be at the beginning
                $heading = $heading -replace '^(\d+(\.\d+)*)\s+', ''
                # Finally, handle any other pattern with a number and period (e.g., "7. ")
                $heading = $heading -replace '^(\d+\.)\s+', ''
                $level = $hashmarks.Length

                # Reset numbering for subordinate levels
                if ($level -eq 1) {
                    $counter["level1"]++
                    $counter["level2"] = 0
                    $counter["level3"] = 0
                    $counter["level4"] = 0
                    $counter["level5"] = 0
                    $number = "$($counter["level1"])"
                }
                elseif ($level -eq 2) {
                    $counter["level2"]++
                    $counter["level3"] = 0
                    $counter["level4"] = 0
                    $counter["level5"] = 0
                    $number = "$($counter["level1"]).$($counter["level2"])"
                }
                elseif ($level -eq 3) {
                    $counter["level3"]++
                    $counter["level4"] = 0
                    $counter["level5"] = 0
                    $number = "$($counter["level1"]).$($counter["level2"]).$($counter["level3"])"
                }
                elseif ($level -eq 4) {
                    $counter["level4"]++
                    $counter["level5"] = 0
                    $number = "$($counter["level1"]).$($counter["level2"]).$($counter["level3"]).$($counter["level4"])"
                }
                elseif ($level -eq 5) {
                    $counter["level5"]++
                    $number = "$($counter["level1"]).$($counter["level2"]).$($counter["level3"]).$($counter["level4"]).$($counter["level5"])"
                }

                # Heading with numbering
                $numberedHeading = "$hashmarks $number $heading"
                # Add TOC entry
                $indent = "    " * ($level - 1)
                $link = ($number + ' ' + $heading).ToLower() -replace '\s+', '-' -replace '[^\w\-]', ''

                # If we had inline code, restore it in the heading
                if ($line -match '`[^`]+`') {
                    for ($i = 0; $i -lt $inlineCodeSegments.Count; $i++) {
                        $numberedHeading = $numberedHeading.Replace("INLINE_CODE_PLACEHOLDER_" + ($i + 1), $inlineCodeSegments[$i])
                    }
                }

                $modifiedLines += $numberedHeading
                $tocEntries += "$indent- [$number $heading](#$link) _($($file.Name))_"
            }
            # Check if the line contains internal links
            elseif ($line -match '\[.+?\]\(#(.+?)\)') {
                # Find and update all internal links in the line
                $modifiedLine = $line

                # Find each link in the line - using a more robust regex that can handle various link formats
                $regexMatches = [regex]::Matches($line, '\[(.*?)\]\(#([^)]+)\)')

                foreach ($match in $regexMatches) {
                    $fullMatch = $match.Value
                    $linkText = $match.Groups[1].Value
                    $originalAnchor = $match.Groups[2].Value

                    # Decode URL-encoded characters if present in the anchor
                    try {
                        $originalAnchor = [System.Web.HttpUtility]::UrlDecode($originalAnchor)
                    }
                    catch {
                        # If System.Web is not available, try a basic decode for common entities
                        $originalAnchor = $originalAnchor -replace '%20', ' ' -replace '%2F', '/'
                    }

                    # Clean the original anchor to match how it would be stored in the heading map
                    $cleanAnchor = $originalAnchor.ToLower() -replace '\s+', '-' -replace '[^\w\-]', ''

                    # If the anchor exists in the map, update the link
                    if ($script:headingMap.ContainsKey($cleanAnchor)) {
                        $newAnchor = $script:headingMap[$cleanAnchor]
                        $newLink = "[$linkText](#$newAnchor)"
                        $modifiedLine = $modifiedLine.Replace($fullMatch, $newLink)
                        Write-Verbose "Updated link: $fullMatch -> $newLink"
                    }
                    else {
                        # Log unresolved references when in verbose mode
                        Write-Verbose "Unresolved reference: $fullMatch (anchor '$cleanAnchor' not found in heading map)"
                    }
                }

                $modifiedLines += $modifiedLine
            }
            else {
                $modifiedLines += $line
            }
        }

        # Save the modified file
        $modifiedContent = $modifiedLines -join "`n"
        Set-Content -Path $file.FullName -Value $modifiedContent
    }

    Write-ColorOutput "$($icons.Success)Heading numbering and internal link updates completed." $colors.Success

    return $tocEntries
}

function Test-MarkdownReferences {
    param(
        [array]$Files,
        [hashtable]$HeadingMap
    )

    Write-ColorOutput "$($icons.Processing)Checking for unresolved references..." $colors.Info

    $unresolvedReferences = @()

    foreach ($file in $Files) {
        Write-ColorOutput "$($icons.Processing)Analyzing references in $($file.Name)..." $colors.Info
        $content = Get-Content -Path $file.FullName -Raw
        $lines = $content -split "`r?`n"
        $lineNumber = 0

        foreach ($line in $lines) {
            $lineNumber++

            # Skip code blocks (simplified check - not handling multi-line code blocks)
            if ($line -match '^```') {
                continue
            }

            # Find all link references
            if ($line -match '\[.+?\]\(#(.+?)\)') {
                $regexMatches = [regex]::Matches($line, '\[(.*?)\]\(#([^)]+)\)')

                foreach ($match in $regexMatches) {
                    $linkText = $match.Groups[1].Value
                    $originalAnchor = $match.Groups[2].Value

                    # Clean the anchor to match how it would be stored in the heading map
                    $cleanAnchor = $originalAnchor.ToLower() -replace '\s+', '-' -replace '[^\w\-]', ''

                    if (-not $HeadingMap.ContainsKey($cleanAnchor)) {
                        $unresolvedReferences += [PSCustomObject]@{
                            File       = $file.Name
                            LineNumber = $lineNumber
                            Reference  = $match.Value
                            Anchor     = $cleanAnchor
                        }
                    }
                }
            }
        }
    }

    if ($unresolvedReferences.Count -gt 0) {
        Write-ColorOutput "$($icons.Warning)Found $($unresolvedReferences.Count) unresolved references:" $colors.Warning
        foreach ($ref in $unresolvedReferences) {
            Write-ColorOutput "  - $($ref.File) (line $($ref.LineNumber)): $($ref.Reference)" $colors.Warning
        }
    }
    else {
        Write-ColorOutput "$($icons.Success)All references resolved successfully." $colors.Success
    }

    return $unresolvedReferences
}

function Write-TableOfContents {
    param(
        [array]$Entries,
        [string]$OutputFile
    )

    Write-ColorOutput "$($icons.List)Creating table of contents..." $colors.Info

    $tocContent = @"
# Table of Contents

_Automatically generated on $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")_

"@

    $tocContent += $Entries -join "`n"

    Set-Content -Path $OutputFile -Value $tocContent

    Write-ColorOutput "$($icons.Success)Table of contents has been saved to '$OutputFile'." $colors.Success
}

# Main script
try {
    # Check if the input directory exists
    if (-not (Test-Path -Path $InputDirectory -PathType Container)) {
        throw "The specified input directory does not exist: $InputDirectory"
    }

    Initialize-Script

    $mdFiles = Get-MarkdownFiles -Directory $InputDirectory -ExcludeFile $TocFile

    if ($mdFiles.Count -eq 0) {
        Write-ColorOutput "$($icons.Warning)No Markdown files found for processing." $colors.Warning
    }
    else {
        $tocEntries = Update-MarkdownFiles -Files $mdFiles
        Write-TableOfContents -Entries $tocEntries -OutputFile $TocFile

        # Check for unresolved references
        $unresolvedRefs = Test-MarkdownReferences -Files $mdFiles -HeadingMap $script:headingMap

        if ($unresolvedRefs.Count -eq 0) {
            Write-Host ""
            Write-ColorOutput "$($icons.Success)Processing completed successfully!" $colors.Success
        }
        else {
            Write-Host ""
            Write-ColorOutput "$($icons.Warning)Processing completed with warnings. Some references could not be resolved." $colors.Warning
        }
    }
}
catch {
    Write-ColorOutput "$($icons.Error)ERROR: $_" $colors.Error
    exit 1
}
