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
$pluginSource = Get-Content -LiteralPath "$PSScriptRoot\src\Plugin.cs" -Raw
$pluginVersion = [regex]::Match($pluginSource, '\[BepInPlugin\(Id, "Sailwind Fast Forward", "([^"]+)"\)\]').Groups[1].Value
if ($pluginVersion -ne $version) { throw 'BepInPlugin version differs from manifest.' }
$icon = [Drawing.Image]::FromFile("$PSScriptRoot\icon.png")
try {
    if ($icon.Width -ne 256 -or $icon.Height -ne 256 -or
        $icon.RawFormat.Guid -ne [Drawing.Imaging.ImageFormat]::Png.Guid) {
        throw 'Icon must be a 256x256 PNG.'
    }
} finally { $icon.Dispose() }

# Explicit lists prevent accidental redistribution of local references or research.
$releaseFiles = [ordered]@{}
foreach ($path in @('manifest.json', 'README.md', 'CHANGELOG.md', 'LICENSE', 'icon.png',
    'docs/BUILDING.md', 'docs/RELEASE_REVIEW.md')) {
    $releaseFiles[$path] = Join-Path $PSScriptRoot $path
}
$releaseFiles['plugins/SailwindFastForward/SailwindFastForward.dll'] =
    "$PSScriptRoot\artifacts\build\$version\SailwindFastForward.dll"
$sourceFiles = [ordered]@{}
foreach ($path in @('.gitignore', '.gitattributes', 'manifest.json', 'README.md', 'CHANGELOG.md',
    'LICENSE', 'icon.png', 'assets/icon.svg', 'tools/Render-Icon.ps1', 'Build.ps1', 'Package.ps1',
    'Directory.Build.props', 'docs/BUILDING.md', 'docs/RELEASE_REVIEW.md',
    'src/AutosaveState.cs', 'src/AutosavePatch.cs', 'src/SaveErrorBoundary.cs', 'src/SaveContinuation.cs', 'src/SaveCoroutine.cs', 'src/Plugin.cs', 'src/PluginSettings.cs', 'src/SpeedOwnership.cs', 'src/HotkeyInput.cs', 'src/HoldInput.cs', 'src/BackgroundExecution.cs',
    'src/ShortcutParser.cs', 'src/ShortcutSetting.cs', 'src/HoldPolicy.cs',
    'src/SailwindFastForward.csproj', 'tests/Program.cs', 'tests/HoldChecks.cs', 'tests/HoldOverrideChecks.cs', 'tests/ShortcutChecks.cs', 'tests/AutosaveChecks.cs', 'tests/HarmonyNormalizationChecks.cs', 'tests/CompatibilityChecks.cs', 'tests/OwnershipChecks.csproj')) {
    $sourceFiles[$path] = Join-Path $PSScriptRoot $path
}
$utf8 = [Text.UTF8Encoding]::new($false, $true)
foreach ($path in $sourceFiles.Values) {
    if ([IO.Path]::GetExtension($path) -ne '.png') {
        $null = $utf8.GetString([IO.File]::ReadAllBytes($path))
    }
}

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
$releaseZip = Join-Path $outputDir "$($manifest.name)-$version.zip"
$sourceZip = Join-Path $outputDir "$($manifest.name)-$version-source.zip"
Write-VerifiedZip $releaseZip $releaseFiles
Write-VerifiedZip $sourceZip $sourceFiles
$checksums = foreach ($path in @($releaseZip, $sourceZip)) {
    "$( (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash )  $([IO.Path]::GetFileName($path))"
}
[IO.File]::WriteAllLines((Join-Path $outputDir "SHA256SUMS-$version.txt"), [string[]]$checksums, $utf8)
$checksums
