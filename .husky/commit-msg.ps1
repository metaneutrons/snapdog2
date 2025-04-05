#!/usr/bin/env pwsh
# Commit message validation hook for SnapDog
param(
    [Parameter(Mandatory = $true)]
    [string]$CommitMessageFile
)

# Get the commit message from the file
$commitMsg = Get-Content -Path $CommitMessageFile -Raw

# Define the pattern (using a conventional commit format)
# Format: type(scope): description
# Types: feat, fix, docs, style, refactor, test, chore, etc.
# Only check the first line of the commit message (allow multi-line commits)
$firstLine = $commitMsg.Split([Environment]::NewLine, [StringSplitOptions]::RemoveEmptyEntries)[0]
$pattern = '^(feat|fix|docs|style|refactor|test|chore|build|ci|perf|revert)(\([a-z0-9-]+\))?: .+'

# Check if the first line of the commit message matches the pattern
if ($firstLine -notmatch $pattern) {
    Write-Host "❌ Commit message header does not follow the conventional commit format." -ForegroundColor Red
    Write-Host "Required format for the FIRST LINE: <type>(optional scope): <description>" -ForegroundColor Yellow
    Write-Host "Types: feat, fix, docs, style, refactor, test, chore, build, ci, perf, revert" -ForegroundColor Yellow
    Write-Host "Example: feat(api): add support for bookmarks endpoint" -ForegroundColor Yellow
    Write-Host "" -ForegroundColor Yellow
    Write-Host "The rest of the commit message can be as verbose as you like!" -ForegroundColor Green
    exit 1
}

# Check for Claude signature in commit message
$claudeSignature = "🤖 Generated with [Claude Code]" 
$coAuthoredByClause = "Co-Authored-By: Claude <noreply@anthropic.com>"

if ($commitMsg -match [regex]::Escape($claudeSignature) -or $commitMsg -match [regex]::Escape($coAuthoredByClause)) {
    Write-Host "❌ ERROR: Commit message contains prohibited Claude signature:" -ForegroundColor Red
    Write-Host "   Detected:" -ForegroundColor Red
    
    if ($commitMsg -match [regex]::Escape($claudeSignature)) {
        Write-Host "   - '$claudeSignature'" -ForegroundColor Red
    }
    
    if ($commitMsg -match [regex]::Escape($coAuthoredByClause)) {
        Write-Host "   - '$coAuthoredByClause'" -ForegroundColor Red
    }
    
    Write-Host "" -ForegroundColor Red
    Write-Host "   Claude signatures and co-authoring references are not allowed in this repository." -ForegroundColor Red
    Write-Host "   Please remove these lines from your commit message and try again." -ForegroundColor Red
    exit 1
}

Write-Host "✅ Commit message follows the conventional commit format. Verbose messages welcome!" -ForegroundColor Green