# Enable Large Address Aware (LAA) for Terraria / tPlainModLoader
#
# A 32-bit process normally only gets 2 GB of address space. On 64-bit Windows,
# setting the LAA flag in the PE header allows up to ~4 GB, which greatly helps
# big worlds (Advanced WorldGen custom sizes, Calamity, etc.).
#
# IMPORTANT:
#   If you launch the game through tPlainModLoader.exe, the game actually runs
#   inside tPlainModLoader.exe, so THAT exe needs the LAA flag. This script
#   patches Terraria.exe, tPlainModLoader.exe and tPlainModLoaderInjector.exe
#   (whichever exist) so it works no matter how you start the game.
#
# Usage:
#   powershell -ExecutionPolicy Bypass -File .\enable-laa.ps1
#   powershell -ExecutionPolicy Bypass -File .\enable-laa.ps1 -GameDir "E:\SteamLibrary\steamapps\common\Terraria"
#
# Close the game first (the exe is locked while running).
# A backup is written next to each exe as <name>.bak.
# Steam "verify integrity" may restore the original Terraria.exe, just run this again afterwards.

param(
    [string]$GameDir = 'E:\SteamLibrary\steamapps\common\Terraria'
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path $GameDir)) { throw "Game folder not found: $GameDir" }

$targets = @('Terraria.exe', 'tPlainModLoader.exe', 'tPlainModLoaderInjector.exe')
$changed = 0

function Get-LaaState([byte[]]$bytes, [string]$path) {
    if ($bytes.Length -lt 0x40) { throw "Not a PE file: $path" }

    $peOffset = [BitConverter]::ToInt32($bytes, 0x3C)
    if ($peOffset -le 0 -or $peOffset + 0x18 -ge $bytes.Length) { throw "Bad PE header: $path" }
    if ([BitConverter]::ToUInt32($bytes, $peOffset) -ne 0x00004550) { throw "Missing PE signature: $path" }

    $charOffset = $peOffset + 0x16
    $chars = [BitConverter]::ToUInt16($bytes, $charOffset)
    return @{ Offset = $charOffset; Chars = $chars; Laa = (($chars -band 0x0020) -ne 0) }
}

foreach ($name in $targets) {
    $path = Join-Path $GameDir $name
    if (-not (Test-Path $path)) {
        Write-Host "[skip] $name (not found)"
        continue
    }

    try {
        $bytes = [System.IO.File]::ReadAllBytes($path)
    } catch {
        Write-Host "[locked] $name - close the game and run this again" -ForegroundColor Yellow
        continue
    }

    $state = Get-LaaState $bytes $path

    if ($state.Laa) {
        Write-Host "[ok]   $name already Large Address Aware (0x$('{0:X4}' -f $state.Chars))"
        continue
    }

    $backup = "$path.bak"
    if (-not (Test-Path $backup)) {
        Copy-Item $path $backup -Force
        Write-Host "[bak]  $backup"
    }

    $newChars = [uint16]($state.Chars -bor 0x0020)
    [BitConverter]::GetBytes($newChars).CopyTo($bytes, $state.Offset)

    try {
        [System.IO.File]::WriteAllBytes($path, $bytes)
    } catch {
        Write-Host "[locked] $name - close the game and run this again" -ForegroundColor Yellow
        continue
    }

    $changed++
    Write-Host ("[done] {0}: 0x{1:X4} -> 0x{2:X4}" -f $name, $state.Chars, $newChars) -ForegroundColor Green
}

Write-Host ""
if ($changed -gt 0) {
    Write-Host "LAA enabled on $changed file(s). Restart the game." -ForegroundColor Green
    Write-Host "32-bit process can now use up to ~4 GB instead of ~2 GB."
} else {
    Write-Host "Nothing changed. If the game was running, close it and run this again."
}
