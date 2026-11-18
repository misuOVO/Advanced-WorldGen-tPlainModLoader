# Build the Advanced World Generation port (seed options) for tPlainModLoader
param([switch]$Run)

$ErrorActionPreference = 'Stop'

$root    = Split-Path -Parent $MyInvocation.MyCommand.Path
$srcDir  = Join-Path $root 'src'
$modTpl  = Join-Path $root 'mod'
$outDir  = Join-Path $root 'dist'
$gameDir = 'E:\SteamLibrary\steamapps\common\Terraria'
$modsDir = Join-Path $gameDir 'Mods\AdvancedWorldGen'
$modName = 'AdvancedWorldGen'

$csc = 'C:\Program Files\dotnet\sdk\8.0.426\Roslyn\bincore\csc.dll'
if (-not (Test-Path $csc)) {
    $csc = (Get-ChildItem 'C:\Program Files\dotnet\sdk' -Directory |
            Sort-Object Name -Descending |
            ForEach-Object { Join-Path $_.FullName 'Roslyn\bincore\csc.dll' } |
            Where-Object { Test-Path $_ } |
            Select-Object -First 1)
}
if (-not $csc) { throw 'Roslyn csc.dll not found. Please install the .NET SDK.' }

$fw  = 'C:\Windows\Microsoft.NET\Framework\v4.0.30319'
$xna = 'C:\Windows\Microsoft.NET\assembly\GAC_32'

# Terraria 1.4.5.8 embeds ReLogic inside Terraria.exe
# (Terraria.Libraries.ReLogic.ReLogic.dll). If ReLogic.dll is not on disk,
# extract it from the exe resources and use that copy as a reference.
$reLogic = Join-Path $gameDir 'ReLogic.dll'
if (-not (Test-Path $reLogic)) {
    $refDir = Join-Path $outDir 'refs'
    New-Item -ItemType Directory -Force -Path $refDir | Out-Null
    $reLogic = Join-Path $refDir 'ReLogic.dll'

    if (-not (Test-Path $reLogic)) {
        Write-Host 'Extracting embedded ReLogic.dll from Terraria.exe ...'
        $exe = Join-Path $gameDir 'Terraria.exe'
        $ps32 = Join-Path $env:WINDIR 'SysWOW64\WindowsPowerShell\v1.0\powershell.exe'
        $extract = Join-Path $env:TEMP 'awg_extract_relogic.ps1'

        $lines = @(
            "`$asm = [Reflection.Assembly]::LoadFrom('$exe')",
            "`$s = `$asm.GetManifestResourceStream('Terraria.Libraries.ReLogic.ReLogic.dll')",
            "if (`$s -eq `$null) { throw 'resource not found' }",
            "`$f = [System.IO.File]::Create('$reLogic')",
            "`$s.CopyTo(`$f)",
            "`$f.Close()",
            "`$s.Close()"
        )

        Set-Content -Path $extract -Value $lines -Encoding ASCII
        & $ps32 -NoProfile -ExecutionPolicy Bypass -File $extract
        if (-not (Test-Path $reLogic)) { throw 'Failed to extract ReLogic.dll' }
    }
}

$refs = @(
    (Join-Path $fw 'mscorlib.dll'),
    (Join-Path $fw 'System.dll'),
    (Join-Path $fw 'System.Core.dll'),
    (Join-Path $fw 'System.Xml.dll'),
    (Join-Path $fw 'System.Drawing.dll'),
    (Join-Path $gameDir 'Terraria.exe'),
    (Join-Path $gameDir 'tContentPatch.dll'),
    (Join-Path $gameDir 'CommandHelp.dll'),
    (Join-Path $gameDir '0Harmony.dll'),
    $reLogic,
    (Join-Path $gameDir 'Newtonsoft.Json.dll'),
    (Join-Path $xna 'Microsoft.Xna.Framework\v4.0_4.0.0.0__842cf8be1de50553\Microsoft.Xna.Framework.dll'),
    (Join-Path $xna 'Microsoft.Xna.Framework.Game\v4.0_4.0.0.0__842cf8be1de50553\Microsoft.Xna.Framework.Game.dll'),
    (Join-Path $xna 'Microsoft.Xna.Framework.Graphics\v4.0_4.0.0.0__842cf8be1de50553\Microsoft.Xna.Framework.Graphics.dll')
)

foreach ($r in $refs) {
    if (-not (Test-Path $r)) { throw "Missing reference: $r" }
}

New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$sources = Get-ChildItem -Path $srcDir -Filter '*.cs' -Recurse | ForEach-Object { $_.FullName }
if (-not $sources) { throw 'No .cs files found under src' }

$dll = Join-Path $outDir "$modName.dll"
if (Test-Path $dll) { Remove-Item $dll -Force }

$argList = @(
    '/nologo',
    '/target:library',
    '/platform:x86',
    '/optimize+',
    '/nostdlib+',
    '/langversion:latest',
    '/codepage:65001',
    "/out:$dll"
)
foreach ($r in $refs) { $argList += "/reference:$r" }

$argList += $sources

# embed the mod icon so the world-creation button can draw it
$ico = Join-Path $root 'assets\ico.png'
if (Test-Path $ico) {
    $argList += "/resource:$ico,AdvancedWorldGen.Assets.ico.png"
}

& dotnet $csc @argList
if ($LASTEXITCODE -ne 0) { throw "Build failed (exit $LASTEXITCODE)" }

Write-Host "Build OK: $dll" -ForegroundColor Green

New-Item -ItemType Directory -Force -Path $modsDir | Out-Null
Copy-Item $dll (Join-Path $modsDir "$modName.dll") -Force

if (Test-Path $modTpl) {
    Copy-Item (Join-Path $modTpl '*.json') $modsDir -Force
}

$ico = Join-Path $root 'assets\ico.png'
if (Test-Path $ico) {
    Copy-Item $ico (Join-Path $modsDir 'ico.png') -Force
}

Write-Host "Deployed to: $modsDir" -ForegroundColor Green
