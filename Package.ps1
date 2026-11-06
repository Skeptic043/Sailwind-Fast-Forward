param(
    [string]$GameDir = 'C:\Steam Games\steamapps\common\Sailwind',
    [string]$BepInExCore = "$PSScriptRoot\.local\references"
)
$ErrorActionPreference = 'Stop'
& "$PSScriptRoot\Build.ps1" -GameDir $GameDir -BepInExCore $BepInExCore

Add-Type -AssemblyName System.IO.Compression.FileSystem
Add-Type -AssemblyName System.Drawing
$manifest = Get-Content -LiteralPath "$PSScriptRoot\manifest.json" -Raw | ConvertFrom-Json
$version = $manifest.version_number
if ($manifest.name -cnotmatch '^[A-Za-z0-9_]{1,128}$') { throw 'Invalid package name.' }
if ($version -notmatch '^\d+\.\d+\.\d+$') { throw 'Invalid release version.' }
if ([string]::IsNullOrWhiteSpace($manifest.description) -or $manifest.description.Length -gt 250) {
    throw 'Package description must contain 1-250 characters.'
}
if ($null -eq $manifest.website_url -or ($manifest.website_url -ne '' -and
    $manifest.website_url -notmatch '^https?://')) { throw 'Invalid website URL.' }
if (@($manifest.dependencies).Count -lt 1) { throw 'Missing loader dependency.' }
foreach ($dependency in $manifest.dependencies) {
    if ($dependency -notmatch '^[A-Za-z0-9_]+-[A-Za-z0-9_]+-\d+\.\d+\.\d+$') {
        throw "Invalid dependency: $dependency"
    }
}
$icon = [Drawing.Image]::FromFile("$PSScriptRoot\icon.png")
try {
    if ($icon.Width -ne 256 -or $icon.Height -ne 256 -or
        $icon.RawFormat.Guid -ne [Drawing.Imaging.ImageFormat]::Png.Guid) {
        throw 'Icon must be a 256x256 PNG.'
    }
} finally { $icon.Dispose() }

# Explicit lists prevent accidental redistribution of local references or research.
$releaseFiles = [ordered]@{}
foreach ($path in @('manifest.json', 'README.md', 'CHANGELOG.md', 'LICENSE', 'icon.png')) {
    $releaseFiles[$path] = Join-Path $PSScriptRoot $path
}
$releaseFiles['BepInEx/plugins/SailwindFastForward/SailwindFastForward.dll'] =
    "$PSScriptRoot\artifacts\build\$version\SailwindFastForward.dll"

function Write-VerifiedZip($Destination, $Files) {
    $temporary = "$Destination.$([Guid]::NewGuid().ToString('N')).tmp"
    try {
        $archive = [IO.Compression.ZipFile]::Open($temporary, [IO.Compression.ZipArchiveMode]::Create)
        try {
            foreach ($entry in $Files.GetEnumerator()) {
                $null = [IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
                    $archive, $entry.Value, $entry.Key, [IO.Compression.CompressionLevel]::Optimal)
            }
        } finally { $archive.Dispose() }
        $archive = [IO.Compression.ZipFile]::OpenRead($temporary)
        try {
            if ($archive.Entries.Count -ne $Files.Count) { throw 'ZIP entry count differs.' }
            foreach ($entry in $Files.GetEnumerator()) {
                $packed = $archive.GetEntry($entry.Key)
                if ($null -eq $packed) { throw "Missing ZIP entry: $($entry.Key)" }
                $stream = $packed.Open()
                $sha = [Security.Cryptography.SHA256]::Create()
                try { $hash = [BitConverter]::ToString($sha.ComputeHash($stream)).Replace('-', '') }
                finally { $stream.Dispose(); $sha.Dispose() }
                if ($hash -ne (Get-FileHash -LiteralPath $entry.Value -Algorithm SHA256).Hash) {
                    throw "ZIP hash mismatch: $($entry.Key)"
                }
            }
        } finally { $archive.Dispose() }
        Move-Item -LiteralPath $temporary -Destination $Destination -Force
        Write-Host "Verified $($Files.Count) files: $Destination"
    } finally {
        if (Test-Path -LiteralPath $temporary) { Remove-Item -LiteralPath $temporary }
    }
}

$outputDir = Join-Path $PSScriptRoot 'artifacts\release'
New-Item -ItemType Directory -Force $outputDir | Out-Null
$releaseZip = Join-Path $outputDir "SailwindFastForward-$version.zip"
Write-VerifiedZip $releaseZip $releaseFiles
