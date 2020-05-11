param(
    [Parameter()]
    [ValidateSet('Debug', 'Release')]
    $Configuration = 'Debug'
)

$ErrorActionPreference = 'Stop'

$netTarget = 'netstandard2.0'
$moduleName = "PSArm"
$dotnetLibName = $moduleName
$outDir = "$PSScriptRoot/out/$moduleName"
$srcDir = "$PSScriptRoot/src"
$dotnetSrcDir = $srcDir
$binDir = "$srcDir/bin/$Configuration/$netTarget"

Push-Location $dotnetSrcDir
try
{
    dotnet restore
    dotnet build
}
finally
{
    Pop-Location
}

if (Test-Path $outDir)
{
    Remove-Item -Path $outDir -Recurse -Force
}

$assets = @(
    "$binDir/*.dll",
    "$binDir/*.pdb",
    "$srcDir/$moduleName.psm1",
    "$srcDir/$moduleName.psd1",
    "$srcDir/schemas",
    "$srcDir/OnImport.ps1"
)

New-Item -ItemType Directory -Path $outDir
foreach ($path in $assets)
{
    Copy-Item -Recurse -Path $path -Destination $outDir
}