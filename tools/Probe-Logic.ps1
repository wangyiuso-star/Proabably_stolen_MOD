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
$mono = Join-Path $tools "mono\Mono\bin\mono.exe"
if (-not (Test-Path $roslyn)) {
    throw "Run tools/Setup-Toolchain.ps1 first"
}
if (-not (Test-Path $mono)) {
    throw "Mono not found at $mono. Download/extract Mono 6.12 or run tools/Probe-Patches.ps1 only."
}
$outDir = Join-Path $tools "logicprobe"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
$exe = Join-Path $outDir "LogicProbe.exe"
$source = Join-Path $PSScriptRoot "LogicProbe\Program.cs"

& $roslyn /nologo /target:exe /langversion:latest /optimize+ /nowarn:1701,1702 /out:$exe /reference:System.Core.dll $source
if ($LASTEXITCODE -ne 0) {
    throw "LogicProbe compilation failed"
}

Remove-Item Env:\MONO_PATH -ErrorAction SilentlyContinue
& $mono $exe $managed `
    (Join-Path $root "dist\AppraisalAssistant\AppraisalAssistant.dll") `
    (Join-Path $root "dist\AugSpotter\AugSpotter.dll")
exit $LASTEXITCODE

