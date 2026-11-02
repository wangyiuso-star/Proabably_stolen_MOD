param(
    [string]$BackupFile = $null,
    [string]$GameDir = $env:PROBABLY_STOLEN_DIR
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
if (-not $GameDir) {
    $GameDir = "C:\Program Files (x86)\Steam\steamapps\common\Probably Stolen"
}
$saveFile = Join-Path $env:USERPROFILE "AppData\LocalLow\Questing Goose Studio\Probably Stolen\SaveFile.es3"
if (-not (Test-Path $saveFile)) {
    throw "SaveFile.es3 not found: $saveFile"
}
$running = Get-Process -Name "Probably Stolen" -ErrorAction SilentlyContinue
if ($running) {
    throw "Close the game before restoring SaveFile.es3."
}

if (-not $BackupFile) {
    $BackupFile = Get-ChildItem (Join-Path $root "backup\SaveFile") -Filter "*.es3" -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1 -ExpandProperty FullName
}
if (-not $BackupFile -or -not (Test-Path $BackupFile)) {
    throw "No backup file found. Pass -BackupFile explicitly."
}

$safety = Join-Path $root ("backup\SaveFile\BeforeRestore_" + (Get-Date -Format "yyyyMMdd-HHmmss") + ".es3")
Copy-Item $saveFile $safety -Force
Copy-Item $BackupFile $saveFile -Force

$before = (Get-FileHash $BackupFile -Algorithm SHA256).Hash
$after = (Get-FileHash $saveFile -Algorithm SHA256).Hash
Write-Host "Restored $saveFile"
Write-Host "  from   : $BackupFile"
Write-Host "  safety : $safety"
Write-Host "  before : $before"
Write-Host "  after  : $after"
if ($before -ne $after) {
    throw "Restore verification failed (hash mismatch)"
}
Write-Host "Restore OK"
