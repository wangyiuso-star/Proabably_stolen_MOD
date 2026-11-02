param(
    [string]$GameDir = $env:PROBABLY_STOLEN_DIR,
    [string]$SaveFile = $null
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

if (-not $GameDir) {
    $GameDir = "C:\Program Files (x86)\Steam\steamapps\common\Probably Stolen"
}
$modsDir = Join-Path $GameDir "Mods"
if (-not $SaveFile) {
    $SaveFile = Join-Path $env:USERPROFILE "AppData\LocalLow\Questing Goose Studio\Probably Stolen\SaveFile.es3"
}
if (-not (Test-Path $SaveFile)) {
    throw "SaveFile.es3 not found: $SaveFile"
}
$running = Get-Process -Name "Probably Stolen" -ErrorAction SilentlyContinue
if ($running) {
    throw "Close the game before changing its mod trust/config file."
}

function Get-Sha256Hex([string]$Path) {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $bytes = [System.IO.File]::ReadAllBytes($Path)
        return [System.BitConverter]::ToString($sha.ComputeHash($bytes)).Replace("-", "").ToLowerInvariant()
    }
    finally {
        $sha.Dispose()
    }
}

function Get-Sha256Text([string]$Text) {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $bytes = [System.Text.Encoding]::UTF8.GetBytes($Text)
        return [System.BitConverter]::ToString($sha.ComputeHash($bytes)).Replace("-", "").ToLowerInvariant()
    }
    finally {
        $sha.Dispose()
    }
}

$backupDir = Join-Path $root "backup\SaveFile"
New-Item -ItemType Directory -Force -Path $backupDir | Out-Null
$backupPath = Join-Path $backupDir ("SaveFile_" + (Get-Date -Format "yyyyMMdd-HHmmss") + ".es3")
Copy-Item $SaveFile $backupPath -Force
Write-Host "Backed up SaveFile.es3 -> $backupPath"

$ids = @("deepspace.appraisal_assistant", "deepspace.aug_spotter")
$records = @()
$now = [DateTime]::UtcNow.ToString("o")

foreach ($name in @("AppraisalAssistant", "AugSpotter")) {
    $modDir = Join-Path $modsDir $name
    $manifest = Join-Path $modDir "manifest.xml"
    $dll = Join-Path $modDir ($name + ".dll")
    if (-not (Test-Path $manifest) -or -not (Test-Path $dll)) {
        throw "Mod not installed properly: $modDir"
    }
    $id = if ($name -eq "AppraisalAssistant") { "deepspace.appraisal_assistant" } else { "deepspace.aug_spotter" }
    $manifestHash = Get-Sha256Hex $manifest
    $dllHash = Get-Sha256Hex $dll
    $combinedInput = ($manifestHash + "`n" + $name + ".dll|" + $dllHash)
    $combined = Get-Sha256Text $combinedInput
    $records += [ordered]@{
        id = $id
        source = "Local"
        workshopId = "0"
        manifestHash = $manifestHash
        files = @($name + ".dll|" + $dllHash)
        combinedHash = $combined
        declared = @()
        detected = @()
        gameVersion = "0.46D"
        approvedAt = $now
        updatedAt = $now
        developerMode = $false
        scannerVersion = 2
    }
    Write-Host "Prepared trust record for $id ($name.dll)"
}

# ES3 stores the list type inside __type. Keep that string exact; do not
# round-trip the whole file through PowerShell's ConvertFrom-Json, because
# Windows PowerShell 5.1 drops reserved-looking "__type" properties.
$listType = "System.Collections.Generic.List``1[[System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]],mscorlib"
$payload = [ordered]@{
    save = $SaveFile
    ids = @($ids)
    listType = $listType
    records = @($records)
}
$payloadDir = Join-Path $root ".scratch"
New-Item -ItemType Directory -Force -Path $payloadDir | Out-Null
$payloadPath = Join-Path $payloadDir "enable-mods-payload.json"
$payload | ConvertTo-Json -Compress -Depth 20 | Set-Content -Encoding UTF8 $payloadPath

& python (Join-Path $PSScriptRoot "update-mod-config.py") $payloadPath
if ($LASTEXITCODE -ne 0) {
    throw "Python updater failed (exit $LASTEXITCODE)"
}
Remove-Item $payloadPath -Force -ErrorAction SilentlyContinue

Write-Host "Enabled mods in $SaveFile"
Write-Host ("modOrder = " + ($ids -join ", "))
