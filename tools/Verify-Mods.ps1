param(
    [string]$GameDir = $env:PROBABLY_STOLEN_DIR
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$tools = Join-Path $root ".tools"
$verifyDir = Join-Path $tools "verify"
$roslyn = Join-Path $tools "roslyn\csc.exe"
$dse = Join-Path $verifyDir "DseCecil.dll"
$netstandard = Join-Path $verifyDir "netstandard.dll"
$exe = Join-Path $verifyDir "VerifyMods.exe"

if (-not $GameDir) {
    $GameDir = "C:\Program Files (x86)\Steam\steamapps\common\Probably Stolen"
}

if (-not (Test-Path $roslyn) -or -not (Test-Path $dse) -or -not (Test-Path $netstandard)) {
    Write-Host "Toolchain incomplete, running Setup-Toolchain.ps1..."
    & (Join-Path $PSScriptRoot "Setup-Toolchain.ps1") -GameDir $GameDir
}

$source = Join-Path $PSScriptRoot "VerifyMods\Program.cs"
$args = @(
    "/nologo",
    "/target:exe",
    "/langversion:latest",
    "/optimize+",
    "/nowarn:1701,1702",
    "/out:$exe",
    "/reference:$dse",
    "/reference:$netstandard",
    "/reference:System.Xml.Linq.dll",
    "/reference:System.Core.dll",
    $source
)
Write-Host "Compiling VerifyMods..."
& $roslyn @args
if ($LASTEXITCODE -ne 0) {
    throw "Verifier compilation failed (exit $LASTEXITCODE)"
}

Push-Location $root
try {
    & $exe $root $GameDir
    exit $LASTEXITCODE
}
finally {
    Pop-Location
}
