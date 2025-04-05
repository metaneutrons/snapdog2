#!/usr/bin/env pwsh
# Pre-commit hook for SnapDog

# Format code with CSharpier
Write-Host "🎨 Running CSharpier to format code..." -ForegroundColor Cyan
dotnet csharpier .

# Verify that all files have GPL-3.0 headers
#Write-Host "📝 Checking for GPL-3.0 headers..." -ForegroundColor Cyan
#$missingHeaders = Get-ChildItem -Path ./SnapDog -Include *.cs -Recurse -File |
#Where-Object { $_.FullName -notlike "*/obj/*" -and $_.FullName -notlike "*/bin/*" } |
#Where-Object { $_.Name -ne "SnapDog.AssemblyInfo.cs" -and $_.Name -ne "SnapDog.GlobalUsings.g.cs" } |
#Where-Object { (Get-Content $_.FullName -Raw) -notlike "*GNU General Public License*" } |
#Select-Object -ExpandProperty FullName

#if ($missingHeaders) {
#    Write-Host "⚠️ The following files are missing GPL-3.0 headers:" -ForegroundColor Yellow
#    $missingHeaders | ForEach-Object { Write-Host "   $_" -ForegroundColor Yellow }
#    Write-Host ""
#    Write-Host "Running Fix-StyleCopIssues.ps1 to add headers..." -ForegroundColor Yellow
#    pwsh -File "scripts/Fix-StyleCopIssues.ps1" -Fix FileHeader -SourceDirectory "./SnapDog"
#    Write-Host "✅ Headers added. Please review the changes." -ForegroundColor Green
#}

# Check for shell scripts (.sh files) in the staged files (excluding .devcontainer and docker directories)
Write-Host "🔍 Checking for shell script files..." -ForegroundColor Cyan
$shellScripts = git diff --cached --name-only --diff-filter=ACM | 
    Select-String -Pattern '\.sh$' | 
    Where-Object { $_ -notmatch '\.devcontainer/' -and $_ -notmatch 'docker/' }

if ($shellScripts) {
    Write-Host "❌ ERROR: Shell script files detected in commit:" -ForegroundColor Red
    $shellScripts | ForEach-Object { Write-Host "   $_" -ForegroundColor Red }
    Write-Host "This project only supports PowerShell scripts (.ps1) outside of the .devcontainer and docker directories. Please convert shell scripts to PowerShell or remove them." -ForegroundColor Red
    exit 1
}

# Run formatting and style checks but don't block commits
Write-Host "📋 Running code style verification..." -ForegroundColor Cyan
dotnet build SnapDog/SnapDog.csproj /p:TreatWarningsAsErrors=false

# Check for potential secrets
Write-Host "🔐 Checking for potential secrets or credentials..." -ForegroundColor Cyan
# Define pattern with double quotes and proper escaping
$secretsPattern = '(password|secret|key|token|credential).*=.*["''][0-9a-zA-Z]{16,}["'']'
$secrets = Get-ChildItem -Path ./SnapDog -Include *.cs, *.json, *.xml -Recurse -File |
Where-Object { $_.FullName -notlike "*/obj/*" -and $_.FullName -notlike "*/bin/*" } |
Select-String -Pattern $secretsPattern

if ($secrets) {
    Write-Host "⚠️ Potential secrets or credentials found:" -ForegroundColor Yellow
    $secrets | ForEach-Object { Write-Host "   $_" -ForegroundColor Yellow }
    Write-Host "Please verify these are not actual secrets before committing." -ForegroundColor Yellow
    exit 1
}

# Check for Claude signature in staged changes - we can't check the commit message in pre-commit
# as it runs before the commit message is finalized, but we can check staged files
Write-Host "🔍 Checking for prohibited Claude signature in staged changes..." -ForegroundColor Cyan
$stagedChanges = git diff --cached
$claudeSignature = "🤖 Generated with [Claude Code]" 
$coAuthoredByClause = "Co-Authored-By: Claude <noreply@anthropic.com>"

if ($stagedChanges -match [regex]::Escape($claudeSignature) -or $stagedChanges -match [regex]::Escape($coAuthoredByClause)) {
    Write-Host "❌ ERROR: Staged changes contain prohibited Claude signature:" -ForegroundColor Red
    Write-Host "   Claude signatures and co-authoring references are not allowed in this repository." -ForegroundColor Red
    Write-Host "   Please remove the Claude signature and co-authoring lines from your changes." -ForegroundColor Red
    exit 1
}

# Success message
Write-Host "✅ Pre-commit checks passed!" -ForegroundColor Green
