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
$srcDir = "$PSScriptRoot/$dotnetLibName"
$binDir = "$srcDir/bin/$Configuration/$netTarget"

Push-Location $srcDir
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

New-Item -ItemType Directory -Path $outDir
foreach ($path in "$binDir/*.dll", "$binDir/*.pdb", "$PSScriptRoot/$moduleName.psm1", "$PSScriptRoot/$moduleName.psd1", "$PSScriptRoot/dsls")
{
    Copy-Item -Recurse -Path $path -Destination $outDir
}