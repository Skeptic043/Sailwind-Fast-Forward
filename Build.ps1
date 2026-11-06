param(
    [string]$GameDir = 'C:\Steam Games\steamapps\common\Sailwind',
    [string]$BepInExCore = "$PSScriptRoot\.local\references"
)
$ErrorActionPreference = 'Stop'
$project = [xml](Get-Content -LiteralPath "$PSScriptRoot\src\SailwindFastForward.csproj" -Raw)
$version = [string]$project.Project.PropertyGroup.Version
dotnet build "$PSScriptRoot\src\SailwindFastForward.csproj" -c Release "-p:GameDir=$GameDir" "-p:BepInExCore=$BepInExCore" -p:NuGetAudit=false
if ($LASTEXITCODE -ne 0) { throw 'Plugin build failed.' }
dotnet run --project "$PSScriptRoot\tests\OwnershipChecks.csproj" -c Release "-p:GameDir=$GameDir" "-p:BepInExCore=$BepInExCore" -p:NuGetAudit=false
if ($LASTEXITCODE -ne 0) { throw 'Ownership checks failed.' }
$dll = "$PSScriptRoot\src\bin\Release\netstandard2.0\SailwindFastForward.dll"
$outputDir = Join-Path $PSScriptRoot "artifacts\build\$version"
New-Item -ItemType Directory -Force $outputDir | Out-Null
Copy-Item -LiteralPath $dll -Destination $outputDir
Get-FileHash -LiteralPath "$outputDir\SailwindFastForward.dll" | Format-List
