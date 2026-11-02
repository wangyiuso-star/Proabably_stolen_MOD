param(
    [string]$GameDir = $env:PROBABLY_STOLEN_DIR,
    [switch]$Enable,
    [switch]$NoBackup
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

if (-not $GameDir) {
    $GameDir = "C:\Program Files (x86)\Steam\steamapps\common\Probably Stolen"
}
$modsDir = Join-Path $GameDir "Mods"
if (-not (Test-Path (Join-Path $GameDir "Probably Stolen.exe"))) {
    throw "Game not found at $GameDir (set PROBABLY_STOLEN_DIR)"
}
if (-not (Test-Path (Join-Path $root "dist"))) {
    throw "dist not found; run tools/Build-Mods.ps1 first"
}

New-Item -ItemType Directory -Force -Path $modsDir | Out-Null

foreach ($name in @("AppraisalAssistant", "AugSpotter")) {
    $source = Join-Path $root "dist\$name"
    $target = Join-Path $modsDir $name
    if (-not (Test-Path $source)) {
        throw "Missing build output: $source"
    }
    if (Test-Path $target) {
        if (-not $NoBackup) {
            $backup = Join-Path $root ("backup\Mods\" + $name + "_" + (Get-Date -Format "yyyyMMdd-HHmmss"))
            New-Item -ItemType Directory -Force -Path (Split-Path -Parent $backup) | Out-Null
            Copy-Item $target $backup -Recurse -Force
            Write-Host "Backed up existing ${name} to $backup"
        }
        Remove-Item $target -Recurse -Force
    }
    Copy-Item $source $target -Recurse -Force
    Write-Host "Installed $name -> $target"
}

if ($Enable) {
    & (Join-Path $PSScriptRoot "Enable-Mods.ps1") -GameDir $GameDir
}

Write-Host "Done. Open the in-game Mods panel and enable both mods (approve them if prompted)."
