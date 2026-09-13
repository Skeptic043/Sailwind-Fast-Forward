param(
    [string]$GameDir = 'C:\Steam Games\steamapps\common\Sailwind',
    [string]$BepInExCore = "$PSScriptRoot\.local\references"
)
$ErrorActionPreference = 'Stop'
$project = [xml](Get-Content -LiteralPath "$PSScriptRoot\src\SailwindFastForward.csproj" -Raw)
$version = [string]$project.Project.PropertyGroup.Version
$manifest = Get-Content -LiteralPath "$PSScriptRoot\manifest.json" -Raw | ConvertFrom-Json
if ($version -ne $manifest.version_number) { throw 'Project and manifest versions differ.' }
dotnet build "$PSScriptRoot\src\SailwindFastForward.csproj" -c Release "-p:GameDir=$GameDir" "-p:BepInExCore=$BepInExCore" -p:NuGetAudit=false
if ($LASTEXITCODE -ne 0) { throw 'Plugin build failed.' }
dotnet run --project "$PSScriptRoot\tests\OwnershipChecks.csproj" -c Release "-p:GameDir=$GameDir" -p:NuGetAudit=false
if ($LASTEXITCODE -ne 0) { throw 'Ownership checks failed.' }
$dll = "$PSScriptRoot\src\bin\Release\netstandard2.0\SailwindFastForward.dll"
if ([Reflection.AssemblyName]::GetAssemblyName($dll).Version.ToString(3) -ne $version) {
    throw 'Built assembly version differs from manifest.'
}
$outputDir = Join-Path $PSScriptRoot "artifacts\build\$version"
New-Item -ItemType Directory -Force $outputDir | Out-Null
Copy-Item -LiteralPath $dll -Destination $outputDir
Get-FileHash -LiteralPath "$outputDir\SailwindFastForward.dll" | Format-List
