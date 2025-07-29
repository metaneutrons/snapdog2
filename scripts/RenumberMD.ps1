#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Renumbers headings in architecture documentation files, handles internal references, and generates a verbose table of contents.

.DESCRIPTION
    This script processes markdown files in a specified directory, renumbers their headings sequentially
    based on alphabetical file order, updates internal references to maintain consistency, and generates
    a comprehensive table of contents. The script handles three types of references:

    1. File-only references: Updates with proper file number and caption
    2. Implicit anchor references: Updates anchors to match new heading numbering
    3. Explicit anchor references: Left untouched to preserve manual customizations

    The script uses a four-phase approach:
    - Phase 1: Build file inventory and extract all headings
    - Phase 2: Process and update internal references
    - Phase 3: Renumber headings with new sequential numbers
    - Phase 4: Generate updated table of contents

.PARAMETER Path
    Mandatory. The directory path or wildcard pattern for the markdown files to process.

.PARAMETER TocFile
    Optional. The name of the table of contents file to generate.
    Defaults to "index.md" in the same directory as the processed files.

.PARAMETER DisableManualNumberingRemoval
    Optional. If specified, manual numbering in headings will not be removed.
    Defaults to removing manual numbering.

.PARAMETER DebugMode
    Optional. If specified, enables debug output for detailed processing information.

.EXAMPLE
    .\RenumberMD.ps1 -Path "docs/architecture"
    Processes all .md files in the docs/architecture directory

.EXAMPLE
    .\RenumberMD.ps1 -Path "docs/architecture/*.md" -TocFile "architecture.md"
    Processes specific markdown files and generates architecture.md as the table of contents
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$Path,

    [Parameter(Mandatory = $false)]
    [string]$TocFile = "",

    [Parameter(Mandatory = $false)]
    [switch]$DisableManualNumberingRemoval,

    [Parameter(Mandatory = $false)]
    [switch]$DebugMode = $false
)

# Function to extract and parse headings from markdown content
function Get-MarkdownHeadings {
    param([string[]]$Content)

    $headings = @()
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

        # Match headings (only outside code blocks)
        if ($line -match '^(#{1,6})\s+(.+)$') {
            $level = $matches[1].Length
            $text = $matches[2].Trim()

            $headings += [PSCustomObject]@{
                LineNumber   = $i
                Level        = $level
                OriginalText = $text
                NewText      = ""
                FullLine     = $line
            }
        }
    }
    return $headings
}

# Function to build file inventory for reference processing
function Get-FileInventory {
    param(
        [array]$Files,
        [string]$BaseDir
    )

    $inventory = @{}
    $fileNumber = 1

    foreach ($file in $Files) {
        $content = Get-Content -Path $file.FullName -Encoding UTF8
        $headings = Get-MarkdownHeadings -Content $content

        # Get the main heading (H1) for caption generation
        $mainHeading = $headings | Where-Object { $_.Level -eq 1 } | Select-Object -First 1
        $caption = if ($mainHeading) {
            Remove-ManualNumbering -HeadingText $mainHeading.OriginalText -RemoveManualNumbering (-not $DisableManualNumberingRemoval) -DebugMode $false
        }
        else {
            $file.BaseName
        }

        # Calculate relative path from the base directory where files are processed
        # This ensures links are correct when TOC is generated in the same directory
        $relativePath = (Resolve-Path $file.FullName).Path.Replace((Resolve-Path $BaseDir).Path, "").TrimStart('\/')

        $inventory[$file.Name] = [PSCustomObject]@{
            FileName     = $file.Name
            FullPath     = $file.FullName
            RelativePath = $relativePath
            FileNumber   = $fileNumber
            Caption      = $caption
            Headings     = $headings
        }

        $fileNumber++
    }

    return $inventory
}

# Function to detect and remove manual numbering from heading text
function Remove-ManualNumbering {
    param(
        [string]$HeadingText,
        [bool]$RemoveManualNumbering = $true,
        [bool]$DebugMode = $false
    )

    if (-not $RemoveManualNumbering) {
        return $HeadingText
    }

    $originalText = $HeadingText

    # Step 1: Remove standard numbering patterns (existing logic)
    $cleanedText = $HeadingText -replace '^\d+(\.\d+)*\.\s*', ''

    # Step 2: Remove additional manual numbering that might appear at the start
    # This catches patterns like "2 General Conventions" after "2.2. " has been removed
    $cleanedText = $cleanedText -replace '^\d+\s+', ''

    # Step 3: Remove patterns like "2. Something" that might remain
    $cleanedText = $cleanedText -replace '^\d+\.\s*', ''

    # Step 4: Handle multiple consecutive numbering patterns
    # Keep applying until no more changes occur
    do {
        $previousText = $cleanedText
        $cleanedText = $cleanedText -replace '^\d+(\.\d+)*\.?\s*', ''
    } while ($cleanedText -ne $previousText -and $cleanedText.Length -gt 0)

    if ($DebugMode -and $originalText -ne $cleanedText) {
        Write-Host "    Manual numbering removed: '$originalText' -> '$cleanedText'" -ForegroundColor Yellow
    }

    return $cleanedText.Trim()
}

# Function to generate anchor from heading text (mimics GitHub Markdown anchor generation)
function Get-StableAnchor {
    param([string]$HeadingText)

    # Convert to lowercase and replace spaces/special characters with hyphens
    # Keep the numbering as part of the anchor to match actual headings
    $anchor = $HeadingText.ToLower()
    $anchor = $anchor -replace '[^\w\s-]', ''  # Remove special characters except hyphens
    $anchor = $anchor -replace '\s+', '-'      # Replace spaces with hyphens
    $anchor = $anchor -replace '-+', '-'       # Replace multiple hyphens with single
    $anchor = $anchor.Trim('-')                # Remove leading/trailing hyphens

    return $anchor
}

# Function to extract all markdown references from content
function Get-MarkdownReferences {
    param(
        [string[]]$Content,
        [string]$FilePath
    )

    $references = @()
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

        # Find all markdown links in the line
        $linkMatches = [regex]::Matches($line, '\[([^\]]+)\]\(([^)]+)\)')

        foreach ($match in $linkMatches) {
            $linkText = $match.Groups[1].Value
            $linkPath = $match.Groups[2].Value

            # Skip if it's not a .md file reference
            if ($linkPath -notmatch '\.md(\#.*)?$') {
                continue
            }

            # Parse the link
            $anchorIndex = $linkPath.IndexOf('#')
            if ($anchorIndex -ge 0) {
                $filePart = $linkPath.Substring(0, $anchorIndex)
                $anchorPart = $linkPath.Substring($anchorIndex + 1)
            }
            else {
                $filePart = $linkPath
                $anchorPart = $null
            }

            # Extract just the filename from the path
            $fileName = Split-Path $filePart -Leaf

            $references += [PSCustomObject]@{
                LineNumber = $i
                FullMatch  = $match.Value
                LinkText   = $linkText
                LinkPath   = $linkPath
                FileName   = $fileName
                FilePart   = $filePart
                AnchorPart = $anchorPart
                SourceFile = $FilePath
                HasAnchor  = $anchorPart -ne $null
            }
        }
    }

    return $references
}

# Function to categorize references as internal/external and anchor types
function Get-ReferenceCategory {
    param(
        [PSCustomObject]$Reference,
        [hashtable]$FileInventory
    )

    $isInternal = $FileInventory.ContainsKey($Reference.FileName)
    $isExplicitAnchor = $false

    if ($Reference.HasAnchor) {
        # Check if it's an explicit anchor (manually set, usually shorter and non-standard)
        # Explicit anchors typically don't follow the auto-generated pattern
        $anchor = $Reference.AnchorPart

        # Heuristic: If anchor is very short or contains numbers without text, it's likely explicit
        if ($anchor.Length -lt 10 -or $anchor -match '^\d+$' -or $anchor -match '^\d+-\w+$') {
            $isExplicitAnchor = $true
        }
    }

    return [PSCustomObject]@{
        IsInternal       = $isInternal
        IsExternal       = -not $isInternal
        HasAnchor        = $Reference.HasAnchor
        IsExplicitAnchor = $isExplicitAnchor
        IsImplicitAnchor = $Reference.HasAnchor -and -not $isExplicitAnchor
        IsFileOnly       = -not $Reference.HasAnchor
    }
}

# Function to find matching heading for an anchor
function Find-HeadingForAnchor {
    param(
        [string]$Anchor,
        [array]$Headings
    )

    foreach ($heading in $Headings) {
        # Try multiple anchor generation strategies to match different markdown processors

        # Strategy 1: Clean anchor without numbering (our standard approach)
        $stableAnchor = Get-StableAnchor -HeadingText $heading.OriginalText
        if ($stableAnchor -eq $Anchor) {
            return $heading
        }

        # Strategy 2: Include current numbering in anchor (GitHub style)
        $fullAnchor = $heading.OriginalText.ToLower() -replace '[^\w\s-]', '' -replace '\s+', '-' -replace '-+', '-'
        $fullAnchor = $fullAnchor.Trim('-')
        if ($fullAnchor -eq $Anchor) {
            return $heading
        }

        # Strategy 3: Try with simplified numbering patterns
        $numberedText = $heading.OriginalText -replace '^\d+(\.\d+)*\.\s*', ''
        $numberedAnchor = $numberedText.ToLower() -replace '[^\w\s-]', '' -replace '\s+', '-' -replace '-+', '-'
        $numberedAnchor = $numberedAnchor.Trim('-')
        if ($numberedAnchor -eq $Anchor) {
            return $heading
        }

        # Strategy 4: Try partial matches for complex headings
        if ($Anchor.Length -gt 5) {
            $headingWords = $numberedText.ToLower() -split '\s+' | ForEach-Object { $_ -replace '[^\w-]', '' } | Where-Object { $_.Length -gt 2 }
            $anchorWords = $Anchor -split '-' | Where-Object { $_.Length -gt 2 }

            if ($headingWords.Count -gt 0 -and $anchorWords.Count -gt 0) {
                $matchCount = 0
                foreach ($word in $anchorWords) {
                    if ($headingWords -contains $word) {
                        $matchCount++
                    }
                }

                # If more than 50% of significant words match, consider it a match
                if ($matchCount / $anchorWords.Count -gt 0.5 -and $matchCount -ge 2) {
                    return $heading
                }
            }
        }
    }

    return $null
}

# Function to update internal references
function Update-InternalReferences {
    param(
        [string[]]$Content,
        [array]$References,
        [hashtable]$FileInventory,
        [hashtable]$NewFileNumbers
    )

    $updatedContent = $Content.Clone()

    foreach ($ref in $References) {
        $category = Get-ReferenceCategory -Reference $ref -FileInventory $FileInventory

        # Skip external references and explicit anchors
        if ($category.IsExternal -or $category.IsExplicitAnchor) {
            continue
        }

        $targetFile = $FileInventory[$ref.FileName]
        if (-not $targetFile) {
            Write-Warning "Reference to unknown file: $($ref.FileName) in $($ref.SourceFile)"
            continue
        }

        $newFileNumber = $NewFileNumbers[$ref.FileName]
        $newLinkText = $ref.LinkText
        $newLinkPath = $ref.FilePart

        if ($category.IsFileOnly) {
            # Update file-only reference with new caption
            $newLinkText = "$newFileNumber. $($targetFile.Caption)"
            $newLinkPath = $targetFile.RelativePath
        }
        elseif ($category.IsImplicitAnchor) {
            # Update implicit anchor reference
            $matchingHeading = Find-HeadingForAnchor -Anchor $ref.AnchorPart -Headings $targetFile.Headings

            if ($matchingHeading) {
                # Generate new anchor with updated numbering
                $newHeadingText = if ($matchingHeading.Level -eq 1) {
                    $cleanText = Remove-ManualNumbering -HeadingText $matchingHeading.OriginalText -RemoveManualNumbering (-not $DisableManualNumberingRemoval) -DebugMode $false
                    "$newFileNumber. $cleanText"
                }
                else {
                    # Will be updated when we process the target file
                    $matchingHeading.OriginalText
                }

                $newAnchor = Get-StableAnchor -HeadingText $newHeadingText
                $newLinkPath = "$($targetFile.RelativePath)#$newAnchor"

                # Update link text if it contains the old numbering
                if ($newLinkText -match '^\d+(\.\d+)*\.') {
                    $baseText = $newLinkText -replace '^\d+(\.\d+)*\.\s*', ''
                    $newLinkText = "$newFileNumber. $baseText"
                }
            }
        }

        # Replace the reference in the content
        $newReference = "[$newLinkText]($newLinkPath)"
        $line = $updatedContent[$ref.LineNumber]
        $updatedContent[$ref.LineNumber] = $line.Replace($ref.FullMatch, $newReference)
    }

    return $updatedContent
}

# Function to process all references across all files
function Update-AllReferences {
    param(
        [hashtable]$FileInventory,
        [hashtable]$NewFileNumbers
    )

    Write-Host "Processing internal references..." -ForegroundColor Cyan

    foreach ($fileInfo in $FileInventory.Values) {
        $content = Get-Content -Path $fileInfo.FullPath -Encoding UTF8
        $references = Get-MarkdownReferences -Content $content -FilePath $fileInfo.FileName

        if ($references.Count -gt 0) {
            Write-Host "  Found $($references.Count) references in $($fileInfo.FileName)" -ForegroundColor Yellow

            # Update internal references
            $updatedContent = Update-InternalReferences -Content $content -References $references -FileInventory $FileInventory -NewFileNumbers $NewFileNumbers

            # Write updated content back to file
            $updatedContent | Set-Content -Path $fileInfo.FullPath -Encoding UTF8

            Write-Host "  ✓ Updated references in $($fileInfo.FileName)" -ForegroundColor Green
        }
    }
}

# Function to update anchor references after headings have been renumbered
function Update-PostRenumberingAnchors {
    param(
        [hashtable]$FileInventory,
        [hashtable]$NewFileNumbers,
        [hashtable]$UpdatedHeadings
    )

    Write-Host "Post-processing anchor references after renumbering..." -ForegroundColor Cyan

    foreach ($fileInfo in $FileInventory.Values) {
        $content = Get-Content -Path $fileInfo.FullPath -Encoding UTF8
        $references = Get-MarkdownReferences -Content $content -FilePath $fileInfo.FileName
        $hasUpdates = $false
        $updatedContent = $content.Clone()

        foreach ($ref in $references) {
            $category = Get-ReferenceCategory -Reference $ref -FileInventory $FileInventory

            # Only process internal implicit anchor references
            if ($category.IsInternal -and $category.IsImplicitAnchor) {
                $targetFile = $FileInventory[$ref.FileName]
                if ($targetFile -and $UpdatedHeadings.ContainsKey($ref.FileName)) {
                    $updatedHeadingsForFile = $UpdatedHeadings[$ref.FileName]
                    $matchingHeading = Find-HeadingForAnchor -Anchor $ref.AnchorPart -Headings $updatedHeadingsForFile

                    if ($matchingHeading) {
                        # Generate new anchor with the updated heading text
                        $newAnchor = Get-StableAnchor -HeadingText $matchingHeading.NewText
                        $newLinkPath = "$($targetFile.RelativePath)#$newAnchor"

                        # Update link text if it contains numbering
                        $newLinkText = $ref.LinkText
                        if ($newLinkText -match '^\d+(\.\d+)*\.') {
                            $baseText = $newLinkText -replace '^\d+(\.\d+)*\.\s*', ''
                            $fileNumber = $NewFileNumbers[$ref.FileName]
                            $newLinkText = "$fileNumber. $baseText"
                        }

                        # Replace the reference
                        $newReference = "[$newLinkText]($newLinkPath)"
                        $line = $updatedContent[$ref.LineNumber]
                        $updatedContent[$ref.LineNumber] = $line.Replace($ref.FullMatch, $newReference)
                        $hasUpdates = $true
                    }
                }
            }
        }

        if ($hasUpdates) {
            $updatedContent | Set-Content -Path $fileInfo.FullPath -Encoding UTF8
            Write-Host "  ✓ Post-processed anchors in $($fileInfo.FileName)" -ForegroundColor Green
        }
    }
}

# Function to renumber headings in content
function Update-HeadingNumbers {
    param(
        [string[]]$Content,
        [int]$FileNumber,
        [bool]$RemoveManualNumbering = $true,
        [bool]$DebugMode = $false
    )

    $headings = Get-MarkdownHeadings -Content $Content
    if ($headings.Count -eq 0) { return $Content, @(), @() }

    if ($DebugMode) {
        Write-Host "  Processing $($headings.Count) headings in file #$FileNumber" -ForegroundColor Cyan
    }

    # Counter for each heading level
    $counters = @(0, 0, 0, 0, 0, 0)  # Support up to 6 levels
    $updatedContent = $Content.Clone()
    $tocEntries = @()
    $updatedHeadings = @()

    foreach ($heading in $headings) {
        $level = $heading.Level

        if ($DebugMode) {
            Write-Host "    Processing heading: '$($heading.OriginalText)'" -ForegroundColor Gray
        }

        # Increment counter for current level
        $counters[$level - 1]++

        # Reset all deeper level counters
        for ($i = $level; $i -lt 6; $i++) {
            $counters[$i] = 0
        }

        # Clean the heading text using the new manual numbering removal function
        $newText = Remove-ManualNumbering -HeadingText $heading.OriginalText -RemoveManualNumbering $RemoveManualNumbering -DebugMode $DebugMode

        # Build the new heading number
        if ($level -eq 1) {
            $newNumber = "$FileNumber"
        }
        else {
            # Build hierarchical numbering (e.g., 6.1.2)
            $numberParts = @($FileNumber)
            for ($i = 1; $i -lt $level; $i++) {
                $numberParts += $counters[$i]
            }
            $newNumber = $numberParts -join '.'
        }

        # Create the new heading line
        $hashMarks = '#' * $level
        $newHeadingLine = "$hashMarks $newNumber. $newText"
        $updatedContent[$heading.LineNumber] = $newHeadingLine

        if ($DebugMode) {
            Write-Host "    Result: '$newHeadingLine'" -ForegroundColor Green
        }

        # Store for TOC generation
        $tocEntries += [PSCustomObject]@{
            Level    = $level
            Number   = $newNumber
            Text     = $newText
            FullText = "$newNumber. $newText"
        }

        # Store updated heading information for anchor processing
        $updatedHeadings += [PSCustomObject]@{
            LineNumber   = $heading.LineNumber
            Level        = $level
            OriginalText = $heading.OriginalText
            NewText      = "$newNumber. $newText"
            FullLine     = $newHeadingLine
        }
    }

    return $updatedContent, $tocEntries, $updatedHeadings
}

# Function to generate table of contents
function TableOfContents {
    param(
        [array]$AllTocEntries,
        [array]$FileInfos,
        [string]$TocFilePath
    )

    $toc = @()
    $toc += "# Table of Contents"
    $toc += ""

    # Calculate the directory where the TOC file will be placed
    $tocDir = Split-Path $TocFilePath -Parent
    if ([string]::IsNullOrEmpty($tocDir)) { $tocDir = "." }

    $currentFileIndex = 0
    foreach ($fileInfo in $FileInfos) {
        $fileEntries = $AllTocEntries | Where-Object { $_.FileIndex -eq $currentFileIndex }

        $hasSubLevels = ($fileEntries | Where-Object { $_.Level -gt 1 }).Count -gt 0

        foreach ($entry in $fileEntries) {
            # Generate anchor link for all levels
            $anchor = Get-StableAnchor -HeadingText $entry.FullText

            # Calculate proper relative path from TOC file to target file
            $targetFilePath = $fileInfo.FullPath
            $relativePath = [System.IO.Path]::GetRelativePath($tocDir, $targetFilePath)
            # Normalize path separators for cross-platform compatibility
            $relativePath = $relativePath.Replace('\', '/')

            $linkPath = "$relativePath#$anchor"

            if ($entry.Level -eq 1) {
                # Level 1: Use clean text without numbering
                $linkText = $entry.Text # e.g., "Introduction"
                $link = "[$linkText]($linkPath)"
                $toc += "$($entry.Number). $link"

                # Add blank line after level 1 if there are sub-levels
                if ($hasSubLevels) {
                    $toc += ""
                }
            }
            else {
                # Sub-levels: Include hierarchical numbering in the link text
                $linkText = "$($entry.Number) $($entry.Text)" # e.g., "1.1 Project Vision & Mission"
                $link = "[$linkText]($linkPath)"
                $toc += "- $link" # Unordered list at root level, not indented
            }
        }

        # Add blank line after each file's entries (except the last)
        if ($currentFileIndex -lt ($FileInfos.Count - 1)) {
            $toc += ""
        }

        $currentFileIndex++
    }

    return $toc
}

# Main script execution
try {
    Write-Host "Starting architecture documentation renumbering with reference handling..." -ForegroundColor Green

    # Resolve the path and get files
    if (Test-Path $Path -PathType Container) {
        # If it's a directory, get all .md files
        $files = Get-ChildItem -Path $Path -Filter "*.md" | Sort-Object Name
        $baseDir = $Path
    }
    elseif ($Path -like "*.*") {
        # If it contains wildcards or file extension
        $files = Get-ChildItem -Path $Path | Sort-Object Name
        $baseDir = Split-Path $Path -Parent
        if ([string]::IsNullOrEmpty($baseDir)) { $baseDir = "." }
    }
    else {
        throw "Invalid path specified: $Path"
    }

    if ($files.Count -eq 0) {
        throw "No markdown files found in the specified path: $Path"
    }

    # Determine TOC file path
    if ([string]::IsNullOrEmpty($TocFile)) {
        $TocFile = Join-Path $baseDir "index.md"
    }
    elseif (-not [System.IO.Path]::IsPathRooted($TocFile)) {
        $TocFile = Join-Path $baseDir $TocFile
    }

    Write-Host "Found $($files.Count) files to process" -ForegroundColor Yellow
    Write-Host "TOC will be generated at: $TocFile" -ForegroundColor Yellow
    Write-Host "Manual numbering removal: $(-not $DisableManualNumberingRemoval)" -ForegroundColor Yellow
    Write-Host "Debug mode: $DebugMode" -ForegroundColor Yellow

    # PHASE 1: Build file inventory and analyze references
    Write-Host "Phase 1: Building file inventory and analyzing references..." -ForegroundColor Magenta
    $fileInventory = Get-FileInventory -Files $files -BaseDir $baseDir

    # Create new file number mapping
    $newFileNumbers = @{}
    $fileNumber = 1
    foreach ($file in $files) {
        $newFileNumbers[$file.Name] = $fileNumber
        $fileNumber++
    }

    # PHASE 2: Update all internal references before renumbering headings
    Update-AllReferences -FileInventory $fileInventory -NewFileNumbers $newFileNumbers

    # PHASE 3: Process each file for heading renumbering
    Write-Host "Phase 3: Renumbering headings..." -ForegroundColor Magenta
    $allTocEntries = @()
    $fileInfos = @()
    $allUpdatedHeadings = @{}
    $fileNumber = 1

    foreach ($file in $files) {
        Write-Host "Processing: $($file.Name)" -ForegroundColor Cyan

        # Use sequential file numbering (1, 2, 3, 4...) regardless of filename
        Write-Host "  Using sequential file number: $fileNumber" -ForegroundColor Yellow

        # Read file content (it may have been updated by reference processing)
        $content = Get-Content -Path $file.FullName -Encoding UTF8

        # Update headings and get TOC entries
        $updatedContent, $tocEntries, $updatedHeadings = Update-HeadingNumbers -Content $content -FileNumber $fileNumber -RemoveManualNumbering (-not $DisableManualNumberingRemoval) -DebugMode $DebugMode

        # Store updated headings for post-processing
        $allUpdatedHeadings[$file.Name] = $updatedHeadings

        # Add file index to TOC entries (use sequential index, not filename-based)
        foreach ($entry in $tocEntries) {
            $entry | Add-Member -NotePropertyName "FileIndex" -NotePropertyValue ($fileNumber - 1)
            $entry | Add-Member -NotePropertyName "FileNumber" -NotePropertyValue $fileNumber
        }
        $allTocEntries += $tocEntries

        # Store file info for TOC generation
        $fileInfos += [PSCustomObject]@{
            Name     = $file.Name
            FullPath = $file.FullName
            Number   = $fileNumber
        }

        # Write updated content back to file
        $updatedContent | Set-Content -Path $file.FullName -Encoding UTF8

        Write-Host "  ✓ Updated headings for file #$fileNumber" -ForegroundColor Green
        $fileNumber++
    }

    # PHASE 3.5: Post-process anchor references with updated heading information
    Update-PostRenumberingAnchors -FileInventory $fileInventory -NewFileNumbers $newFileNumbers -UpdatedHeadings $allUpdatedHeadings

    # PHASE 4: Generate table of contents
    Write-Host "Phase 4: Generating table of contents..." -ForegroundColor Magenta
    $tocContent = TableOfContents -AllTocEntries $allTocEntries -FileInfos $fileInfos -TocFilePath $TocFile

    # Write TOC file
    $tocContent | Set-Content -Path $TocFile -Encoding UTF8

    Write-Host "✓ Successfully processed $($files.Count) files with reference handling" -ForegroundColor Green
    Write-Host "✓ Generated table of contents: $TocFile" -ForegroundColor Green
    Write-Host "Architecture documentation renumbering with reference handling completed!" -ForegroundColor Green

}
catch {
    Write-Error "Error: $($_.Exception.Message)"
    Write-Error "Stack trace: $($_.ScriptStackTrace)"
    exit 1
}
