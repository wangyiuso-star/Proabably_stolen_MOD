param(
    [string]$GameDir = $env:PROBABLY_STOLEN_DIR
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
if (-not $GameDir) {
    $GameDir = "C:\Program Files (x86)\Steam\steamapps\common\Probably Stolen"
}
$modsDir = Join-Path $GameDir "Mods"
foreach ($name in @("AppraisalAssistant", "AugSpotter")) {
    $target = Join-Path $modsDir $name
    if (Test-Path $target) {
        $backup = Join-Path $root ("backup\Mods\" + $name + "_removed_" + (Get-Date -Format "yyyyMMdd-HHmmss"))
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $backup) | Out-Null
        Copy-Item $target $backup -Recurse -Force
        Remove-Item $target -Recurse -Force
        Write-Host "Removed $name (backup: $backup)"
    } else {
        Write-Host "$name is not installed"
    }
}
Write-Host "Mod folders removed. If the game still lists them, open the in-game Mods panel and refresh, or remove their IDs from SaveFile.es3 modOrder."
