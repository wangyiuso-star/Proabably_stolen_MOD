param(
    [string]$GameDir = $env:PROBABLY_STOLEN_DIR
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$tools = Join-Path $root ".tools"
$roslyn = Join-Path $tools "roslyn\csc.exe"
if (-not $GameDir) {
    $GameDir = "C:\Program Files (x86)\Steam\steamapps\common\Probably Stolen"
}
$managed = Join-Path $GameDir "Probably Stolen_Data\Managed"
if (-not (Test-Path $roslyn)) {
    throw "Run tools/Setup-Toolchain.ps1 first"
}
$outDir = Join-Path $tools "harmonyprobe"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
Copy-Item (Join-Path $managed "0Harmony.dll") $outDir -Force
Copy-Item (Join-Path $managed "netstandard.dll") $outDir -Force
$exe = Join-Path $outDir "HarmonyProbe.exe"
$source = Join-Path $PSScriptRoot "HarmonyProbe\Program.cs"

$harmonyRef = "/reference:" + (Join-Path $managed "0Harmony.dll")
$netstandardRef = "/reference:" + (Join-Path $managed "netstandard.dll")
$args = @(
    "/nologo",
    "/target:exe",
    "/langversion:latest",
    "/optimize+",
    "/nowarn:1701,1702",
    "/out:$exe",
    $harmonyRef,
    $netstandardRef,
    "/reference:System.Core.dll",
    $source
)
& $roslyn @args
if ($LASTEXITCODE -ne 0) {
    throw "HarmonyProbe compilation failed"
}

$modArgs = @(
    (Join-Path $root "dist\AppraisalAssistant\AppraisalAssistant.dll"),
    (Join-Path $root "dist\AugSpotter\AugSpotter.dll")
)
$mono = Join-Path $tools "mono\Mono\bin\mono.exe"
if (Test-Path $mono) {
    Write-Host "Running HarmonyProbe under Mono..."
    & $mono $exe $managed @modArgs
} else {
    Write-Host "Mono not found; running under .NET Framework (MonoBehaviour patches may fail with ECall errors)."
    & $exe $managed @modArgs
}
exit $LASTEXITCODE
