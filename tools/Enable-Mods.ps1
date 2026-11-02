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

$json = [System.IO.File]::ReadAllText($SaveFile)
$save = $json | ConvertFrom-Json
if ($null -eq $save) {
    throw "Could not parse SaveFile.es3"
}

$recordStore = [ordered]@{ records = @(); stopped = @() }
if ($save.PSObject.Properties.Name -contains "modTrust") {
    $raw = $save.modTrust.value
    if (-not [string]::IsNullOrWhiteSpace($raw)) {
        $parsed = $raw | ConvertFrom-Json
        if ($parsed) {
            if ($parsed.PSObject.Properties.Name -contains "records" -and $parsed.records) {
                $recordStore.records = @($parsed.records)
            }
            if ($parsed.PSObject.Properties.Name -contains "stopped" -and $parsed.stopped) {
                $recordStore.stopped = @($parsed.stopped)
            }
        }
    }
}

$order = @()
if ($save.PSObject.Properties.Name -contains "modOrder") {
    $order = @($save.modOrder.value)
}
$order = @($order | Where-Object { $_ })

$ids = @("deepspace.appraisal_assistant", "deepspace.aug_spotter")
foreach ($id in $ids) {
    if ($order -notcontains $id) {
        $order += $id
    }
}

$records = @()
foreach ($existing in @($recordStore.records)) {
    if ($existing -and $existing.id -and $existing.id -ne "deepspace.appraisal_assistant" -and $existing.id -ne "deepspace.aug_spotter") {
        $records += $existing
    }
}

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
    $now = [DateTime]::UtcNow.ToString("o")
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

$recordStore.records = @($records)
$recordStore.stopped = @($recordStore.stopped)

$trustJson = $recordStore | ConvertTo-Json -Compress -Depth 20
if ($save.PSObject.Properties.Name -contains "modTrust") {
    $save.modTrust.value = $trustJson
} else {
    $save | Add-Member -NotePropertyName "modTrust" -NotePropertyValue ([ordered]@{ __type = "string"; value = $trustJson }) -Force
}
$listType = "System.Collections.Generic.List``1[[System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]],mscorlib"
if ($save.PSObject.Properties.Name -contains "modOrder") {
    $save.modOrder.value = @($order)
} else {
    $save | Add-Member -NotePropertyName "modOrder" -NotePropertyValue ([ordered]@{ __type = $listType; value = @($order) }) -Force
}

$outJson = $save | ConvertTo-Json -Compress -Depth 50
$utf8 = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText($SaveFile, $outJson, $utf8)
Write-Host "Enabled mods in $SaveFile"
Write-Host ("modOrder = " + ($order -join ", "))
