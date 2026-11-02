param(
    [string]$GameDir = $env:PROBABLY_STOLEN_DIR,
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$tools = Join-Path $root ".tools"
$roslyn = Join-Path $tools "roslyn\csc.exe"
$dist = Join-Path $root "dist"

if (-not $GameDir) {
    $GameDir = "C:\Program Files (x86)\Steam\steamapps\common\Probably Stolen"
}
if (-not (Test-Path $roslyn)) {
    throw "Roslyn csc.exe not found at $roslyn. Run tools/Setup-Toolchain.ps1 first."
}
$managed = Join-Path $GameDir "Probably Stolen_Data\Managed"
if (-not (Test-Path (Join-Path $managed "Assembly-CSharp.dll"))) {
    throw "Game Managed folder not found: $managed (set PROBABLY_STOLEN_DIR)"
}

New-Item -ItemType Directory -Force -Path $dist | Out-Null

$references = @(
    (Join-Path $managed "Assembly-CSharp.dll"),
    (Join-Path $managed "0Harmony.dll"),
    (Join-Path $managed "UnityEngine.CoreModule.dll"),
    (Join-Path $managed "UnityEngine.UIModule.dll"),
    (Join-Path $managed "UnityEngine.UI.dll"),
    (Join-Path $managed "UnityEngine.TextCoreTextEngineModule.dll"),
    (Join-Path $managed "UnityEngine.TextCoreFontEngineModule.dll"),
    (Join-Path $managed "UnityEngine.TextRenderingModule.dll"),
    (Join-Path $managed "Unity.TextMeshPro.dll"),
    (Join-Path $managed "netstandard.dll")
)
foreach ($reference in $references) {
    if (-not (Test-Path $reference)) {
        throw "Missing reference assembly: $reference"
    }
}

$optimize = if ($Configuration -eq "Release") { "/optimize+" } else { "/optimize-" }
$projects = @(
    @{ Name = "AppraisalAssistant"; Source = "src\AppraisalAssistant\AppraisalAssistant.cs" },
    @{ Name = "AugSpotter"; Source = "src\AugSpotter\AugSpotter.cs" }
)

foreach ($project in $projects) {
    $source = Join-Path $root $project.Source
    $outDir = Join-Path $dist $project.Name
    New-Item -ItemType Directory -Force -Path $outDir | Out-Null
    $dll = Join-Path $outDir ($project.Name + ".dll")
    $args = @(
        "/nologo",
        "/target:library",
        "/langversion:latest",
        "/codepage:65001",
        $optimize,
        "/deterministic+",
        "/nowarn:1701,1702",
        "/out:$dll"
    )
    foreach ($reference in $references) {
        $args += "/reference:$reference"
    }
    $args += $source
    Write-Host "Compiling $($project.Name)..."
    & $roslyn @args
    if ($LASTEXITCODE -ne 0) {
        throw "Compilation failed for $($project.Name) (exit $LASTEXITCODE)"
    }
    Copy-Item (Join-Path $root "src\$($project.Name)\manifest.xml") (Join-Path $outDir "manifest.xml") -Force
    $size = (Get-Item $dll).Length
    if ($size -le 0) {
        throw "Output DLL is empty: $dll"
    }
    Write-Host "  -> $dll ($size bytes)"
}

Write-Host "Build OK: $dist"
