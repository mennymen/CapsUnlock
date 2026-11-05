$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot "windows\CapsUnlock.Hotkeys\CapsUnlock.Hotkeys.csproj"
$compilerCandidates = @(
    (Join-Path $env:LOCALAPPDATA "Programs\Inno Setup 6\ISCC.exe"),
    (Join-Path $env:ProgramFiles "Inno Setup 6\ISCC.exe"),
    (Join-Path ${env:ProgramFiles(x86)} "Inno Setup 6\ISCC.exe")
)
$compiler = Get-Command ISCC.exe -ErrorAction SilentlyContinue
if ($compiler) {
    $compilerPath = $compiler.Source
} else {
    $compilerPath = $compilerCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
}

if (-not $compilerPath) {
    throw "Inno Setup 6 is required to build the installer. Install it from https://jrsoftware.org/isdl.php"
}

dotnet publish $project --configuration Release --runtime win-x64 --self-contained true
if ($LASTEXITCODE -ne 0) {
    throw "Publishing CapsUnlock failed with exit code $LASTEXITCODE."
}

$outputDirectory = Join-Path $repoRoot "dist"
New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
& $compilerPath (Join-Path $repoRoot "installer\CapsUnlock.iss")
if ($LASTEXITCODE -ne 0) {
    throw "Building the CapsUnlock installer failed with exit code $LASTEXITCODE."
}

Write-Host "Installer created in $outputDirectory"
