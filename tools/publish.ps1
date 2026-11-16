# Publishes a new Cultist version to GitHub:
#   takes the mod from Desktop\Покет\Флагеллянт (the installed copy) and the sources from Desktop\Покет\Исходники\Флагеллянт,
#   refreshes install/ and source/, builds Cultist_PocketRogues_v<Version>.zip, commits, pushes and makes a Release with the zip.
# Usage:  powershell -ExecutionPolicy Bypass -File tools\publish.ps1 -Version 2.5.0 -Notes "что нового"
param(
    [Parameter(Mandatory = $true)][string]$Version,
    [string]$Notes = ""
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$git = 'C:\Program Files\Git\cmd\git.exe'
$gh = 'C:\Program Files\GitHub CLI\gh.exe'
$env:PATH = 'C:\Program Files\Git\cmd;' + $env:PATH          # gh needs git on PATH
$desk = [Environment]::GetFolderPath('Desktop')
$modSrc = Join-Path $desk 'Покет\Флагеллянт\BepInEx'
$codeSrc = Join-Path $desk 'Покет\Исходники\Флагеллянт'
if (-not (Test-Path "$modSrc\plugins\Flagellant\FlagellantMod.dll")) { throw "No mod at $modSrc" }
if (-not (Test-Path "$codeSrc\mod\src")) { throw "No sources at $codeSrc" }

function Mirror($from, $to, [string[]]$excludeDirs = @()) {
    $rc = @($from, $to, '/MIR', '/NFL', '/NDL', '/NJH', '/NJS', '/NP')
    if ($excludeDirs.Count) { $rc += '/XD'; $rc += $excludeDirs }
    & robocopy @rc | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "robocopy failed ($LASTEXITCODE): $from -> $to" }
}

# 1. the game package: the plugin folder and its config (the BepInEx loader in install/ stays as it is)
Mirror "$modSrc\plugins\Flagellant" "$repo\install\BepInEx\plugins\Flagellant"
Copy-Item "$modSrc\config\custom.pocketrogues.flagellant.cfg" "$repo\install\BepInEx\config\" -Force -ErrorAction SilentlyContinue
# 2. the sources
Mirror "$codeSrc\mod\src" "$repo\source\mod\src"
Copy-Item "$codeSrc\mod\FlagellantMod.csproj" "$repo\source\mod\" -Force
Mirror "$codeSrc\ArtGen" "$repo\source\ArtGen" @('bin', 'obj')
Copy-Item "$codeSrc\art\*" "$repo\source\art\" -Recurse -Force
Set-Content -Path "$repo\VERSION" -Value $Version -Encoding ascii

# 3. the zip for the Release (forward slashes in entry names, flat: unpack straight into the game folder)
Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem
$zip = Join-Path $env:TEMP "Cultist_PocketRogues_v$Version.zip"
if (Test-Path $zip) { Remove-Item $zip -Force }
$files = Get-ChildItem "$repo\install" -Recurse -File -Force
$z = [IO.Compression.ZipFile]::Open($zip, 'Create')
foreach ($f in $files) {
    $name = $f.FullName.Substring("$repo\install\".Length).Replace('\', '/')
    [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($z, $f.FullName, $name) | Out-Null
}
$z.Dispose()
$z = [IO.Compression.ZipFile]::OpenRead($zip); $count = $z.Entries.Count; $z.Dispose()
if ($count -ne $files.Count) { throw "zip has $count entries, install/ has $($files.Count) files" }
Write-Host "zip: $zip ($count files)"

# 4. commit, push, release
& $git -C $repo add -A
& $git -C $repo commit -m "Культист $Version" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>" | Out-Host
& $git -C $repo push origin HEAD | Out-Host
if ($LASTEXITCODE -ne 0) { throw "push failed" }
if (-not $Notes) { $Notes = "Культист $Version. Скачай zip ниже и распакуй в папку игры с заменой файлов." }
Push-Location $repo
try { & $gh release create "v$Version" $zip --title "Культист $Version" --notes $Notes | Out-Host }
finally { Pop-Location }
if ($LASTEXITCODE -ne 0) { throw "release failed" }
Write-Host "published v$Version"
