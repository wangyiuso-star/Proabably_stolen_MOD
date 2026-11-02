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
$dseCecil = Join-Path $managed "DseCecil.dll"
if (-not (Test-Path $dseCecil)) {
    throw "DseCecil.dll not found in $managed (set PROBABLY_STOLEN_DIR)"
}

if (-not (Test-Path $roslyn)) {
    Write-Host "Downloading Roslyn compiler toolset (MIT) from NuGet..."
    $python = @'
import io, os, urllib.request, zipfile
root = os.path.abspath(".tools")
os.makedirs(os.path.join(root, "nuget"), exist_ok=True)
os.makedirs(os.path.join(root, "roslyn"), exist_ok=True)
url = "https://api.nuget.org/v3-flatcontainer/microsoft.net.compilers.toolset/4.8.0/microsoft.net.compilers.toolset.4.8.0.nupkg"
data = urllib.request.urlopen(url, timeout=180).read()
z = zipfile.ZipFile(io.BytesIO(data))
for name in z.namelist():
    if not name.startswith("tasks/net472/"):
        continue
    rel = name[len("tasks/net472/"):]
    if not rel:
        continue
    dst = os.path.join(root, "roslyn", rel)
    os.makedirs(os.path.dirname(dst), exist_ok=True)
    with open(dst, "wb") as f:
        f.write(z.read(name))
print("Roslyn extracted")
'@
    $python | Set-Content -Encoding UTF8 (Join-Path $tools "download-roslyn.py")
    Push-Location $root
    try {
        & python (Join-Path $tools "download-roslyn.py")
        if ($LASTEXITCODE -ne 0) { throw "Roslyn download failed" }
    }
    finally {
        Pop-Location
    }
}

New-Item -ItemType Directory -Force -Path (Join-Path $tools "verify") | Out-Null
Copy-Item $dseCecil (Join-Path $tools "verify\DseCecil.dll") -Force
Copy-Item (Join-Path $managed "netstandard.dll") (Join-Path $tools "verify\netstandard.dll") -Force
Copy-Item (Join-Path $managed "Assembly-CSharp.dll") (Join-Path $tools "verify\Assembly-CSharp.dll") -Force

Write-Host "Toolchain OK:"
Write-Host "  $roslyn"
Write-Host "  $tools\verify\DseCecil.dll"
