# Enable Large Address Aware (LAA) for Terraria.exe
#
# A 32-bit process normally only gets 2 GB of address space. On 64-bit Windows,
# setting the LAA flag in the PE header allows up to ~4 GB, which greatly helps
# big worlds (Advanced WorldGen custom sizes, Calamity, etc.).
#
# Usage:
#   powershell -ExecutionPolicy Bypass -File .\enable-laa.ps1
#   powershell -ExecutionPolicy Bypass -File .\enable-laa.ps1 -Path "D:\...\Terraria.exe"
#
# Close the game before running. A backup is written next to the exe as Terraria.exe.bak.
# Steam "verify integrity" may restore the original exe, just run this again afterwards.

param(
    [string]$Path = 'D:\steam\steamapps\common\Terraria\Terraria.exe'
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path $Path)) { throw "Not found: $Path" }

$bytes = [System.IO.File]::ReadAllBytes($Path)
if ($bytes.Length -lt 0x40) { throw "Not a PE file?" }

$peOffset = [BitConverter]::ToInt32($bytes, 0x3C)
if ($peOffset -le 0 -or $peOffset + 0x18 -ge $bytes.Length) { throw "Bad PE header" }

if ([BitConverter]::ToUInt32($bytes, $peOffset) -ne 0x00004550) { throw "Missing PE signature" } # 'PE\0\0'

$charOffset = $peOffset + 0x16
$chars = [BitConverter]::ToUInt16($bytes, $charOffset)

if (($chars -band 0x0020) -ne 0) {
    Write-Host "Already Large Address Aware: $Path"
    exit 0
}

$backup = "$Path.bak"
if (-not (Test-Path $backup)) {
    Copy-Item $Path $backup -Force
    Write-Host "Backup: $backup"
}

$newChars = [uint16]($chars -bor 0x0020)
[BitConverter]::GetBytes($newChars).CopyTo($bytes, $charOffset)
[System.IO.File]::WriteAllBytes($Path, $bytes)

Write-Host ("Done. Characteristics 0x{0:X4} -> 0x{1:X4}" -f $chars, $newChars)
Write-Host "Restart the game. 32-bit process can now use up to ~4 GB instead of ~2 GB."
